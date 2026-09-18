using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using KnowHowToAI.TestSupport;
using Microsoft.Win32;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Startet die veröffentlichte KnowHowToAI.Server-EXE aus einem frischen
/// dotnet-publish-Verzeichnis an einem freien Loopback-Port. Der Host ist eine
/// Black-Box-Testinfrastruktur ohne Assertions; Prozess, Port und Verzeichnis
/// werden beim Verlassen in jedem Fall freigegeben.
/// </summary>
public sealed class PublishedServerHost : IAsyncDisposable
{
    private readonly Process _serverProcess;
    private readonly TestTempDirectory _testDirectory;

    private PublishedServerHost(TestTempDirectory testDirectory, Process serverProcess, string address)
    {
        _testDirectory = testDirectory;
        _serverProcess = serverProcess;
        Address = address;
    }

    public string Address { get; }

    public static async Task<PublishedServerHost> StartAsync()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

        EnsureChromeStableIsInstalled();
        var repositoryRoot = TestRepositoryRoot.Resolve();
        var testDirectory = TestTempDirectory.Create("browser-tests");

        try
        {
            var publishDirectory = testDirectory.FilePath("publish");
            await PublishServerAsync(repositoryRoot, publishDirectory);
            var address = AllocateLoopbackAddress();
            var serverProcess = StartServer(publishDirectory, address);
            return new PublishedServerHost(testDirectory, serverProcess, address);
        }
        catch
        {
            testDirectory.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_serverProcess.HasExited)
                _serverProcess.Kill(entireProcessTree: true);

            await _serverProcess.WaitForExitAsync();
        }
        finally
        {
            _serverProcess.Dispose();
            _testDirectory.Dispose();
        }
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureChromeStableIsInstalled()
    {
        var version = ReadChromeVersion(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome"))
            ?? ReadChromeVersion(Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome"));
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("Google Chrome Stable ist erforderlich; es wurde keine installierte Version gefunden.");
    }

    private static async Task PublishServerAsync(string repositoryRoot, string publishDirectory)
    {
        var publish = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        publish.ArgumentList.Add("publish");
        publish.ArgumentList.Add(Path.Combine(repositoryRoot, "src", "KnowHowToAI.Server", "KnowHowToAI.Server.csproj"));
        publish.ArgumentList.Add("--nologo");
        publish.ArgumentList.Add("--output");
        publish.ArgumentList.Add(publishDirectory);

        using var process = Process.Start(publish) ?? throw new InvalidOperationException("Der Publish-Prozess konnte nicht gestartet werden.");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Der Publish-Prozess ist mit Exitcode {process.ExitCode} beendet worden.");
    }

    private static Process StartServer(string publishDirectory, string address)
    {
        var executable = Path.Combine(publishDirectory, "KnowHowToAI.Server.exe");
        if (!File.Exists(executable))
            throw new FileNotFoundException("Die veröffentlichte Server-EXE fehlt.", executable);

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = publishDirectory
        };
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(address);
        start.ArgumentList.Add("--contentRoot");
        start.ArgumentList.Add(publishDirectory);
        start.ArgumentList.Add("--KnowHowToAI:Migrations:ApplyOnStartup=false");
        return Process.Start(start) ?? throw new InvalidOperationException("Die veröffentlichte Server-EXE konnte nicht gestartet werden.");
    }

    private static string AllocateLoopbackAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return $"http://127.0.0.1:{port}";
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadChromeVersion(RegistryKey? key)
    {
        using (key)
            return key?.GetValue("DisplayVersion") as string;
    }
}

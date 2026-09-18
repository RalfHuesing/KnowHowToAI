using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
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
    private readonly string _testRoot;

    private PublishedServerHost(string testRoot, Process serverProcess, string address)
    {
        _testRoot = testRoot;
        _serverProcess = serverProcess;
        Address = address;
    }

    public string Address { get; }

    public static async Task<PublishedServerHost> StartAsync()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

        EnsureChromeStableIsInstalled();
        var repositoryRoot = FindRepositoryRoot();
        var testRoot = Path.Combine(repositoryRoot, "temp", "browser-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);

        try
        {
            var publishDirectory = Path.Combine(testRoot, "publish");
            await PublishServerAsync(repositoryRoot, publishDirectory);
            var address = AllocateLoopbackAddress();
            var serverProcess = StartServer(publishDirectory, address);
            return new PublishedServerHost(testRoot, serverProcess, address);
        }
        catch
        {
            Directory.Delete(testRoot, recursive: true);
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
            DeleteDirectoryWithRetry(_testRoot);
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

    // Windows hält die Image-Section eines frisch beendeten Prozesses kurz nach
    // dem Exit-Event weiterhin geöffnet; der Löschversuch wartet diesen Nachlauf
    // über wenige Wiederholungen ab, statt den Test am Cleanup scheitern zu lassen.
    private static void DeleteDirectoryWithRetry(string path)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception exception) when (
                attempt < 5 &&
                exception is UnauthorizedAccessException or IOException)
            {
                Thread.Sleep(millisecondsTimeout: 200);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "KnowHowToAI.slnx")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException("Das Repository-Root konnte nicht ermittelt werden.");
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadChromeVersion(RegistryKey? key)
    {
        using (key)
            return key?.GetValue("DisplayVersion") as string;
    }
}

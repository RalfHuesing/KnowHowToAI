using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using Microsoft.Playwright;
using Microsoft.Win32;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Trait("Category", "Integration")]
public sealed class ShellSmokeTests
{
    private const string RequiredChromeVersion = "152.0.7977.83";

    [Fact]
    public async Task RootShell_UsesOneInteractiveCircuitWithoutServerLoopback()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

        EnsureRequiredChromeVersion();
        var repositoryRoot = FindRepositoryRoot();
        var testRoot = Path.Combine(repositoryRoot, "temp", "browser-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        Process? serverProcess = null;

        try
        {
            var publishDirectory = Path.Combine(testRoot, "publish");
            await PublishServerAsync(repositoryRoot, publishDirectory);
            var address = AllocateLoopbackAddress();
            serverProcess = StartServer(publishDirectory, address);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = "chrome",
                Headless = true
            });
            var page = await browser.NewPageAsync();
            var observedRequests = new List<string>();
            page.Request += (_, request) => observedRequests.Add(request.Url);

            var response = await page.GotoAsync(address, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.OK, response.Status);
            Assert.Contains("text/html", response.Headers["content-type"], StringComparison.OrdinalIgnoreCase);
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "KnowHowToAI" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("shell-status")).ToContainTextAsync("Shell bereit");
            await page.GetByRole(AriaRole.Button, new() { Name = "Interaktivität prüfen" }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("interaction-status")).ToHaveTextAsync("Interaktivität ist verfügbar.");

            Assert.NotEmpty(observedRequests);
            Assert.All(observedRequests, request => Assert.StartsWith(address, request, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (serverProcess is not null)
            {
                if (!serverProcess.HasExited)
                    serverProcess.Kill(entireProcessTree: true);
                await serverProcess.WaitForExitAsync();
                serverProcess.Dispose();
            }

            Directory.Delete(testRoot, recursive: true);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureRequiredChromeVersion()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

        var version = ReadChromeVersion(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome"))
            ?? ReadChromeVersion(Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Google Chrome"));
        if (!string.Equals(version, RequiredChromeVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Google Chrome Stable {RequiredChromeVersion} ist erforderlich; gefunden: {version ?? "nicht installiert"}.");
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

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
    [Fact]
    public async Task RootShell_UsesOneInteractiveCircuitWithoutServerLoopback()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

        EnsureChromeStableIsInstalled();
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
            // Der Klick kann ankommen, bevor der Circuit das Ereignis verdrahtet
            // hat (Warmup nach dem Serverstart). Deshalb klicken wir erneut, bis
            // der beobachtbare Statuswechsel die Interaktivität belegt.
            var interactionStatus = page.GetByTestId("interaction-status");
            for (var attempt = 1; ; attempt++)
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Interaktivität prüfen" }).ClickAsync();
                try
                {
                    await Assertions.Expect(interactionStatus).ToHaveTextAsync(
                        "Interaktivität ist verfügbar.",
                        new() { Timeout = 2_000 });
                    break;
                }
                catch (PlaywrightException) when (attempt < 10)
                {
                    // Circuit noch nicht verbunden; erneut klicken.
                }
            }

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

            DeleteDirectoryWithRetry(testRoot);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureChromeStableIsInstalled()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

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

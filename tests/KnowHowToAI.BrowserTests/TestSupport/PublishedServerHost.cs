using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Startet die veröffentlichte KnowHowToAI.Server-EXE aus einem frischen
/// dotnet-publish-Verzeichnis an einem freien Loopback-Port. Der Host ist eine
/// Black-Box-Testinfrastruktur ohne Assertions; Prozess, Port und Verzeichnis
/// werden beim Verlassen in jedem Fall freigegeben.
/// </summary>
public sealed class PublishedServerHost : IAsyncDisposable
{
    // Mehrere gleichzeitige dotnet-publish-Aufrufe desselben Projekts konkurrieren
    // um MSBuild-Locks in obj/ und scheitern intermittierend mit Exitcode 1. Das
    // Browser-Testprojekt deaktiviert zusätzlich die xUnit-Parallelisierung, weil
    // Workflow-Smokes und dedizierte Host-Smokes dasselbe Browser-Testziel nutzen.
    // Der Publish-Gate bleibt als lokale Schutzgrenze bestehen.
    private static readonly SemaphoreSlim _publishGate = new(1, 1);

    private readonly Process _serverProcess;
    private readonly TestTempDirectory _testDirectory;

    private PublishedServerHost(TestTempDirectory testDirectory, Process serverProcess, string address, BoundedProcessLog log)
    {
        _testDirectory = testDirectory;
        _serverProcess = serverProcess;
        Address = address;
        Log = log;
    }

    public string Address { get; }

    /// <summary>
    /// Begrenzte, redigierte Ausgabe des Serverprozesses für Diagnosen;
    /// bewusst nicht unbegrenzt und nicht als Verhaltensassertion nutzbar.
    /// </summary>
    public BoundedProcessLog Log { get; }

    public static Task<PublishedServerHost> StartAsync(string? address = null) =>
        StartAsync(BrowserTestDatabaseKind.Workflow, address);

    internal static Task<PublishedServerHost> StartWithoutDatabaseCleanupAsync(string address) =>
        StartAsync(BrowserTestDatabaseKind.Workflow, address, cleanDatabase: false);

    internal static async Task<PublishedServerHost> StartAsync(
        BrowserTestDatabaseKind databaseKind,
        string? address = null,
        bool cleanDatabase = true)
    {
        ChromeStablePreflight.EnsureIsInstalled();
        var repositoryRoot = TestRepositoryRoot.Resolve();
        var testDirectory = TestTempDirectory.Create("browser-tests");

        try
        {
            var workflowDatabaseSettings = BrowserTestDatabaseSettings.LoadWorkflow(repositoryRoot);
            var visualDatabaseSettings = BrowserTestDatabaseSettings.LoadVisualShell(repositoryRoot);
            var databaseSettings = databaseKind switch
            {
                BrowserTestDatabaseKind.Workflow => workflowDatabaseSettings,
                BrowserTestDatabaseKind.VisualShell => visualDatabaseSettings,
                _ => throw new ArgumentOutOfRangeException(nameof(databaseKind), databaseKind, "Unbekannter Browser-Testdatenbanktyp.")
            };
            // Die Browser-Suite liest die Produktsektion DatabaseConnection nicht.
            // Sie bereinigt ausschließlich die explizit gewählte, präfixgeschützte
            // Workflow- oder Visual-Testdatenbank.
            if (cleanDatabase)
                await BrowserTestDatabaseCleaner.CleanSchemaAsync(databaseSettings).ConfigureAwait(false);
            var publishDirectory = testDirectory.FilePath("publish");
            await PublishServerAsync(repositoryRoot, publishDirectory);
            // Explizite Adresse für Tests, die denselben Circuit-Origin erneut
            // erreichen müssen (Hostneustart); sonst eine freie Loopback-Adresse.
            address ??= AllocateLoopbackAddress();
            var processLog = new BoundedProcessLog();
            var serverProcess = StartServer(publishDirectory, address, processLog, databaseSettings);
            try
            {
                // Der Server wird erst weitergereicht, wenn er tatsächlich antwortet;
                // der erste Start einer frisch veröffentlichten EXE kann mehrere
                // Sekunden dauern, und Browser-Navigation würde sonst ins Leere laufen.
                await WaitForReadinessAsync(address, serverProcess, processLog);
                return new PublishedServerHost(testDirectory, serverProcess, address, processLog);
            }
            catch
            {
                if (!serverProcess.HasExited)
                    serverProcess.Kill(entireProcessTree: true);

                serverProcess.Dispose();
                throw;
            }
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

    private static async Task PublishServerAsync(string repositoryRoot, string publishDirectory)
    {
        await _publishGate.WaitAsync();
        try
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
        finally
        {
            _publishGate.Release();
        }
    }

    private static Process StartServer(
        string publishDirectory,
        string address,
        BoundedProcessLog processLog,
        BrowserTestDatabaseSettings databaseSettings)
    {
        var executable = Path.Combine(publishDirectory, "KnowHowToAI.Server.exe");
        if (!File.Exists(executable))
            throw new FileNotFoundException("Die veröffentlichte Server-EXE fehlt.", executable);

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = publishDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(address);
        start.ArgumentList.Add("--contentRoot");
        start.ArgumentList.Add(publishDirectory);
        start.ArgumentList.Add("--KnowHowToAI:Migrations:ApplyOnStartup=true");
        ApplyBrowserDatabaseConfiguration(start, databaseSettings);
        var process = Process.Start(start) ?? throw new InvalidOperationException("Die veröffentlichte Server-EXE konnte nicht gestartet werden.");
        _ = CollectOutputAsync(process.StandardOutput, processLog);
        _ = CollectOutputAsync(process.StandardError, processLog);
        return process;
    }

    private static void ApplyBrowserDatabaseConfiguration(
        ProcessStartInfo start,
        BrowserTestDatabaseSettings databaseSettings)
    {
        // Zugangsdaten werden nicht in ArgumentList übergeben: Betriebssysteme
        // können Prozessargumente sichtbar machen. Die Child-Environment ist
        // ausschließlich für die veröffentlichte Test-EXE gültig.
        start.Environment["DatabaseConnection__Server"] = databaseSettings.Server;
        start.Environment["DatabaseConnection__Database"] = databaseSettings.Database;
        start.Environment["DatabaseConnection__UserName"] = databaseSettings.UserName;
        start.Environment["DatabaseConnection__Password"] = databaseSettings.Password;
        start.Environment["DatabaseConnection__UseWindowsAuthentication"] = databaseSettings.UseWindowsAuthentication.ToString();
    }

    private static async Task CollectOutputAsync(StreamReader reader, BoundedProcessLog processLog)
    {
        while (await reader.ReadLineAsync() is { } line)
            processLog.Append(line);
    }

    private static async Task WaitForReadinessAsync(string address, Process serverProcess, BoundedProcessLog processLog)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Der veröffentlichte Server ist vor der Betriebsbereitschaft mit Exitcode {serverProcess.ExitCode} beendet worden. " +
                    $"Letzte begrenzte Logzeilen: {string.Join(" | ", processLog.Snapshot())}");
            }

            try
            {
                _ = await client.GetAsync(address);
                return;
            }
            catch (Exception exception) when (exception is HttpRequestException or SocketException or TaskCanceledException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
        }

        throw new TimeoutException(
            $"Der veröffentlichte Server unter {address} wurde nicht rechtzeitig betriebsbereit. Letzte begrenzte Logzeilen: {string.Join(" | ", processLog.Snapshot())}");
    }

    private static string AllocateLoopbackAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return $"http://127.0.0.1:{port}";
    }
}

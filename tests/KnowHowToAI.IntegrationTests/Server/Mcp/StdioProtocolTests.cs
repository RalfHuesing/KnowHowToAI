using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// STDIO-Integrationstests: starten einen echten Serverprozess und prüfen,
/// dass stdout ausschließlich MCP-Protokollnachrichten enthält, Startup-/SQL-/
/// Logging-Ausgaben stdout nicht verunreinigen, protokollkonforme Antworten
/// auf bekannte Anfragen geliefert werden und der Prozess kontrolliert endet
/// (verbindlich: docs/Roadmap.md M6.7, docs/konzept/05-MCP-API.md Abschnitt 64,
/// .agents/rules/TestRichtlinien.mdc MCP-Vertragstests).
/// </summary>
[Trait("Category", "Integration")]
public sealed class StdioProtocolTests
{
    private static readonly string ServerProjectPath =
        FindRepoRoot("KnowHowToAI.slnx") is { } root
            ? Path.Combine(root, "src", "KnowHowToAI.Server", "KnowHowToAI.Server.csproj")
            : throw new InvalidOperationException("Repository-Root nicht gefunden.");

    // ── Initialize → Shutdown ─────────────────────────────────────────────────

    [Fact]
    public async Task Initialize_ReceivesValidJsonRpcResultWithServerInfo()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);

        var initializeRequest = BuildRequest(
            id: 1,
            method: "initialize",
            new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "StdioProtocolTests", version = "1.0" }
            });

        await server.WriteLineAsync(initializeRequest, cts.Token);

        var response = await server.ReadLineAsync(cts.Token);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.True(root.TryGetProperty("result", out var result),
            $"Kein 'result' in Antwort: {response}");
        Assert.True(result.TryGetProperty("serverInfo", out _),
            $"Kein 'serverInfo' in result: {response}");
    }

    // ── stdout-Reinheit ────────────────────────────────────────────────────────

    [Fact]
    public async Task Server_BeforeFirstMessage_DoesNotWriteAnythingToStdout()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = await StartServerAsync(cts.Token);

        // Kurz warten, damit etwaige asynchrone Startup-Logs stdout erreichen könnten.
        await Task.Delay(400, cts.Token);

        var stdoutLines = server.DrainBufferedLines();
        Assert.Empty(stdoutLines);
    }

    // ── Unbekanntes Tool ──────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownTool_ReturnsJsonRpcErrorWithoutProcessCrash()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);
        await InitializeServerAsync(server, cts.Token);

        var callRequest = BuildRequest(
            id: 2,
            method: "tools/call",
            new { name = "nonexistent_tool_xyz", arguments = new { } });

        await server.WriteLineAsync(callRequest, cts.Token);

        var response = await server.ReadLineAsync(cts.Token);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        // Das Protokoll erwartet eine strukturierte Antwort (result oder error), keinen Prozessabsturz.
        Assert.True(
            root.TryGetProperty("result", out _) || root.TryGetProperty("error", out _),
            $"Protokollkonforme Antwort erwartet, erhalten: {response}");
        Assert.False(server.HasExited, "Prozess darf nach unbekanntem Tool nicht abgestürzt sein.");
    }

    [Fact]
    public async Task KnownToolCall_ReturnsStructuredDomainEnvelopeOverStdio()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);
        await InitializeServerAsync(server, cts.Token);

        var callRequest = BuildRequest(
            id: 2,
            method: "tools/call",
            new { name = "get_transaction", arguments = new { transactionId = "not-a-guid" } });

        await server.WriteLineAsync(callRequest, cts.Token);

        var response = await server.ReadLineAsync(cts.Token);

        using var document = JsonDocument.Parse(response);
        Assert.Equal(2, document.RootElement.GetProperty("id").GetInt32());
        Assert.Contains("TransactionNotFound", response, StringComparison.Ordinal);
        Assert.False(server.HasExited, "Prozess darf nach einem fachlich abgelehnten Tool-Aufruf nicht abstürzen.");
    }

    // ── Ungültige JSON-Zeile ──────────────────────────────────────────────────
    //
    // Das MCP-SDK garantiert für unparseierbare stdin-Zeilen keine dedizierte
    // -32700-Antwort (Diagnose gehört per Konfiguration nach stderr), aber es
    // darf stdout niemals verunreinigen, den Prozess nie abbrechen und muss
    // anschließend gültige Anfragen weiterhin beantworten (Roadmap M6.7,
    // TestRichtlinien: stdout ausschließlich Protokollnachrichten).

    [Fact]
    public async Task MalformedJsonLine_StdoutStaysProtocolCleanAndServerContinuesServing()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);
        await InitializeServerAsync(server, cts.Token);

        await server.WriteLineAsync("not-valid-json-{{{", cts.Token);

        var listRequest = BuildRequest(id: 2, method: "tools/list", new { });
        await server.WriteLineAsync(listRequest, cts.Token);

        // Liest Zeilen bis zur Antwort auf die gültige Folgerequest; jede
        // gelesene Zeile muss gültiges JSON sein (sonst schlägt der Parse fehl).
        var lines = await ReadLinesUntilIdAsync(server, 2, cts.Token);

        Assert.NotEmpty(lines);
        Assert.False(server.HasExited, "Prozess darf nach ungültigem JSON nicht abgestürzt sein.");
    }

    // ── Ungültige JSON-Typen und unbekannte Felder in Arguments ───────────────

    [Fact]
    public async Task ToolsCall_UnknownFieldsAndInvalidArgumentTypes_ReturnsProtocolConformantAnswerWithoutCrash()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);
        await InitializeServerAsync(server, cts.Token);

        var callRequest = BuildRequest(
            id: 2,
            method: "tools/call",
            new { name = "get_snapshot", arguments = new { snapshotId = 123, unbekanntesFeld = "x" } });

        await server.WriteLineAsync(callRequest, cts.Token);

        var response = await server.ReadLineAsync(cts.Token);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        Assert.True(
            root.TryGetProperty("result", out _) || root.TryGetProperty("error", out _),
            $"Protokollkonforme Antwort erwartet, erhalten: {response}");
        Assert.False(server.HasExited, "Prozess darf nach ungültigen Argumenttypen nicht abgestürzt sein.");
    }

    // ── Cancellation via EOF ───────────────────────────────────────────────────

    [Fact]
    public async Task CloseStdin_ServerShutsDownGracefullyWithoutProcessCrash()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);
        await InitializeServerAsync(server, cts.Token);

        server.CloseInput();

        // Der Server soll innerhalb kurzer Zeit sauber enden.
        var exited = await server.WaitForExitAsync(TimeSpan.FromSeconds(10));

        Assert.True(exited, "Server soll nach geschlossenem stdin sauber beendet werden.");
    }

    // ── tool_list zeigt alle V1-Tools ─────────────────────────────────────────

    [Fact]
    public async Task ToolsList_ReturnsAllReleasedV1Tools()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);
        await InitializeServerAsync(server, cts.Token);

        var listRequest = BuildRequest(id: 2, method: "tools/list", new { });
        await server.WriteLineAsync(listRequest, cts.Token);

        var response = await server.ReadLineAsync(cts.Token);

        using var doc = JsonDocument.Parse(response);
        var tools = doc.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString()!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        // Beweist, dass alle V1-Tools über STDIO erreichbar sind (M6.7 STDIO-Vertragsnachweis).
        Assert.Contains("begin_transaction", tools);
        Assert.Contains("commit_transaction", tools);
        Assert.Contains("get_snapshot", tools);
        Assert.Contains("compare_snapshots", tools);
        Assert.Contains("get_transaction_changes", tools);
        Assert.Contains("create_release", tools);
        Assert.Contains("list_releases", tools);
        Assert.Contains("get_root", tools);
        Assert.Contains("create_node", tools);
        Assert.True(tools.Length >= 28, $"Weniger Tools als erwartet: {tools.Length}");
    }

    // ── stdout enthält nach Protokollverkehr keine Log-Ausgaben ───────────────

    [Fact]
    public async Task AllStdoutLines_AreValidJsonRpc_NoLogPollution()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var server = await StartServerAsync(cts.Token);

        await InitializeServerAsync(server, cts.Token);

        var listRequest = BuildRequest(id: 2, method: "tools/list", new { });
        await server.WriteLineAsync(listRequest, cts.Token);
        await server.ReadLineAsync(cts.Token);

        var allLines = server.DrainBufferedLines();
        foreach (var line in allLines.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            Assert.True(IsValidJson(line), $"stdout enthält Nicht-JSON: {line}");
        }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private static async Task InitializeServerAsync(ServerProcess server, CancellationToken cancellationToken)
    {
        var initializeRequest = BuildRequest(
            id: 1,
            method: "initialize",
            new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "StdioProtocolTests", version = "1.0" }
            });
        await server.WriteLineAsync(initializeRequest, cancellationToken);
        await server.ReadLineAsync(cancellationToken);

        // initialized notification
        var initialized = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}";
        await server.WriteLineAsync(initialized, cancellationToken);
    }

    private static string BuildRequest(int id, string method, object @params) =>
        JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params
        });

    private static bool IsValidJson(string line)
    {
        try
        {
            JsonDocument.Parse(line);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Liest stdout-Zeilen bis zur JSON-RPC-Antwort mit der gesuchten ID und
    /// validiert nebenbei, dass jede gelesene Zeile gültiges JSON ist.
    /// </summary>
    private static async Task<string[]> ReadLinesUntilIdAsync(
        ServerProcess server, int expectedId, CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        while (true)
        {
            var line = await server.ReadLineAsync(cancellationToken);
            Assert.True(IsValidJson(line), $"stdout enthält ungültiges JSON: {line}");
            lines.Add(line);

            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("id", out var id)
                && id.TryGetInt32(out var value)
                && value == expectedId)
            {
                return lines.ToArray();
            }
        }
    }

    private static async Task<ServerProcess> StartServerAsync(CancellationToken cancellationToken)
    {
        // Migrations beim Start deaktivieren, damit kein DB-Zugriff nötig ist.
        // Die SQL-Integrationstests belegen Migration separat (Category=Integration).
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{ServerProjectPath}\" --no-build",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        // Migrationen und DB-Zugriff beim Test-Serverstart abschalten.
        process.StartInfo.Environment["KnowHowToAI__Migrations__ApplyOnStartup"] = "false";

        var serverProcess = new ServerProcess(process);
        await serverProcess.StartAsync(cancellationToken);
        return serverProcess;
    }

    private static string? FindRepoRoot(string markerFile)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, markerFile)))
                return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }

    // ── ServerProcess ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kapselt den gestarteten Serverprozess mit gepuffertem stdout-Lesen,
    /// damit keine Ausgaben vor der ersten Abfrage verloren gehen.
    /// </summary>
    private sealed class ServerProcess : IDisposable
    {
        private readonly Process _process;
        private readonly List<string> _bufferedLines = [];
        private readonly List<string> _stderrLines = [];
        private readonly SemaphoreSlim _lineSemaphore = new(0);
        private readonly CancellationTokenSource _readerCts = new();

        public ServerProcess(Process process) => _process = process;

        public bool HasExited => _process.HasExited;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _process.Start();

            // Hintergrundtask: stdout zeilenweise lesen und puffern.
            _ = Task.Run(ReadStdoutIntoBufferAsync, _readerCts.Token);

            // stderr im Hintergrund drainen, damit die Log-Ausgaben des Servers
            // die Pipe nicht füllen und den Prozess beim Schreiben blockieren.
            _ = Task.Run(DrainStderrAsync, _readerCts.Token);

            // Kurz warten, bis der Prozess sich initialisiert hat.
            await Task.Delay(800, cancellationToken);
        }

        private async Task ReadStdoutIntoBufferAsync()
        {
            try
            {
                while (!_readerCts.Token.IsCancellationRequested)
                {
                    var line = await _process.StandardOutput.ReadLineAsync(_readerCts.Token);
                    if (line is null)
                        break;
                    lock (_bufferedLines)
                        _bufferedLines.Add(line);
                    _lineSemaphore.Release();
                }
            }
            catch (OperationCanceledException) { /* erwartet beim Dispose */ }
        }

        private async Task DrainStderrAsync()
        {
            try
            {
                while (!_readerCts.Token.IsCancellationRequested)
                {
                    var line = await _process.StandardError.ReadLineAsync(_readerCts.Token);
                    if (line is null)
                        break;
                    lock (_stderrLines)
                        _stderrLines.Add(line);
                }
            }
            catch (OperationCanceledException) { /* erwartet beim Dispose */ }
            catch (Exception) { /* stderr ist nur Diagnostik */ }
        }

        public async Task WriteLineAsync(string line, CancellationToken cancellationToken)
        {
            await _process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
            await _process.StandardInput.FlushAsync(cancellationToken);
        }

        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            // Warte auf eine gepufferte Zeile oder Timeout.
            await _lineSemaphore.WaitAsync(cancellationToken);
            lock (_bufferedLines)
            {
                var line = _bufferedLines[0];
                _bufferedLines.RemoveAt(0);
                return line;
            }
        }

        public string[] DrainBufferedLines()
        {
            lock (_bufferedLines)
            {
                var lines = _bufferedLines.ToArray();
                _bufferedLines.Clear();
                return lines;
            }
        }

        public void CloseInput() => _process.StandardInput.Close();

        public async Task<bool> WaitForExitAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await _process.WaitForExitAsync(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _readerCts.Cancel();
            _readerCts.Dispose();
            _lineSemaphore.Dispose();
            if (!_process.HasExited)
            {
                try { _process.Kill(entireProcessTree: true); }
                catch { /* best effort */ }
            }
            _process.Dispose();
        }
    }
}

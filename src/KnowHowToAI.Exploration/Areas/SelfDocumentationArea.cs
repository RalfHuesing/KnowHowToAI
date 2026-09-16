using System.Text;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Transactions;
using KnowHowToAI.Server.Mcp.Tools.Mutations;
using KnowHowToAI.Server.Mcp.Tools.Navigation;
using KnowHowToAI.Server.Mcp.Tools.Retrieval;
using KnowHowToAI.Server.Mcp.Tools.Transactions;

namespace KnowHowToAI.Exploration.Areas;

/// <summary>
/// Beispiel-Bereich "eigene Doku als Testmaterial": importiert die Projekt-Doku
/// aus docs/ end-toend über die originalen Mutation-Tools (Transaction → Struktur
/// → Content → Validate → Commit) und verifiziert das Ergebnis über die Read-Tools.
/// Erneute Läufe sind idempotent: committete Nodes werden anhand Titel wiederverwendet.
/// Was in der DB landet, ist Explorations-Scratch.
/// </summary>
public sealed class SelfDocumentationArea : IExplorationArea
{
    private const string RoleId = "Default";
    private const string RootTitle = "KnowHowTo AI – Dokumentation";

    public string Id => "self-documentation";

    public string Description =>
        "Importiert die eigene Projekt-Doku (docs/) end-toend über die Mutation-Tools: begin_transaction → create_node → replace_content → validate_transaction → commit_transaction; Verifikation per get_root/list_children/search.";

    public IReadOnlyList<ExplorationScenario> CreateScenarios(ExplorationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return
        [
            new("import-readme",
                "docs/README.md als Root-Node importieren, committen und über get_root/search verifizieren",
                async cancellationToken => await ImportReadmeAsync(context, cancellationToken).ConfigureAwait(false)),

            new("import-docs-children",
                "Alle weiteren docs/*.md als Kind-Nodes importieren, committen und über list_children verifizieren",
                async cancellationToken => await ImportDocFilesAsync(context, cancellationToken).ConfigureAwait(false))
        ];
    }

    private static async Task ImportReadmeAsync(ExplorationContext context, CancellationToken cancellationToken)
    {
        var raw = File.ReadAllText(FindDocsFile("README.md"));
        var content = StripHeadings(raw);
        context.Info(
            $"docs/README.md: {Encoding.UTF8.GetByteCount(raw)} Bytes roh, {Encoding.UTF8.GetByteCount(content)} Bytes nach Heading-Strip.");

        var transaction = await BeginTransactionAsync(context, "Doku-Import: README als Root", cancellationToken);
        if (transaction is null)
            return;

        var committed = false;
        try
        {
            var rootId = await EnsureRootAsync(context, transaction.TransactionId, cancellationToken);
            if (rootId is null)
                return;

            if (!await ReplaceContentAsync(context, transaction.TransactionId, rootId, "README.md", content, cancellationToken))
                return;

            if (!await ValidateAsync(context, transaction.TransactionId, cancellationToken))
                return;

            if (!await CommitAsync(context, transaction, "Exploration: docs/README.md importiert", cancellationToken))
                return;

            committed = true;

            var navigationTools = context.Tools<NavigationTools>();
            var retrievalTools = context.Tools<RetrievalTools>();

            var root = await navigationTools
                .GetRoot(RoleId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            context.ReportEnvelope("get_root (verifiziert)", root);
            if (root.Data?.Content is not { Length: > 0 } committedContent)
                context.Bug("Committeter Root liefert keinen Content über get_root.");
            else
                context.Info($"get_root liefert Content zurück ({Encoding.UTF8.GetByteCount(committedContent)} Bytes).");

            var search = await retrievalTools
                .Search("MCP", roleId: RoleId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            context.ReportEnvelope("search ('MCP')", search);
            var hits = search.Data?.Items ?? [];
            if (hits.Count == 0)
                context.Bug("Suche nach 'MCP' findet den committeten Content nicht.");
            else
                context.Info($"Suche nach 'MCP' findet {hits.Count} Treffer, erster: '{hits[0].Title}'.");
        }
        finally
        {
            if (!committed)
                await DiscardAsync(context, transaction.TransactionId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ImportDocFilesAsync(ExplorationContext context, CancellationToken cancellationToken)
    {
        var docsDirectory = Path.GetDirectoryName(FindDocsFile("README.md"))!;
        var docFiles = Directory.GetFiles(docsDirectory, "*.md")
            .Where(path => !string.Equals(Path.GetFileName(path), "README.md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        context.Info($"{docFiles.Length} Doku-Dateien zum Import gefunden.");

        var transaction = await BeginTransactionAsync(context, "Doku-Import: Fachdokumente als Kind-Nodes", cancellationToken);
        if (transaction is null)
            return;

        var committed = false;
        try
        {
            var rootId = await EnsureRootAsync(context, transaction.TransactionId, cancellationToken);
            if (rootId is null)
                return;

            var navigationTools = context.Tools<NavigationTools>();
            var childrenPage = await navigationTools
                .ListChildren(RoleId, parentNodeId: rootId, limit: 100, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var existingByTitle = (childrenPage.Data?.Items ?? [])
                .GroupBy(child => child.Title, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().NodeId, StringComparer.Ordinal);
            if (existingByTitle.Count > 0)
                context.Info($"{existingByTitle.Count} bestehende Kind-Node(s) werden bei Titelsgleichheit wiederverwendet.");

            var imported = 0;
            for (var index = 0; index < docFiles.Length; index++)
            {
                var filePath = docFiles[index];
                var title = Path.GetFileNameWithoutExtension(filePath);
                var raw = File.ReadAllText(filePath);
                var content = StripHeadings(raw);
                context.Info(
                    $"{title}: {Encoding.UTF8.GetByteCount(raw)} Bytes roh, {Encoding.UTF8.GetByteCount(content)} Bytes nach Heading-Strip.");

                if (!existingByTitle.TryGetValue(title, out var nodeId))
                {
                    var created = await context.Tools<NodeMutationTools>()
                        .CreateNode(
                            transaction.TransactionId, title,
                            description: $"Import aus docs/{Path.GetFileName(filePath)}",
                            parentNodeId: rootId,
                            sortOrder: index,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    context.ReportEnvelopeBrief($"create_node ({title})", created);
                    if (created.Data is not { } createdData)
                    {
                        context.Bug($"create_node für '{title}' scheiterte: code={created.Code}, message='{created.Message}'.");
                        return;
                    }

                    nodeId = createdData.NodeId;
                }
                else
                {
                    context.Info($"Kind-Node '{title}' existiert bereits und wird wiederverwendet (nodeId={nodeId}).");
                }

                if (!await ReplaceContentAsync(context, transaction.TransactionId, nodeId, title, content, cancellationToken))
                    return;
                imported++;
            }

            if (!await ValidateAsync(context, transaction.TransactionId, cancellationToken))
                return;
            if (!await CommitAsync(context, transaction, $"Exploration: {imported} Doku-Dateien importiert", cancellationToken))
                return;
            committed = true;

            var verify = await navigationTools
                .ListChildren(RoleId, parentNodeId: rootId, limit: 100, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            context.ReportEnvelope("list_children (verifiziert)", verify);
            var childCount = verify.Data?.Items.Count ?? 0;
            if (childCount < docFiles.Length)
                context.Bug($"Erwartet mindestens {docFiles.Length} Kind-Nodes, gefunden {childCount}.");
            else
                context.Info($"list_children bestätigt {childCount} Kind-Nodes (Import-Erneuerung funktioniert).");
        }
        finally
        {
            if (!committed)
                await DiscardAsync(context, transaction.TransactionId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<McpTransactionData?> BeginTransactionAsync(
        ExplorationContext context, string purpose, CancellationToken cancellationToken)
    {
        var begin = await context.Tools<TransactionTools>()
            .BeginTransaction(
                purpose: purpose,
                actor: "KnowHowToAI.Exploration",
                client: "self-documentation",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        context.ReportEnvelope("begin_transaction", begin);
        if (begin.Data is not { } transaction)
        {
            context.Bug($"begin_transaction scheiterte: code={begin.Code}, message='{begin.Message}'.");
            return null;
        }

        return transaction;
    }

    private static async Task<string?> EnsureRootAsync(
        ExplorationContext context, string transactionId, CancellationToken cancellationToken)
    {
        var committedRoot = await context.Tools<NavigationTools>()
            .GetRoot(RoleId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (committedRoot.Data is { } existing)
        {
            context.Info($"Root '{existing.Title}' existiert bereits und wird wiederverwendet (nodeId={existing.NodeId}).");
            return existing.NodeId;
        }

        var created = await context.Tools<NodeMutationTools>()
            .CreateNode(
                transactionId, RootTitle,
                description: "Import der Projekt-Doku aus docs/ (Explorations-Harness).",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        context.ReportEnvelope("create_node (Root)", created);
        if (created.Data is not { } data)
        {
            context.Bug($"create_node (Root) scheiterte: code={created.Code}, message='{created.Message}'.");
            return null;
        }

        return data.NodeId;
    }

    private static async Task<bool> ReplaceContentAsync(
        ExplorationContext context, string transactionId, string nodeId, string title, string content,
        CancellationToken cancellationToken)
    {
        var replaced = await context.Tools<ContentMutationTools>()
            .ReplaceContent(
                transactionId, nodeId, RoleId, "Independent", content,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        context.ReportEnvelopeBrief($"replace_content ({title})", replaced);
        if (replaced.Data is not { } data)
        {
            context.Bug($"replace_content für '{title}' scheiterte: code={replaced.Code}, message='{replaced.Message}'.");
            return false;
        }

        context.Info($"{title}: Content-Revision {data.ContentRevisionId} ({data.Freshness}) gespeichert.");
        return true;
    }

    private static async Task<bool> ValidateAsync(
        ExplorationContext context, string transactionId, CancellationToken cancellationToken)
    {
        var report = await context.Tools<TransactionTools>()
            .ValidateTransaction(transactionId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        context.ReportEnvelope("validate_transaction", report);
        if (report.Data is not { IsValid: true })
        {
            var errors = report.Data?.Errors.Select(error => $"{error.Code}: {error.Message}").ToList()
                ?? [$"code={report.Code}, message='{report.Message}'"];
            context.Bug($"Working Snapshot ist ungültig: {string.Join("; ", errors)}");
            return false;
        }

        context.Info(
            $"Working Snapshot gültig: {report.Data.Warnings.Count} Warnung(en), {report.Data.StaleContents.Count} stale Content(s), {report.Data.RefactoringCandidates.Count} Refactoring-Kandidat(en).");
        return true;
    }

    private static async Task<bool> CommitAsync(
        ExplorationContext context, McpTransactionData transaction, string commitMessage, CancellationToken cancellationToken)
    {
        var committed = await context.Tools<TransactionTools>()
            .CommitTransaction(transaction.TransactionId, commitMessage, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        context.ReportEnvelope("commit_transaction", committed);
        if (!committed.IsSuccess || committed.Data?.CommittedAtUtc is null)
        {
            context.Bug($"Commit scheiterte: code={committed.Code}, message='{committed.Message}'.");
            return false;
        }

        return true;
    }

    private static async Task DiscardAsync(
        ExplorationContext context, string transactionId, CancellationToken cancellationToken)
    {
        var discarded = await context.Tools<TransactionTools>()
            .DiscardTransaction(transactionId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        context.Info($"Offene Transaction nach Abbruch verworfen: code={discarded.Code}.");
    }

    /// <summary>Entfernt ATX-Überschriften (# …), da Content-Tools Überschriften hart ablehnen.</summary>
    private static string StripHeadings(string markdown) =>
        string.Join('\n', markdown.Split('\n').Where(line => !line.TrimStart().StartsWith("#", StringComparison.Ordinal)));

    private static string FindDocsFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docs", fileName)))
            directory = directory.Parent!;
        if (directory is null)
            throw new InvalidOperationException(
                $"docs/{fileName} wurde ab {AppContext.BaseDirectory} aufwärts nicht gefunden.");

        return Path.Combine(directory.FullName, "docs", fileName);
    }
}

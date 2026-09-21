using System.ComponentModel;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Nodes;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Mutations;

/// <summary>
/// Dünne MCP-Handler der globalen Strukturänderungen: ausschließlich Mapping und
/// Delegation an den transportneutralen <see cref="NodeMutationApplicationService"/>;
/// der kombinierte create_node-Aufruf delegiert zusätzlich an den
/// <see cref="ContentMutationApplicationService"/>.
/// </summary>
[McpServerToolType]
internal sealed class NodeMutationTools
{
    private readonly NodeMutationApplicationService _nodeMutationService;
    private readonly ContentMutationApplicationService _contentMutationService;

    public NodeMutationTools(
        NodeMutationApplicationService nodeMutationService,
        ContentMutationApplicationService contentMutationService)
    {
        _nodeMutationService = nodeMutationService ?? throw new ArgumentNullException(nameof(nodeMutationService));
        _contentMutationService = contentMutationService ?? throw new ArgumentNullException(nameof(contentMutationService));
    }

    [McpServerTool(Name = "create_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Legt eine Node als globale Strukturänderung innerhalb einer offenen Transaction an; " +
        "ohne parentNodeId wird eine Root-Node erzeugt. Mit contentMd wird im selben Aufruf der " +
        "explizite Zielgruppen-Content gesetzt (Semantik von replace_content: audienceId erforderlich, " +
        "contentMode optional mit Standard 'Independent').")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> CreateNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Titel der neuen Node.")] string title,
        [Description("Optionale Beschreibung der Node.")] string? description = null,
        [Description("Optionale Node-ID des Parents (GUID-String); ohne Wert wird eine Root-Node angelegt.")] string? parentNodeId = null,
        [Description("Optionale 0-basierte Sortierposition; wird auf die lückenlose Reihenfolge " +
            "0..N-1 der Geschwistergruppe normalisiert (außerhalb liegende Werte ans Ende, " +
            "negative an den Anfang).")] int sortOrder = 0,
        [Description("Optionaler vollständiger Markdown-Inhalt ohne Überschriften, der im selben " +
            "Aufruf gesetzt wird (Gliederung über Listen und Fließtext, Ersatztitel ggf. als " +
            "freistehender Fettabsatz).")] string? contentMd = null,
        [Description("Zielgruppe des Contents (audienceId aus list_audiences); erforderlich, wenn contentMd gesetzt ist.")] string? audienceId = null,
        [Description("Content-Modus bei contentMd: 'Independent' (Standard) oder 'Derived'.")] string? contentMode = null,
        [Description("Optionale Source-Revisions für Derived Content (je nodeId, audienceId, contentRevisionId).")] McpContentSourceData[]? sources = null,
        [Description("Optionaler erwarteter ChangeVersion-Stand der Transaction; bei einer zwischenzeitlichen Mutation wird der Write abgelehnt.")] long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var parsedArguments = ParseCreateArguments(transactionId, parentNodeId, audienceId, contentMode, contentMd, sources);
        if (!parsedArguments.IsSuccess)
            return McpToolEnvelope<McpNodeMutationData>.Failure(parsedArguments.Error!);

        var (parsedTransactionId, parsedParentNodeId, parsedContent) = parsedArguments.Value!;
        var created = await _nodeMutationService
            .CreateAsync(
                parsedTransactionId,
                new CreateNodeRequest(parsedParentNodeId, title, description, sortOrder),
                expectedChangeVersion,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess)
            return McpMutationMapper.ToEnvelope(created);

        return parsedContent is null
            ? McpMutationMapper.ToEnvelope(created)
            : await WriteCombinedContentAsync(parsedTransactionId, created, parsedContent.Value, expectedChangeVersion, cancellationToken)
                .ConfigureAwait(false);
    }

    private readonly record struct ParsedCreateArguments(
        TransactionId TransactionId,
        NodeId? ParentNodeId,
        CombinedContentSpec? Content);

    /// <summary>
    /// Parst und validiert die create_node-Argumente vor dem ersten Mutationsschritt,
    /// damit ein ungültiger Content-Spec keine halbe Node im Working Snapshot erzeugt.
    /// </summary>
    private static Result<ParsedCreateArguments> ParseCreateArguments(
        string transactionId,
        string? parentNodeId,
        string? audienceId,
        string? contentMode,
        string? contentMd,
        McpContentSourceData[]? sources)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return Result<ParsedCreateArguments>.Failure(parsedTransactionId.Error!);

        var parsedParentNodeId = McpNavigationMapper.ParseOptionalNodeId(parentNodeId);
        if (!parsedParentNodeId.IsSuccess)
            return Result<ParsedCreateArguments>.Failure(parsedParentNodeId.Error!);

        var parsedContent = ParseOptionalContentSpec(audienceId, contentMode, contentMd, sources);
        if (!parsedContent.IsSuccess)
            return Result<ParsedCreateArguments>.Failure(parsedContent.Error!);

        return Result<ParsedCreateArguments>.Success(new ParsedCreateArguments(
            parsedTransactionId.Value,
            parsedParentNodeId.Value,
            parsedContent.Value));
    }

    /// <summary>
    /// Führt den Content-Schritt des kombinierten create_node-Aufrufs aus und
    /// vereinigt beide Mutationsergebnisse zu einem Envelope.
    /// </summary>
    private async Task<McpToolEnvelope<McpNodeMutationData>> WriteCombinedContentAsync(
        TransactionId transactionId,
        Result<NodeMutationResult> created,
        CombinedContentSpec spec,
        long? expectedChangeVersion,
        CancellationToken cancellationToken)
    {
        var contentRequest = new ReplaceContentRequest(
            created.Value!.Node.NodeId,
            spec.Audience,
            spec.ContentMode,
            spec.ContentMd,
            spec.Sources,
            created.Value.ChangeVersion);
        var content = await _contentMutationService
            .ReplaceContentAsync(transactionId, contentRequest, cancellationToken)
            .ConfigureAwait(false);
        return McpMutationMapper.ToMergedEnvelope(created, content);
    }

    private readonly record struct CombinedContentSpec(
        AudienceId Audience, ContentMode ContentMode, string ContentMd, IReadOnlyList<ContentDependencySource> Sources);

    /// <summary>
    /// Liefert die Content-Spezifikation des kombinierten create_node-Aufrufs oder null,
    /// wenn kein contentMd gesetzt ist.
    /// </summary>
    private static Result<CombinedContentSpec?> ParseOptionalContentSpec(
        string? audienceId,
        string? contentMode,
        string? contentMd,
        McpContentSourceData[]? sources)
    {
        if (contentMd is null)
            return Result<CombinedContentSpec?>.Success(null);

        var required = ParseRequiredContentSpec(audienceId, contentMode, contentMd, sources);
        return required.IsSuccess
            ? Result<CombinedContentSpec?>.Success(required.Value)
            : Result<CombinedContentSpec?>.Failure(required.Error!);
    }

    /// <summary>
    /// Parst den Content-Spec bei gesetztem contentMd: audienceId ist erforderlich
    /// (stabile Fehlermeldung AudienceIdRequired), contentMode optional (Standard 'Independent').
    /// </summary>
    private static Result<CombinedContentSpec> ParseRequiredContentSpec(
        string? audienceId,
        string? contentMode,
        string contentMd,
        McpContentSourceData[]? sources)
    {
        if (string.IsNullOrWhiteSpace(audienceId))
        {
            return Result<CombinedContentSpec>.Failure(new DomainError(
                AudienceMutationErrorCodes.AudienceIdRequired,
                "Bei gesetztem contentMd ist audienceId erforderlich.",
                new Dictionary<string, string>
                {
                    [AudienceMutationErrorCodes.AudienceIdDetail] = audienceId ?? string.Empty
                }));
        }

        var parsedContentMode = contentMode is null
            ? Result<ContentMode>.Success(ContentMode.Independent)
            : McpMutationMapper.ParseContentMode(contentMode);
        if (!parsedContentMode.IsSuccess)
            return Result<CombinedContentSpec>.Failure(parsedContentMode.Error!);

        var parsedSources = McpMutationMapper.ParseSources(sources);
        if (!parsedSources.IsSuccess)
            return Result<CombinedContentSpec>.Failure(parsedSources.Error!);

        return Result<CombinedContentSpec>.Success(new CombinedContentSpec(
            new AudienceId(audienceId),
            parsedContentMode.Value,
            contentMd,
            parsedSources.Value!));
    }

    [McpServerTool(Name = "update_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ändert Titel und Beschreibung einer Node als globale Strukturänderung " +
        "innerhalb einer offenen Transaction.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> UpdateNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Neuer Titel der Node.")] string title,
        [Description("Optionale neue Beschreibung der Node.")] string? description = null,
        [Description("Optionaler erwarteter ChangeVersion-Stand der Transaction; bei einer zwischenzeitlichen Mutation wird der Write abgelehnt.")] long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .UpdateAsync(
                parsedTransactionId.Value,
                new UpdateNodeRequest(parsedNodeId.Value!.Value, title, description, expectedChangeVersion),
                cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "move_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Verschiebt eine Node unter einen neuen Parent und setzt ihre Sortierposition; " +
        "ohne parentNodeId wird die Node zur Root-Node.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> MoveNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("0-basierte Sortierposition; wird auf die lückenlose Reihenfolge 0..N-1 " +
            "der (neuen) Geschwistergruppe normalisiert.")] int sortOrder,
        [Description("Optionale Node-ID des neuen Parents (GUID-String); ohne Wert wird die Node zur Root-Node.")] string? parentNodeId = null,
        [Description("Optionaler erwarteter ChangeVersion-Stand der Transaction; bei einer zwischenzeitlichen Mutation wird der Write abgelehnt.")] long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        var parsedParentNodeId = McpNavigationMapper.ParseOptionalNodeId(parentNodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess || !parsedParentNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error, parsedParentNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .MoveAsync(
                parsedTransactionId.Value,
                new MoveNodeRequest(parsedNodeId.Value!.Value, parsedParentNodeId.Value, sortOrder, expectedChangeVersion),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "reorder_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Setzt die Sortierposition einer Node innerhalb ihrer bestehenden " +
        "Geschwistergruppe als globale Strukturänderung.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> ReorderNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Neue 0-basierte Sortierposition; wird auf die lückenlose Reihenfolge " +
            "0..N-1 der Geschwistergruppe normalisiert.")] int sortOrder,
        [Description("Optionaler erwarteter ChangeVersion-Stand der Transaction; bei einer zwischenzeitlichen Mutation wird der Write abgelehnt.")] long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .ReorderAsync(
                parsedTransactionId.Value,
                parsedNodeId.Value!.Value,
                sortOrder,
                expectedChangeVersion,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "delete_node", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Löscht eine Node als globale Strukturänderung über alle Zielgruppen; eine Node " +
        "mit aktiven Children erfordert deleteSubtree=true.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> DeleteNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Optional: gesamten Teilbaum einschließlich aller Children und Contents löschen (Standard false).")] bool deleteSubtree = false,
        [Description("Optionaler erwarteter ChangeVersion-Stand der Transaction; bei einer zwischenzeitlichen Mutation wird der Write abgelehnt.")] long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .DeleteAsync(parsedTransactionId.Value, parsedNodeId.Value!.Value, deleteSubtree, expectedChangeVersion, cancellationToken)
            .ConfigureAwait(false));
    }

    private static McpToolEnvelope<McpNodeMutationData> Failure(params DomainError?[] errors) =>
        McpToolEnvelope<McpNodeMutationData>.Failure(errors.First(error => error is not null)!);
}

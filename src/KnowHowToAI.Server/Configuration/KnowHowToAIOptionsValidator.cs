using Microsoft.Extensions.Options;

namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Validiert <see cref="KnowHowToAIOptions"/> beim Serverstart fail-fast und vollständig.
/// Ungültige Werte liefern einen verständlichen Fehler ohne Secrets.
/// Feldübergreifende Beziehungen (z.B. DefaultPageSize &lt;= MaximumPageSize) stehen hier
/// genau einmal; Defaults stehen ausschließlich in appsettings.json.
/// </summary>
internal sealed class KnowHowToAIOptionsValidator : IValidateOptions<KnowHowToAIOptions>
{
    public ValidateOptionsResult Validate(string? name, KnowHowToAIOptions options)
    {
        var errors = new List<string>();
        ValidateValidation(options.Validation, errors);
        ValidateRetrieval(options.Retrieval, errors);
        ValidateStorage(options.Storage, errors);
        ValidateMigrations(options.Migrations, errors);
        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateValidation(ValidationOptions v, List<string> errors)
    {
        if (v.ContentSizeWarningBytes is < 512 or > 1_048_576)
            errors.Add($"KnowHowToAI:Validation:ContentSizeWarningBytes muss zwischen 512 und 1048576 liegen, ist aber {v.ContentSizeWarningBytes}.");
        if (v.ChildCountWarning is < 2 or > 10_000)
            errors.Add($"KnowHowToAI:Validation:ChildCountWarning muss zwischen 2 und 10000 liegen, ist aber {v.ChildCountWarning}.");
        if (v.HierarchyDepthWarning is < 2 or > 256)
            errors.Add($"KnowHowToAI:Validation:HierarchyDepthWarning muss zwischen 2 und 256 liegen, ist aber {v.HierarchyDepthWarning}.");
    }

    private static void ValidateRetrieval(RetrievalOptions r, List<string> errors)
    {
        if (r.MaximumPageSize is < 1 or > 1000)
            errors.Add($"KnowHowToAI:Retrieval:MaximumPageSize muss zwischen 1 und 1000 liegen, ist aber {r.MaximumPageSize}.");
        if (r.DefaultPageSize < 1 || r.DefaultPageSize > r.MaximumPageSize)
            errors.Add($"KnowHowToAI:Retrieval:DefaultPageSize muss zwischen 1 und MaximumPageSize ({r.MaximumPageSize}) liegen, ist aber {r.DefaultPageSize}.");
        if (r.SearchMaximumPageSize is < 1 or > 500)
            errors.Add($"KnowHowToAI:Retrieval:SearchMaximumPageSize muss zwischen 1 und 500 liegen, ist aber {r.SearchMaximumPageSize}.");
        if (r.SearchPageSize < 1 || r.SearchPageSize > r.SearchMaximumPageSize)
            errors.Add($"KnowHowToAI:Retrieval:SearchPageSize muss zwischen 1 und SearchMaximumPageSize ({r.SearchMaximumPageSize}) liegen, ist aber {r.SearchPageSize}.");
        if (r.SnippetMaximumCharacters is < 50 or > 4000)
            errors.Add($"KnowHowToAI:Retrieval:SnippetMaximumCharacters muss zwischen 50 und 4000 liegen, ist aber {r.SnippetMaximumCharacters}.");
    }

    private static void ValidateStorage(StorageOptions s, List<string> errors)
    {
        if (s.CommandTimeoutSeconds is < 1 or > 600)
            errors.Add($"KnowHowToAI:Storage:CommandTimeoutSeconds muss zwischen 1 und 600 liegen, ist aber {s.CommandTimeoutSeconds}.");
    }

    private static void ValidateMigrations(MigrationOptions m, List<string> errors)
    {
        if (m.LockTimeoutSeconds is < 1 or > 600)
            errors.Add($"KnowHowToAI:Migrations:LockTimeoutSeconds muss zwischen 1 und 600 liegen, ist aber {m.LockTimeoutSeconds}.");
    }
}


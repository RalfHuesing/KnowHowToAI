using System.Reflection;
using Dapper;
using KnowHowToAI.Core.Sync;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Core.Migrations;

// Kein DbUp/Journal-Tabelle: sql-scripts/*.sql sind selbst idempotent (IF NOT EXISTS-Guards,
// siehe docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 1) und laufen bei jedem Import
// erneut. Ein simpler Runner reicht dafür aus. Einschränkung dadurch: kein `GO`-Batch-Separator-
// Support — jedes Skript muss ein einzelner Batch bleiben.
public static class SchemaMigrator
{
    private const string TableNamePlaceholder = "{{DocumentsTableName}}";

    public static async Task MigrateAsync(
        string connectionString,
        string documentsTableName,
        Action<string> logInformation,
        CancellationToken cancellationToken)
    {
        SqlIdentifierValidator.EnsureValid(documentsTableName);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var script in DiscoverScripts(documentsTableName))
        {
            logInformation($"Führe SQL-Skript aus: {script.Name}");
            await connection.ExecuteAsync(new CommandDefinition(script.Sql, cancellationToken: cancellationToken));
        }
    }

    public static IReadOnlyList<SqlScript> DiscoverScripts(string documentsTableName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return [.. assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => ReadScript(assembly, name, documentsTableName))];
    }

    private static SqlScript ReadScript(Assembly assembly, string resourceName, string documentsTableName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd().Replace(TableNamePlaceholder, documentsTableName, StringComparison.Ordinal);
        return new SqlScript(resourceName, sql);
    }
}

public sealed record SqlScript(string Name, string Sql);

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KnowHowToAI.Storage.SqlServer.Migrations;

/// <summary>
/// Lädt eingebettete SQL-Migrationsskripte aus dem Assembly, normalisiert den Inhalt
/// auf UTF-8/LF und berechnet deterministisch SHA-256-Checksums.
/// Bootstrap-Skript 0000 wird separat bereitgestellt und nicht in die versionierte Liste aufgenommen.
/// </summary>
internal sealed partial class EmbeddedMigrationCatalog
{
    private static readonly Regex VersionPattern = VersionPatternRegex();

    /// <summary>Versionierte Migrationsskripte in strikt aufsteigender Versionsreihenfolge.</summary>
    public IReadOnlyList<MigrationScript> Scripts { get; }

    /// <summary>Bootstrap-Skript (0000) – reentranter, selbst nicht journalisierter Lauf.</summary>
    public MigrationScript BootstrapScript { get; }

    public EmbeddedMigrationCatalog()
        : this(LoadFromAssembly(typeof(EmbeddedMigrationCatalog).Assembly))
    {
    }

    internal EmbeddedMigrationCatalog(IReadOnlyCollection<MigrationScript> allScripts)
    {
        ArgumentNullException.ThrowIfNull(allScripts);
        ValidateScripts(allScripts);

        var bootstrapScripts = allScripts.Where(script => script.Version == 0).ToArray();
        if (bootstrapScripts.Length != 1)
            throw new InvalidOperationException("Es muss genau ein Bootstrap-Skript mit Version 0000 geben.");

        BootstrapScript = bootstrapScripts[0];

        var versioned = allScripts
            .Where(s => s.Version > 0)
            .OrderBy(s => s.Version)
            .ToArray();

        var duplicate = versioned
            .GroupBy(s => s.Version)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Doppelte Migrationsversionsnummer {duplicate.Key} gefunden: " +
                string.Join(", ", duplicate.Select(s => s.Name)));
        }

        Scripts = Array.AsReadOnly(versioned);
    }

    private static List<MigrationScript> LoadFromAssembly(Assembly assembly)
    {
        var results = new List<MigrationScript>();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                continue;

            // MSBuild kodiert Verzeichnistrenner als Punkte im Ressourcennamen.
            // Format: "KnowHowToAI.Storage.SqlServer.SqlScripts.NNNN_name.sql"
            // Der Dateiname beginnt am NNNN_-Muster.
            var fileNameMatch = VersionPattern.Match(resourceName);
            if (!fileNameMatch.Success)
                continue;

            var fileName = fileNameMatch.Groups["fileName"].Value;
            var version = int.Parse(fileNameMatch.Groups["version"].Value, System.Globalization.CultureInfo.InvariantCulture);

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Ressource '{resourceName}' konnte nicht geöffnet werden.");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var rawContent = reader.ReadToEnd();
            results.Add(MigrationScript.Create(version, fileName, rawContent));
        }

        return results;
    }

    /// <summary>Normalisiert Zeilenenden auf LF (deterministisch für Checksum-Berechnung).</summary>
    internal static string NormalizeLineEndings(string content) =>
        content.Replace("\r\n", "\n", StringComparison.Ordinal)
               .Replace('\r', '\n');

    /// <summary>Berechnet SHA-256 über den UTF-8-kodierten, LF-normalisierten Inhalt.</summary>
    internal static byte[] ComputeSha256(string normalizedContent)
    {
        var bytes = Encoding.UTF8.GetBytes(normalizedContent);
        return SHA256.HashData(bytes);
    }

    private static void ValidateScripts(IReadOnlyCollection<MigrationScript> scripts)
    {
        if (scripts.Any(script => script.Version < 0))
            throw new InvalidOperationException("Migrationsversionen dürfen nicht negativ sein.");

        if (scripts.Any(script => string.IsNullOrWhiteSpace(script.Name)))
            throw new InvalidOperationException("Migrationsskripte benötigen einen nicht leeren Namen.");

        if (scripts.Any(script => script.ChecksumSha256.Length != 32))
            throw new InvalidOperationException("Migrationsskripte benötigen eine SHA-256-Checksum mit 32 Byte.");

        var duplicateName = scripts.GroupBy(script => script.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
            throw new InvalidOperationException($"Doppelter Migrationsname '{duplicateName.Key}' gefunden.");
    }

    [GeneratedRegex(@"(?:^|\.)(?<fileName>(?<version>\d{4})_[^.]+\.sql)$", RegexOptions.ExplicitCapture)]
    private static partial Regex VersionPatternRegex();
}

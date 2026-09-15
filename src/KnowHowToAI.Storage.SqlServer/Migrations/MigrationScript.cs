namespace KnowHowToAI.Storage.SqlServer.Migrations;

/// <summary>
/// Stellt ein eingebettetes SQL-Migrationsskript dar.
/// Inhalt ist UTF-8/LF-normalisiert; Checksum ist deterministisch über diesen Inhalt berechnet.
/// </summary>
internal sealed record MigrationScript
{
    /// <summary>Numerische Versionsnummer (aus dem Dateinamenpräfix NNNN).</summary>
    public required int Version { get; init; }

    /// <summary>Vollständiger Dateiname (z. B. "0001_create_system_and_snapshots.sql").</summary>
    public required string Name { get; init; }

    /// <summary>LF-normalisierter SQL-Inhalt.</summary>
    public required string Content { get; init; }

    /// <summary>SHA-256-Checksum über den UTF-8-kodierten, LF-normalisierten Inhalt.</summary>
    public required byte[] ChecksumSha256 { get; init; }

    internal static MigrationScript Create(int version, string name, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(content);

        var normalizedContent = EmbeddedMigrationCatalog.NormalizeLineEndings(content);
        return new MigrationScript
        {
            Version = version,
            Name = name,
            Content = normalizedContent,
            ChecksumSha256 = EmbeddedMigrationCatalog.ComputeSha256(normalizedContent),
        };
    }
}

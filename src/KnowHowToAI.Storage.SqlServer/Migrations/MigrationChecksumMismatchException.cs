namespace KnowHowToAI.Storage.SqlServer.Migrations;

/// <summary>
/// Wird geworfen, wenn ein bereits im Journal erfasster Migrationsskript eine abweichende
/// SHA-256-Checksum aufweist. Deutet auf eine nachträgliche Änderung eines angewendeten Skripts hin.
/// Fehlercode: MigrationChecksumMismatch
/// </summary>
internal sealed class MigrationChecksumMismatchException : Exception
{
    public const string ErrorCode = "MigrationChecksumMismatch";

    public string ScriptName { get; }
    public int Version { get; }

    public MigrationChecksumMismatchException(string scriptName, int version)
        : base($"Migrationsskript '{scriptName}' (Version {version}) wurde verändert: " +
               "die SHA-256-Checksum weicht vom Journal ab. " +
               "Bereits angewendete Migrationsskripte dürfen nicht verändert werden.")
    {
        ScriptName = scriptName;
        Version = version;
    }
}

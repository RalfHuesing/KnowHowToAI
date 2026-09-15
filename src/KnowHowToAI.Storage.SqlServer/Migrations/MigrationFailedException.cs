namespace KnowHowToAI.Storage.SqlServer.Migrations;

/// <summary>
/// Wird geworfen, wenn ein Migrationsskript fehlschlägt. Enthält Skriptname und Version,
/// aber keine Connection-String-Daten oder Credentials.
/// Fehlercode: MigrationFailed
/// </summary>
internal sealed class MigrationFailedException : Exception
{
    public const string ErrorCode = "MigrationFailed";

    public string ScriptName { get; }
    public int Version { get; }
    public int? DatabaseErrorNumber { get; }

    public MigrationFailedException(string scriptName, int version, int? databaseErrorNumber)
        : base($"Migration '{scriptName}' (Version {version}) fehlgeschlagen. " +
               "Weder Journal-Eintrag noch Teilschema wurden hinterlassen.")
    {
        ScriptName = scriptName;
        Version = version;
        DatabaseErrorNumber = databaseErrorNumber;
    }
}

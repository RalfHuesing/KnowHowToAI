# KnowHowTo AI – SQL-Skripte

Dieses Verzeichnis enthält die relationalen Datenbankschema- und Initialisierungsskripte für KnowHowTo AI.

## Ziel-Datenbanksystem
- **Microsoft SQL Server 2019 (15.x) oder neuer** (inklusive Azure SQL Database / Managed Instance).

## Konventionen & Architektur
1. **Tabellen-Präfix**: Alle Tabellen tragen das Präfix `KnowHowToAI_`.
2. **Snapshot-Isolation & Versionierung**:
   - Versionierte Tabellen (`Node`, `NodeContent`, `Role`, `RoleResolution`, `ContentDependency`) besitzen `SnapshotId` als Bestandteil ihres Primärschlüssels.
   - Snapshots sind nach dem Commit vollständig unveränderlich.
   - Löschungen erfolgen fachlich über Soft-Delete (`IsDeleted = 1`), niemals physisch.
3. **Datentypen**:
   - Primäre Identitäten (`NodeId`, `TransactionId`, `ContentRevisionId`): `UNIQUEIDENTIFIER`.
   - Snapshot-Identitäten (`SnapshotId`, `ReleaseId`): `BIGINT`.
   - Markdown-Inhalte: `NVARCHAR(MAX)` (UTF-16/Unicode).
   - Datums-/Zeitstempel: `DATETIME2(7)` in UTC (`SYSUTCDATETIME()`).
4. **Idempotenz**:
   - Jedes Skript ist idempotent aufgebaut (`IF OBJECT_ID(...) IS NULL`).
   - Die Skripte werden strikt in numerischer Reihenfolge ausgeführt.

## Skript-Reihenfolge
- `0001_create_system_and_snapshots.sql`: Systemstatus, Snapshots, Transaktionen und Releases.
- `0002_create_roles.sql`: Rollen und Role Resolution Orders (versioniert pro Snapshot).
- `0003_create_nodes_and_content.sql`: Node-Hierarchie, Node-Content und Content-Abhängigkeiten.
- `0004_seed_initial_state.sql`: Initialer Snapshot 1 (`Committed`), SystemState und Basiseintrag für Rolle `Default`.

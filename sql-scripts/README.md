# KnowHowToAI – SQL-Skripte

Dieses Verzeichnis enthält die relationalen Datenbankschema- und Initialisierungsskripte für KnowHowToAI.

## Ziel-Datenbanksystem
- **Microsoft SQL Server 2019 (15.x) oder neuer** (inklusive Azure SQL Database / Managed Instance).

## Konventionen & Architektur
1. **Tabellen-Präfix**: Alle Tabellen tragen das Präfix `KnowHowToAI_`.
2. **Snapshot-Isolation & Versionierung**:
   - Versionierte Tabellen (`Node`, `NodeContent`, `Role`, `RoleResolution`, `ContentDependency`) besitzen `SnapshotId` als Bestandteil ihres Primärschlüssels.
   - Snapshots sind nach dem Commit vollständig unveränderlich.
   - Fachobjekte mit stabiler Identität werden über Soft-Delete (`IsDeleted = 1`) entfernt.
   - Reine Zuordnungen (`RoleResolution`, `ContentDependency`) dürfen nur innerhalb eines Working Snapshots atomar ersetzt werden; der Base Snapshot erhält die vorherige Fassung.
3. **Datentypen**:
   - Primäre Identitäten (`NodeId`, `TransactionId`, `ContentRevisionId`): `UNIQUEIDENTIFIER`.
   - Snapshot-Identitäten (`SnapshotId`, `ReleaseId`): `BIGINT`.
   - Rollen-IDs: `NVARCHAR(50)` mit binärer, case-sensitiver Collation.
   - Markdown-Inhalte: `NVARCHAR(MAX)` (UTF-16/Unicode).
   - Datums-/Zeitstempel: `DATETIME2(7)` in UTC (`SYSUTCDATETIME()`).
4. **Migrationen und Idempotenz**:
   - `0000_bootstrap_schema_migrations.sql` ist das einzige reentrante Bootstrap-Skript. Es legt das Migrationsjournal an und wird selbst nicht darin erfasst.
   - Die versionierten Skripte ab `0001` werden durch den Migration Runner exakt einmal und strikt in numerischer Reihenfolge innerhalb kurzer SQL-Transaktionen ausgeführt.
   - Das Journal mit SHA-256-Checksums ist die Idempotenzquelle. Die Skripte verschleiern ein vorhandenes oder abweichendes Schema nicht mit `IF OBJECT_ID`.
   - Bis zum ersten unterstützten Datenbankstand darf die Greenfield-Baseline direkt korrigiert werden. Danach werden angewendete Skripte nicht verändert; jede Änderung erhält eine neue Versionsnummer.
   - DDL- und Seed-Skripte setzen die von SQL Server für gefilterte Indizes verlangten Sessionoptionen explizit. Schreibende Anwendungsverbindungen müssen diese Standardoptionen ebenfalls beibehalten.
5. **Datenbank- und Domain-Verantwortung**:
   - Relationale Constraints sichern lokal beweisbare Invarianten wie Zustände, Eindeutigkeiten, Fremdschlüssel und Self-References.
   - Snapshot-übergreifende oder graphbasierte Regeln wie committed Current Snapshot, aktive Referenzen, lückenlose Sortierung und Zyklenfreiheit bleiben zusätzlich Aufgabe der Domain-/Application-Validierung.

## Skript-Reihenfolge
- `0000_bootstrap_schema_migrations.sql`: Reentrantes Bootstrap für `KnowHowToAI_SchemaMigration`; nicht Teil der versionierten Migrationen.
- `0001_create_system_and_snapshots.sql`: Systemstatus, Snapshots, Transaktionen und Releases.
- `0002_create_roles.sql`: Rollen und Role Resolution Orders (versioniert pro Snapshot).
- `0003_create_nodes_and_content.sql`: Node-Hierarchie, Node-Content und Content-Abhängigkeiten.
- `0004_seed_initial_state.sql`: Initialer leerer committed Snapshot, SystemState und Basiseintrag für Rolle `Default`; keine fest verdrahtete Snapshot-ID.

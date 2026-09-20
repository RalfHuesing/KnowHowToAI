# Datenmodell

## Tabellen

Alle Tabellen tragen den Präfix `KnowHowToAI_` im Schema `dbo`:

| Tabelle | Inhalt |
|---|---|
| `KnowHowToAI_SchemaMigration` | Migrationsjournal (Skript, Checksum, angewendet am) |
| `KnowHowToAI_Snapshot` | Snapshots mit `State` (Working/Committed/Discarded) und Zeitstempeln |
| `KnowHowToAI_SystemState` | Systemzustand, referenziert `CurrentSnapshotId` |
| `KnowHowToAI_Transaction` | Transaktionsmetadaten |
| `KnowHowToAI_Release` | benannte, unveränderliche Verweise auf committed Snapshots |
| `KnowHowToAI_Role` | Zielgruppen (`RoleId` NVARCHAR, binäre Collation) |
| `KnowHowToAI_RoleResolution` | Resolution Orders (Kandidatenrolle + Priorität pro Snapshot) |
| `KnowHowToAI_Node` | Hierarchieknoten pro Snapshot |
| `KnowHowToAI_NodeContent` | rollenabhängiger Content pro Snapshot |
| `KnowHowToAI_ContentDependency` | Provenienz-Abhängigkeiten pro Snapshot |

## Datentypkonventionen

- Primäre Identitäten (`NodeId`, `TransactionId`, `ContentRevisionId`):
  `UNIQUEIDENTIFIER` (GUIDs).
- Snapshot-Identitäten (`SnapshotId`, `ReleaseId`): `BIGINT`.
- Zielgruppen-IDs: `NVARCHAR(50)` mit binärer, case-sensitiver Collation
  (`COLLATE Latin1_General_100_BIN2`) – `N'Default'` und `'default'` sind
  verschiedene Rollen.
- Markdown-Inhalte: `NVARCHAR(MAX)`.
- Zeitstempel: `DATETIME2(7)` in UTC (`SYSUTCDATETIME()`).

## Snapshot-Schlüssel

Versionierte Tabellen (`Node`, `NodeContent`, `Role`, `RoleResolution`,
`ContentDependency`) besitzen `SnapshotId` als Bestandteil ihres
Primärschlüssels. Beispiel `KnowHowToAI_Node`:

```text
SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
```

Der logische Primärbezug eines Objekts bleibt seine fachliche ID (`NodeId`);
die gespeicherte Ausprägung gehört zu einem konkreten Snapshot.

## NodeContent

```text
SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
```

`ContentMode` ist `Independent` oder `Derived` ([Zielgruppen und
Content](Zielgruppen-und-Content.md)). Fallback benötigt keinen duplizierten
Content-Datensatz – Fallback ist ein Ergebnis der Role Resolution zur Lesezeit.

## ContentDependency

```text
SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId
```

Damit ist prüfbar, ob eine abgeleitete Darstellung noch auf den aktuellen
Source-Revisions basiert.

## Transaktionsmetadaten

`KnowHowToAI_Transaction` besitzt mindestens:

```text
TransactionId, BaseSnapshotId, WorkingSnapshotId, State, CreatedAt, CommittedAt, ChangeVersion
```

`ChangeVersion` beginnt bei 0 und wird bei jeder erfolgreichen Änderung des
Working Snapshots atomar erhöht. Paginierte Reads erkennen daran, dass ein Cursor
nach einer zwischenzeitlichen Mutation nicht mehr zum selben Arbeitsstand gehört
(`CursorExpired`). Optionale Metadaten `Purpose`, `Actor`, `Client`,
`CommitMessage` erleichtern Audit und Analyse, sind aber keine Voraussetzung der
fachlichen Semantik.

V1 erhält mindestens den Zustand vor und nach einer Transaction (Base- versus
Committed Snapshot) und leitet daraus den Netto-Unterschied ab. Ein vollständiges
Operation-Log jedes einzelnen Schreibaufrufs existiert nicht; ein append-only Log
wäre eine spätere Erweiterung ([Entscheidungen](Entscheidungen.md)).

## Migrationen

Die Migrations-Skripte liegen unter `sql-scripts/` (`0000` Bootstrap/Migrationsjournal,
`0001` Snapshots/SystemState/Transaction/Release, `0002` Rollen, `0003` Nodes/Content,
`0004` Seed des initialen Zustands inklusive Zielgruppe `Default` und ihrer Resolution
Order). Der Runner arbeitet mit:

- Skriptkatalog mit Checksums (`MigrationChecksumMismatch` bei Abweichung),
- Journal pro Datenbank, Anwendung genau einmal,
- Locking gegen konkurrierende Runner (`LockTimeoutSeconds`),
- atomarem Fehler (`MigrationFailed`), der den Serverstart verhindert,
- optionaler Automatik beim Start (`Migrations:ApplyOnStartup`).

Angewendete Migrationsskripte sind unveränderlich; Änderungen erfolgen ausschließlich
durch neue, vorwärts gerichtete Migrationen mit neuer Versionsnummer. Vor dem
ersten unterstützten Datenbankstand darf die Greenfield-Baseline direkt korrigiert
werden.

Die Datenbank wird manuell bereitgestellt und als gegeben angenommen
(`sql-scripts/README.md`); der SQL-Integrationstest-Harness führt niemals
`CREATE DATABASE` oder `DROP DATABASE` aus.

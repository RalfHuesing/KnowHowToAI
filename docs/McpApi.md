# MCP-API

Der MCP-Server stellt 27 Tools über MCP STDIO bereit. Alle IDs (`NodeId`, `RoleId`,
`TransactionId`, `SnapshotId`) sind symmetrisch: Ausgaben sind ohne Bereinigung
oder Typkonvertierung als Eingabe für Folgetools nutzbar (Round-Trip-Garantie).
Rollen werden immer explizit als `roleId` übergeben; es gibt keinen globalen
Session-Rollenstatus.

## Tool-Inventar

| Gruppe | Tools |
|---|---|
| Transactions | `begin_transaction`, `get_transaction`, `validate_transaction`, `commit_transaction`, `discard_transaction` |
| Navigation (read-only) | `get_root`, `get_node`, `list_children`, `list_roles` |
| Retrieval (read-only) | `search`, `export_tree` |
| Struktur (write) | `create_node`, `update_node`, `move_node`, `reorder_node`, `delete_node` |
| Rollen (write) | `create_role`, `update_role`, `delete_role`, `set_role_resolution` |
| Content (write) | `replace_content`, `replace_text`, `delete_content` |
| Historie (read-only) | `get_snapshot`, `list_releases`, `compare_snapshots`, `get_transaction_changes` |
| Releases (Metadaten) | `create_release` |

## Selektor-Regeln

- **Jeder Schreib-Tool** erfordert eine offene KnowHowTo-AI-Transaction
  (`transactionId`) und verändert ausschließlich deren Working Snapshot. Die
  einzige Ausnahme ist `create_release` (reine Metadatenverwaltung).
- **Read-Tools** verwenden die gemeinsamen Selektor-Felder `transactionId`
  (optionaler String), `snapshotId` (optionaler String) und `includeDeleted`
  (optionales Boolean, Standard `false`). Gelesen wird ohne Selektor aus dem
  aktuellen committed Snapshot, mit `transactionId` aus dem Working Snapshot, mit
  `snapshotId` aus dem historischen Snapshot. Beide Selektoren zugleich sind
  unzulässig und führen zu `InvalidReadContext`.
- Ein nicht als GUID („D“-Format) parsebarer Node-ID-String (`nodeId`, `parentNodeId`,
  `rootNodeId`) ist ein Parameterfehler und führt zu `InvalidNodeId` mit Parametername
  und Rohwert in `details`. Nicht parsebare Selektor-Strings (`transactionId`,
  `snapshotId`) und Transaction-/Snapshot-IDs der Mutation- und Historie-Tools können
  keine existierende Entität bezeichnen und führen deterministisch zum passenden
  `…NotFound`-Fehler mit dem Rohwert in `details`.

## Antwort-Envelope

Jedes Tool gibt genau ein JSON-Objekt zurück. Erfolg und Fehler unterscheiden sich
ausschließlich über `code`; Feldnamen sind im Server-Code über JSON-Attribute
fixiert und damit unabhängig von Serialisierungsoptionen camelCase.

| Feld | JSON-Name | Typ | Nullability |
|---|---|---|---|
| Ergebniscode | `code` | string | immer gesetzt: `Success` oder stabiler Fehlercode aus dem Katalog |
| Meldung | `message` | string | nur bei Fehlern gesetzt und dort erforderlich; bei `Success` nie vorhanden |
| Details | `details` | Objekt string → string | nur bei Fehlern gesetzt, wenn strukturierte Zusatzdaten vorliegen |
| Warnungen | `warnings` | Array | nur gesetzt, wenn mindestens eine Warnung vorliegt; bei Erfolg und Fehler möglich |
| Daten | `data` | Objekt | nur bei Erfolg gesetzt, wenn das Tool einen Payload liefert |

Regeln:

- Leere `warnings` und `details` sowie nicht vorhandene Werte werden weggelassen,
  nie als `null` serialisiert.
- Warnungen tragen stabile Warncodes aus dem Katalog und sind keine Fehler.
- `limit` gilt einheitlich für Listen-, Search- und Diff-Tools (Semantik:
  [Retrieval](Retrieval.md)).

Beispiele (in Vertragstests fixiert):

```json
{
  "code": "Success",
  "data": { "transactionId": "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90" },
  "warnings": [
    {
      "code": "NodeTooLarge",
      "message": "Der normalisierte Content übersteigt die Warnschwelle.",
      "details": { "actualBytes": "5123", "thresholdBytes": "4096" }
    }
  ]
}
```

```json
{
  "code": "TransactionNotFound",
  "message": "Die angefragte Transaction existiert nicht.",
  "details": { "transactionId": "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90" }
}
```

## Request-/Response-Felder der Transaktions-Tools

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `begin_transaction` | `purpose` (optional), `actor` (optional), `client` (optional) | `transactionId`, `baseSnapshotId`, `workingSnapshotId`, `state`, `createdAtUtc`; optional `committedAtUtc`, `purpose`, `actor`, `client`, `commitMessage` |
| `get_transaction` | `transactionId` (erforderlich) | dieselben Feldnamen wie `begin_transaction` |
| `validate_transaction` | `transactionId` (erforderlich) | `isValid`, `errors` (je `code`, `message`, optionales `details`), `warnings` (McpWarning), `staleContents` (je `nodeId`, `roleId`, `contentRevisionId`), `refactoringCandidates` (je `nodeId`, `reasonCodes`) |
| `commit_transaction` | `transactionId` (erforderlich), `commitMessage` (optional) | dieselben Feldnamen wie `begin_transaction`; Validierungsbefunde erscheinen zusätzlich als `warnings` auf Envelope-Ebene, auch bei fachlicher Ablehnung |
| `discard_transaction` | `transactionId` (erforderlich) | kein `data` |

Regeln:

- `validate_transaction` ist read-only und wiederholbar; harte Fehlerbefunde
  erscheinen als Erfolg mit `isValid: false` im Payload.
- Fehlende, geschlossene oder nicht mehr offene Transactions liefern stabil
  `TransactionNotFound` beziehungsweise `TransactionClosed`; ein Commit bei
  zwischenzeitlich fortgeschriebenem Current Snapshot liefert `SnapshotConflict`
  mit `baseSnapshotId` und `currentSnapshotId` in den Details.

## Request-/Response-Felder der Read-Tools

Alle Read-Tools verwenden die Selektor-Felder und die einheitliche
`limit`/`cursor`-Paging-Semantik.

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `get_root` | `roleId` (erforderlich), Selektor-Felder | Node-Felder: `nodeId`, `title`, optional `description`, `sortOrder`, `requestedRole`, optional `resolvedRole`, `fallbackUsed`, `availability`, `freshness`, optional `contentRevisionId` und `content`; ohne Root kein `data` |
| `get_node` | `nodeId`, `roleId` (erforderlich), Selektor-Felder | dieselben Node-Felder wie `get_root` |
| `list_children` | `roleId` (erforderlich), optional `parentNodeId`, Selektor-Felder, `limit`, `cursor` | optional `parentNodeId`, `items` (je `nodeId`, `title`, optional `description`, `sortOrder`, `childCount`, `contentSizeBytes`, `availability`, optional `resolvedRole`, `freshness`), optional `nextCursor` |
| `list_roles` | Selektor-Felder, `limit`, `cursor` | `items` (je `roleId`, `name`, optional `description`), optional `nextCursor` |
| `search` | `text` (erforderlich), optional `roleId`, Selektor-Felder, `limit`, `cursor` | `query`, `items` (je `nodeId`, `title`, optional `description`, optional `snippet`, `hitField`, `availability`, optional `resolvedRole`, `freshness`), optional `nextCursor` |
| `export_tree` | `rootNodeId`, `roleId` (erforderlich), Selektor-Felder | `markdown`; Export-Warnungen (`HierarchyTooDeep`, `StaleDerivedContent`) erscheinen als `warnings` auf Envelope-Ebene |

## Request-/Response-Felder der Struktur-Tools

Alle Struktur-Tools ändern ausschließlich den Working Snapshot der offenen
Transaction; `delete_node` wirkt global über alle Rollen
([Transaktionen und Historie](Transaktionen-und-Historie.md)).

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `create_node` | `transactionId`, `title` (erforderlich), optional `description`, optional `parentNodeId` (ohne Wert wird eine Root-Node angelegt), optional `sortOrder` (Standard 0) | `nodeId`, optional `parentNodeId`, `title`, `snapshotId`, `changeVersion`, `affectedNodeIds` |
| `update_node` | `transactionId`, `nodeId`, `title` (erforderlich), optional `description` | dieselben Feldnamen wie `create_node` |
| `move_node` | `transactionId`, `nodeId`, `sortOrder` (erforderlich), optional `parentNodeId` (ohne Wert wird die Node zur Root-Node) | dieselben Feldnamen wie `create_node` |
| `reorder_node` | `transactionId`, `nodeId`, `sortOrder` (erforderlich) | dieselben Feldnamen wie `create_node` |
| `delete_node` | `transactionId`, `nodeId` (erforderlich), optional `deleteSubtree` (Standard `false`) | dieselben Feldnamen wie `create_node` |

`affectedNodeIds` umfasst die geänderte Node und alle durch
Sortiernormalisierung oder Verschieben betroffenen aktiven Nachfahren. Unbekannte
IDs führen zu `NodeNotFound` beziehungsweise `ParentNodeNotFound` mit dem Rohwert
in den Details; nicht parsebare ID-Strings sind Parameterfehler und führen zu
`InvalidNodeId`.

## Request-/Response-Felder der Rollen-Tools

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `create_role` | `transactionId`, `name` (erforderlich; er bestimmt den `roleId`), optional `description` | `roleId`, `name`, optional `description` |
| `update_role` | `transactionId`, `roleId`, `name` (erforderlich), optional `description` | dieselben Feldnamen wie `create_role` |
| `delete_role` | `transactionId`, `roleId` (erforderlich) | dieselben Feldnamen wie `create_role` |
| `set_role_resolution` | `transactionId`, `roleId`, `candidateRoleIds` (erforderlich; die Reihenfolge bestimmt die Priorität, 1 = höchste) | `requestedRoleId`, `items` (je `candidateRoleId`, `priority`) |

Regeln: `set_role_resolution` ersetzt die Order vollständig und bleibt nicht
rekursiv ([Rollen und Content](Rollen-und-Content.md)). Eine referenzierte Rolle
(Content, Dependencies, Resolution Orders) kann nicht gelöscht werden
(`RoleInUse`). Doppelte Kandidaten führen zu `DuplicateCandidateRole`, unbekannte
zu `CandidateRoleNotFound`.

## Request-/Response-Felder der Content-Tools

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `replace_content` | `transactionId`, `nodeId`, `roleId`, `contentMode` (erforderlich; exakt `Independent` oder `Derived`), `contentMd` (erforderlich), optional `sources` (je `nodeId`, `roleId`, `contentRevisionId`) | `nodeId`, `roleId`, `contentRevisionId`, `contentMode`, `freshness`, `snapshotId`, `changeVersion` |
| `replace_text` | `transactionId`, `nodeId`, `roleId`, `oldText`, `newText` (erforderlich) | dieselben Feldnamen wie `replace_content` |
| `delete_content` | `transactionId`, `nodeId`, `roleId` (erforderlich) | dieselben Feldnamen wie `replace_content` |

Regeln:

- Content-Tools verändern ausschließlich den expliziten Rollen-Content der
  übergebenen `roleId` und lassen die Node unverändert; `delete_content`
  tombstoned per Soft-Delete.
- Ein unbekannter `contentMode`-Wert ist eine harte Dependency-Verletzung und
  führt zu `InvalidDependency`; derselbe Code gilt für nicht parsebare
  Source-Revisions.
- Markdown- oder HTML-Überschriften im `contentMd` sind harte Fehler
  (`HeadingNotAllowed`), heuristisch erkennbare Ersatztitel liefern die Warnung
  `PossibleEmbeddedHeading` auf Envelope-Ebene.
- `replace_text` verlangt exakt einen ordinalen Treffer: `TextNotFound`,
  `MultipleTextMatches`.
- Bei Erfolg wird der Content-Text nicht zurückgegeben; die gültige Version ist
  über `get_node` mit der gelieferten `contentRevisionId` abrufbar.

## Request-/Response-Felder der Historien-Tools

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `get_snapshot` | `snapshotId` (erforderlich) | `snapshotId`, `state`, `createdAtUtc`, optional `baseSnapshotId`, optional `committedAtUtc` |
| `list_releases` | `limit`, `cursor` | `items` (je `releaseId`, `snapshotId`, `name`, `releasedAtUtc`, optional `description`), optional `nextCursor` |
| `compare_snapshots` | `baseSnapshotId`, `targetSnapshotId`, `limit`, `cursor` | paginierte Diff-Kategorien ([Transaktionen und Historie](Transaktionen-und-Historie.md)) |
| `get_transaction_changes` | `transactionId`, `limit`, `cursor` | paginierte Diff-Kategorien gegen Base/Working beziehungsweise Base/Committed |
| `create_release` | `name` (erforderlich), `snapshotId` (erforderlich), optional `description` | `releaseId`, `snapshotId`, `name`, `releasedAtUtc`, optional `description` |

`create_release` benötigt keine Transaction
([Transaktionen und Historie](Transaktionen-und-Historie.md)).

<!-- mcp-catalog-start -->
## Stabiler Fehlercode-Katalog

Der Katalog ist zentral im Server-Code angelegt (Klasse McpErrorCatalog) und wird
von einem Synchronisationstest gegen diesen Abschnitt geprüft. Neue Codes dürfen
ergänzt werden; veröffentlichte Codes werden nicht beiläufig umbenannt.

- Kontext/Zustand: `InvalidReadContext`, `SnapshotNotFound`, `SnapshotNotCommitted`,
  `TransactionNotFound`, `TransactionClosed`, `SnapshotConflict`, `InvalidCursor`,
  `CursorExpired`, `WorkingSnapshotNotOpen`, `TransactionDiscarded`
- Struktur/Rollen: `NodeNotFound`, `InvalidNodeId`, `RootAlreadyExists`, `ParentNodeNotFound`,
  `InvalidHierarchy`, `NodeHasChildren`, `RoleNotFound`, `RoleInUse`,
  `RoleResolutionNotConfigured`, `InvalidRoleResolution`, `DuplicateNodeId`,
  `HierarchyCycle`, `NodeIdAlreadyUsed`, `SelfParentNotAllowed`, `SnapshotMismatch`,
  `TitleRequired`, `RoleNameRequired`, `RoleIdRequired`, `CandidateRoleDeleted`,
  `CandidateRoleNotFound`, `DuplicateCandidateRole`, `DuplicatePriority`,
  `InvalidPriority`, `RequestedRoleDeleted`, `RequestedRoleNotFound`
- Content: `ExplicitContentNotFound`, `HeadingNotAllowed`, `FrontMatterNotAllowed`,
  `TextNotFound`, `MultipleTextMatches`, `InvalidDependency`, `DependencyCycle`
- Migration/Release: `MigrationChecksumMismatch`, `MigrationFailed`,
  `ReleaseNotFound`, `ReleaseNameConflict`, `ReleaseNameRequired`

Warncodes wie `NodeTooLarge`, `PossibleEmbeddedHeading`, `TooManyChildren`,
`HierarchyTooDeep`, `LargeContentReplace` und `StaleDerivedContent` sind keine
Fehler.
<!-- mcp-catalog-end -->

## STDIO-Protokollverhalten

Der Server läuft als STDIO-Prozess. `stdout` enthält ausschließlich
MCP-Protokollnachrichten; alle Diagnose- und Protokollausgaben gehen nach
`stderr` oder in eine Datei ([Konfiguration und
Betrieb](Konfiguration-und-Betrieb.md)). Für nicht parsebare stdin-Zeilen
garantiert das MCP-SDK keine JSON-RPC-Fehlerantwort (−32700): Parse-Fehler werden
nach stderr geloggt, `stdout` bleibt sauber und der Prozess beantwortet
nachfolgende gültige Requests normal. Ungültige JSON-Typen oder unbekannte Felder
in `arguments` liefern dagegen protokollkonforme Antworten (isError-Result
beziehungsweise JSON-RPC-Error). Das Ende des Client-Eingabestroms, SIGTERM/Ctrl+C
und Schreibfehler auf der Pipe führen zu einem geordneten Herunterfahren mit
stabilem Exitcode.

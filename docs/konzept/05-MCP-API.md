# 61. MCP-API – konzeptionelle V1-Funktionen

Die exakten Tool-Namen und JSON-Schemas werden bei der Implementierung festgelegt.

Konzeptionell benötigt V1 mindestens folgende Funktionsgruppen.

## Transactions

```text
begin_transaction
commit_transaction
discard_transaction
get_transaction
validate_transaction
```

### Verbindliche Request-/Response-Felder der Transaktions-Tools

Alle Feldnamen sind über JSON-Attribute fixiert und camelCase. IDs sind Strings im
Format der Tool-Ausgaben und ohne Umformatierung als Folgeparameter verwendbar.

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `begin_transaction` | `purpose` (optional), `actor` (optional), `client` (optional) | `transactionId`, `baseSnapshotId`, `workingSnapshotId`, `state`, `createdAtUtc`; optional `committedAtUtc`, `purpose`, `actor`, `client`, `commitMessage` |
| `get_transaction` | `transactionId` (erforderlich) | dieselben Feldnamen wie `begin_transaction` |
| `validate_transaction` | `transactionId` (erforderlich) | `isValid`, `errors` (je `code`, `message`, optionales `details`), `warnings` (McpWarning), `staleContents` (je `nodeId`, `roleId`, `contentRevisionId`), `refactoringCandidates` (je `nodeId`, `reasonCodes`) |
| `commit_transaction` | `transactionId` (erforderlich), `commitMessage` (optional) | dieselben Feldnamen wie `begin_transaction`; Validierungsbefunde erscheinen zusätzlich als `warnings` auf Envelope-Ebene, auch bei fachlicher Ablehnung |
| `discard_transaction` | `transactionId` (erforderlich) | kein `data` |

Regeln:

- `validate_transaction` ist read-only, wiederholbar und meldet auch harte
  Fehlerbefunde als Erfolg mit `isValid: false` im Payload; die Befund-Codes
  entsprechen dem Katalog der Roadmap.
- Ein nicht als GUID „D“ parsebarer `transactionId` kann keine existierende
  Transaction bezeichnen und führt deterministisch zu `TransactionNotFound` mit dem
  Rohwert unter `details.transactionId`.
- Fehlende, geschlossene oder nicht mehr offene Transactions liefern stabile
  `TransactionNotFound`- beziehungsweise `TransactionClosed`-Fehler; ein Commit bei
  zwischenzeitlich fortgeschriebenem Current Snapshot liefert `SnapshotConflict`
  mit `baseSnapshotId` und `currentSnapshotId` in den Details.

## Navigation und Lesen

```text
get_root
get_node
list_children
list_roles
search
```

### Verbindliche Request-/Response-Felder der Read-Tools

Alle Read-Tools verwenden die gemeinsamen Selektor-Felder `transactionId`,
`snapshotId` und `includeDeleted` gemäß Abschnitt 64 sowie die einheitliche
`limit`/`cursor`-Paging-Semantik. Rollen werden explizit als `roleId` übergeben.

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `get_root` | `roleId` (erforderlich), Selektor-Felder | Node-Felder: `nodeId`, `title`, optional `description`, `sortOrder`, `requestedRole`, optional `resolvedRole`, `fallbackUsed`, `availability`, `freshness`, optional `contentRevisionId` und `content`; ohne Root kein `data` |
| `get_node` | `nodeId`, `roleId` (erforderlich), Selektor-Felder | dieselben Node-Felder wie `get_root` |
| `list_children` | `roleId` (erforderlich), optional `parentNodeId`, Selektor-Felder, `limit`, `cursor` | optional `parentNodeId`, `items` (je `nodeId`, `title`, optional `description`, `sortOrder`, `childCount`, `contentSizeBytes`, `availability`, optional `resolvedRole`, `freshness`), optional `nextCursor` |
| `list_roles` | Selektor-Felder, `limit`, `cursor` | `items` (je `roleId`, `name`, optional `description`), optional `nextCursor` |
| `search` | `text` (erforderlich), optional `roleId`, Selektor-Felder, `limit`, `cursor` | `query`, `items` (je `nodeId`, `title`, optional `description`, optional `snippet`, `hitField`, `availability`, optional `resolvedRole`, `freshness`), optional `nextCursor` |

## Export

```text
export_tree
```

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `export_tree` | `rootNodeId`, `roleId` (erforderlich), Selektor-Felder | `markdown`; Export-Warnungen (`HierarchyTooDeep`, `StaleDerivedContent`) erscheinen als `warnings` auf Envelope-Ebene |

## Strukturänderungen

```text
create_node
update_node
move_node
reorder_node
delete_node
```

### Verbindliche Request-/Response-Felder der Struktur-Tools

Alle Schreib-Tools erfordern eine offene KnowHowTo-AI-Transaction und ändern
ausschließlich deren Working Snapshot (`delete_node` ist global über alle Rollen,
Konzeptmodul 43). Fehlende oder geschlossene Transactions liefern
`TransactionNotFound` beziehungsweise `TransactionClosed`.

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `create_node` | `transactionId` (erforderlich), `title` (erforderlich), optional `description`, optional `parentNodeId` (ohne Wert wird eine Root-Node angelegt), optional `sortOrder` (Standard 0) | `nodeId`, optional `parentNodeId`, `title`, `snapshotId`, `changeVersion`, `affectedNodeIds` |
| `update_node` | `transactionId` (erforderlich), `nodeId`, `title` (erforderlich), optional `description` | dieselben Feldnamen wie `create_node` |
| `move_node` | `transactionId` (erforderlich), `nodeId`, `sortOrder` (erforderlich), optional `parentNodeId` (ohne Wert wird die Node zur Root-Node) | dieselben Feldnamen wie `create_node` |
| `reorder_node` | `transactionId` (erforderlich), `nodeId`, `sortOrder` (erforderlich) | dieselben Feldnamen wie `create_node` |
| `delete_node` | `transactionId` (erforderlich), `nodeId` (erforderlich), optional `deleteSubtree` (Standard `false`) | dieselben Feldnamen wie `create_node` |

Regeln:

- Eine Node mit aktiven Children wird ohne `deleteSubtree: true` mit
  `NodeHasChildren` abgelehnt; die Subtree-Löschung tombstoned Nodes und deren
  Contents atomar.
- `affectedNodeIds` umfasst die geänderte Node und alle durch Sortier­normalisierung
  oder Verschieben betroffenen aktiven Nachfahren.
- unbekannte oder nicht als GUID „D“ parsebare IDs führen zu `NodeNotFound`
  beziehungsweise `ParentNodeNotFound` mit dem Rohwert in den Details.

## Rollen und Resolution Orders

```text
create_role
update_role
delete_role
set_role_resolution
```

### Verbindliche Request-/Response-Felder der Rollen-Tools

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `create_role` | `transactionId` (erforderlich), `name` (erforderlich; er bestimmt den `roleId`), optional `description` | `roleId`, `name`, optional `description` |
| `update_role` | `transactionId` (erforderlich), `roleId`, `name` (erforderlich), optional `description` | dieselben Feldnamen wie `create_role` |
| `delete_role` | `transactionId` (erforderlich), `roleId` (erforderlich) | dieselben Feldnamen wie `create_role` |
| `set_role_resolution` | `transactionId` (erforderlich), `roleId` (erforderlich), `candidateRoleIds` (erforderlich; die Reihenfolge bestimmt die Priorität, 1 = höchste) | `requestedRoleId`, `items` (je `candidateRoleId`, `priority`) |

Regeln:

- `set_role_resolution` ersetzt die bisherige Resolution Order der angefragten
  Rolle vollständig; sie bleibt nicht rekursiv (Konzeptmodul 20).
- eine Rolle, die noch von Content, Dependencies oder Resolution Orders
  referenziert wird, kann nicht gelöscht werden (`RoleInUse`).
- doppelte Kandidaten führen zu `DuplicateCandidateRole`, unbekannte zu
  `CandidateRoleNotFound`; für Content gilt weiterhin Modul 63 (explizite `roleId`).

## Content

```text
replace_content
replace_text
delete_content
```

### Verbindliche Request-/Response-Felder der Content-Tools

| Tool | Request-Felder | Response-Daten (`data`) |
|---|---|---|
| `replace_content` | `transactionId` (erforderlich), `nodeId`, `roleId`, `contentMode` (erforderlich; exakt `Independent` oder `Derived`), `contentMd` (erforderlich), optional `sources` (je `nodeId`, `roleId`, `contentRevisionId`) | `nodeId`, `roleId`, `contentRevisionId`, `contentMode`, `freshness`, `snapshotId`, `changeVersion` |
| `replace_text` | `transactionId` (erforderlich), `nodeId`, `roleId`, `oldText`, `newText` (erforderlich) | dieselben Feldnamen wie `replace_content` |
| `delete_content` | `transactionId` (erforderlich), `nodeId`, `roleId` (erforderlich) | dieselben Feldnamen wie `replace_content` |

Regeln:

- Content-Tools verändern ausschließlich den expliziten Rollen-Content der
  übergebenen `roleId` und lassen die Node unverändert (Konzeptmodul 43);
  `delete_content` tombstoned per Soft-Delete.
- ein unbekannter `contentMode`-Wert ist eine harte Dependency-Verletzung und
  führt zu `InvalidDependency`; derselbe Code gilt für nicht parsebare
  Source-Revisions in `sources`.
- Markdown- oder HTML-Überschriften im `contentMd` sind harte Fehler
  (`HeadingNotAllowed`), heuristisch erkennbare Ersatztitel liefern die Warnung
  `PossibleEmbeddedHeading` auf Envelope-Ebene.
- `replace_text` verlangt exakt einen ordinalen Treffer; 0 Treffer liefern
  `TextNotFound`, mehrere `MultipleTextMatches`.
- bei Erfolg wird der Content-Text nicht zurückgegeben; die gültige Version ist
  über `get_node` mit der gelieferten `contentRevisionId` abrufbar.

## Prüfung

```text
validate_transaction
```

## Historie

V1 enthält mindestens:

```text
get_snapshot
list_releases
compare_snapshots
get_transaction_changes
create_release
```

`create_release` registriert atomar einen unveränderlichen Verweis auf einen bereits
committed Snapshot. Da es keinen versionierten Snapshot-Inhalt verändert, benötigt
diese Metadatenoperation keine KnowHowTo-AI-Transaction.

`compare_snapshots` und `get_transaction_changes` liefern die paginierten Kategorien
in der Reihenfolge aus Modul 46a. Jeder Eintrag enthält `kind`, `entityType` und die
vollständigen fachlichen Schlüsselfelder. Dependency-Einträge identifizieren deshalb
neben Target (`id`, `roleId`) auch Source (`sourceNodeId`, `sourceRoleId`); zwei
Dependencies desselben Targets bleiben im MCP-Vertrag unterscheidbar.

---

# 62. Schreiboperationen benötigen immer TransactionId

Folgendes darf nicht existieren:

```text
update_content(nodeId, ...)
```

ohne Transaktionskontext.

Richtig:

```text
update_content(
    transactionId,
    nodeId,
    ...
)
```

Damit kann kein Agent versehentlich direkt einen committed Zustand verändern.

---

# 63. Rolle wird explizit übergeben

Content-bezogene MCP-Aufrufe erhalten explizit eine Rolle.

Es gibt keinen unsichtbaren globalen Rollenstatus innerhalb einer MCP-Session.

Beispiel:

```text
get_node(
    nodeId,
    roleId
)
```

Dadurch bleiben einzelne Tool-Aufrufe nachvollziehbar und stateless.

---

# 64. Tool-Antworten

MCP-Antworten sollen möglichst strukturiert und maschinenlesbar sein.

Wichtige Daten werden nicht nur in Prosa zurückgegeben.

Beispielsweise:

```text
nodeId
snapshotId
requestedRole
resolvedRole
fallbackUsed
availability
freshness
contentRevisionId
content
warnings
```

Fehler sollen klare, stabile Fehlercodes besitzen.

Beispiele:

```text
TransactionNotFound
TransactionClosed
SnapshotConflict
NodeNotFound
RoleNotFound
HeadingNotAllowed
InvalidHierarchy
TextNotFound
MultipleTextMatches
InvalidDependency
```

### Gemeinsamer Antwort-Envelope

Jedes Tool gibt genau ein JSON-Objekt zurück. Erfolg und Fehler unterscheiden sich
ausschließlich über `code`; die Feldnamen sind im Server-Code über JSON-Attribute
fixiert und damit unabhängig von Serialisierungsoptionen camelCase.

| Feld | JSON-Name | Typ | Nullability |
|---|---|---|---|
| Ergebniscode | `code` | string | immer gesetzt: `Success` oder stabiler Fehlercode aus dem Katalog der Roadmap |
| Meldung | `message` | string | nur bei Fehlern gesetzt und dort erforderlich; bei `Success` nie vorhanden |
| Details | `details` | Objekt string → string | nur bei Fehlern gesetzt, wenn strukturierte Zusatzdaten vorliegen |
| Warnungen | `warnings` | Array | nur gesetzt, wenn mindestens eine Warnung vorliegt; bei Erfolg und Fehler möglich |
| Daten | `data` | Objekt | nur bei Erfolg gesetzt, wenn das Tool einen Payload liefert |

Regeln:

- Leere `warnings` und `details` sowie nicht vorhandene Werte werden weggelassen,
  nie als `null` serialisiert.
- Warnungen tragen stabile Warncodes aus dem Katalog der Roadmap und sind keine
  Fehler.
- Read-Tools verwenden die gemeinsamen Selektor-Felder `transactionId`
  (optionaler String), `snapshotId` (optionaler String) und `includeDeleted`
  (optionales Boolean, Standard `false`). Beide Selektoren zugleich sind unzulässig
  und führen zu `InvalidReadContext`.
- IDs werden als Strings exakt im Format der Tool-Ausgaben übergeben und sind
  ohne Bereinigung oder Umformatierung als Folgeparameter verwendbar
  (Round-Trip-Garantie).
- `limit` gilt einheitlich für Listen-, Search- und Diff-Tools: fehlend oder ≤ 0
  ergibt die konfigurierte Standardseitengröße (`RetrievalPolicy.DefaultPageSize`),
  Werte oberhalb des Maximums werden auf `RetrievalPolicy.MaximumPageSize` geklemmt.
  Cursor-Strings bleiben opak und werden unverändert weitergereicht.

Beispiele (verbindlich, in Vertragstests fixiert):

Erfolg mit Payload:

```json
{
  "code": "Success",
  "data": {
    "transactionId": "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90"
  }
}
```

Erfolg mit Warnung:

```json
{
  "code": "Success",
  "data": {
    "transactionId": "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90"
  },
  "warnings": [
    {
      "code": "NodeTooLarge",
      "message": "Der normalisierte Content übersteigt die Warnschwelle.",
      "details": { "actualBytes": "5123", "thresholdBytes": "4096" }
    }
  ]
}
```

Fehler:

```json
{
  "code": "TransactionNotFound",
  "message": "Die angefragte Transaction existiert nicht.",
  "details": { "transactionId": "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90" }
}
```

---

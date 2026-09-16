# 61. MCP-API – konzeptionelle V1-Funktionen

Die exakten Tool-Namen und JSON-Schemas werden bei der Implementierung festgelegt.

Konzeptionell benötigt V1 mindestens folgende Funktionsgruppen.

## Transactions

```text
begin_transaction
commit_transaction
discard_transaction
get_transaction
```

## Navigation und Lesen

```text
get_root
get_node
list_children
list_roles
search
```

## Strukturänderungen

```text
create_node
update_node
move_node
reorder_node
delete_node
```

## Rollen und Resolution Orders

```text
create_role
update_role
delete_role
set_role_resolution
```

## Content

```text
replace_content
replace_text
delete_content
```

## Prüfung

```text
validate_transaction
```

## Export

```text
export_tree
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

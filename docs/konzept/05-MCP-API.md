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

---

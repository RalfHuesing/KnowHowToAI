# 31. Transaktionen

Sämtliche fachlichen Schreiboperationen eines Agenten laufen innerhalb einer **KnowHowTo-AI-Transaktion**.

Beispiel:

```text
begin_transaction()
→ transactionId
```

Danach:

```text
create_node(transactionId, ...)
update_content(transactionId, ...)
move_node(transactionId, ...)
delete_content(transactionId, ...)
```

Am Ende:

```text
commit_transaction(transactionId)
```

oder:

```text
discard_transaction(transactionId)
```

---

# 32. KnowHowTo-AI-Transaction ist keine offene SQL-Transaction

Eine KnowHowTo-AI-Transaction darf **keine minutenlang offene SQL-Server-Transaktion** sein.

Agenten können zwischen zwei MCP-Aufrufen:

- lange rechnen,
- weitere Tools verwenden,
- abstürzen,
- beendet werden.

Eine SQL-Transaction über die gesamte Agentenlaufzeit würde unter anderem zu:

- Locks,
- Timeouts,
- blockierenden Verbindungen,
- unnötig schwieriger Fehlerbehandlung

führen.

Stattdessen besteht eine KnowHowTo-AI-Transaction aus mehreren kurzen atomaren SQL-Operationen.

---

# 33. Vollständiger Working Snapshot

Beim Öffnen einer Transaction wird der vollständige aktuelle Wissensstand kopiert.

Beispiel:

```text
Current Snapshot = 100
```

Agent ruft auf:

```text
begin_transaction()
```

Der Server erzeugt:

```text
Snapshot 101
State = Working
BaseSnapshotId = 100
```

Der Inhalt von Snapshot 100 wird vollständig über SQL-Operationen wie:

```text
INSERT ... SELECT ...
```

in den Working Snapshot kopiert.

Die vollständige Kopie umfasst mindestens:

- Rollen,
- Role Resolution Orders,
- Nodes,
- NodeContents,
- ContentDependencies.

Die Transaction referenziert:

```text
TransactionId
BaseSnapshotId = 100
WorkingSnapshotId = 101
```

Alle weiteren Änderungen erfolgen ausschließlich auf Snapshot 101.

---

# 34. Warum vollständige Kopien

V1 verwendet bewusst vollständige Snapshot-Kopien.

Vorteile:

- sehr einfaches mentales Modell,
- jeder Snapshot ist vollständig,
- Reads benötigen keine Rekonstruktion aus Deltas,
- kein Event-Replay,
- kein komplexes Overlay,
- Transaktionen können vollständig verworfen werden,
- Unterschiede zwischen zwei Zuständen können direkt ermittelt werden,
- historische Zustände bleiben reproduzierbar.

Der höhere Speicherbedarf wird für V1 bewusst akzeptiert.

---

# 35. Spätere Snapshot-Optimierung

Falls vollständige Kopien bei sehr großen Datenmengen später zu teuer werden, kann intern beispielsweise auf:

```text
Copy-on-write
```

oder andere Snapshot-Verfahren umgestellt werden.

Die MCP- und Domain-API darf davon nicht abhängen.

Für Agenten existieren weiterhin nur:

```text
TransactionId
SnapshotId
NodeId
RoleId
```

Die interne Speicheroptimierung ist ein Implementierungsdetail.

Copy-on-write wird in V1 **nicht implementiert**.

---

# 36. Keine physischen fachlichen Löschungen

Historische Wissensstände dürfen nicht zerstört werden.

Eine Löschoperation bedeutet deshalb fachlich beispielsweise:

```text
IsDeleted = true
```

oder eine äquivalente Tombstone-Semantik.

Es werden keine historischen Datensätze physisch entfernt, um den aktuellen Zustand herzustellen.

Ein alter Snapshot bleibt unverändert nachvollziehbar.

Soft-Delete gilt für fachliche Objekte mit stabiler Identität, insbesondere Nodes,
Rollen und Rollen-Content. Reine Zuordnungen wie Role Resolution Orders und Content
Dependencies dürfen innerhalb eines Working Snapshots atomar ersetzt werden. Dies
zerstört keine Historie, weil der unveränderte Base Snapshot die vorherige Zuordnung
vollständig enthält. Committed oder historische Snapshots werden niemals physisch
verändert.

---

# 37. Snapshot-Zustände

Mindestens:

```text
Working
Committed
Discarded
```

Optional später:

```text
Abandoned
```

Ein Working Snapshot gehört zu einer offenen KnowHowTo-AI-Transaction.

Ein Committed Snapshot ist unveränderlich.

Ein Discarded Snapshot wird nicht zum aktuellen Wissensstand, kann aber zur Nachvollziehbarkeit erhalten bleiben.

---

# 38. Commit

Beim Commit wird in einer kurzen atomaren SQL-Transaction geprüft und ausgeführt:

1. Transaction ist noch offen.
2. Working Snapshot ist gültig.
3. Validatoren laufen.
4. Es besteht kein konkurrierender Snapshot-Konflikt.
5. Working Snapshot wird `Committed`.
6. `CurrentSnapshotId` wird auf diesen Snapshot gesetzt.
7. Transaction wird `Committed`.

Committed Snapshots werden danach nicht mehr verändert.

---

# 39. Discard

```text
discard_transaction(transactionId)
```

bewirkt:

```text
Transaction.State = Discarded
WorkingSnapshot.State = Discarded
```

Der globale aktuelle Snapshot bleibt unverändert.

Es müssen keine Änderungen rückwärts ausgeführt werden.

Der komplette Working Snapshot wird einfach nicht aktiviert.

---

# 40. Gleichzeitige Transactions

Mehrere Agenten können grundsätzlich auf demselben aktuellen Snapshot starten.

Beispiel:

```text
Current = 100

Agent A:
Base = 100
Working = 101

Agent B:
Base = 100
Working = 102
```

Agent A committet:

```text
Current = 101
```

Agent B darf anschließend Snapshot 102 nicht einfach committen.

Sonst würden Änderungen von A verloren gehen.

Commit B muss fehlschlagen:

```text
SnapshotConflict

BaseSnapshot = 100
CurrentSnapshot = 101
```

Agent B muss anschließend beispielsweise:

- seine Transaction verwerfen,
- aktuellen Stand neu laden,
- neue Transaction starten,
- Änderungen erneut anwenden.

---

# 41. Kein automatisches Merge in V1

Folgendes wird in V1 bewusst nicht implementiert:

- automatisches Merge,
- automatisches Rebase,
- Drei-Wege-Merge von Knowledge Snapshots,
- Konfliktauflösung durch den Server.

Das System bevorzugt zunächst deterministisches und sicheres Verhalten.

---

# 42. Lesen innerhalb einer Transaction

Ein Agent muss seine eigenen Änderungen lesen können.

Deshalb akzeptieren Read-Funktionen optional eine `transactionId`.

Ohne Transaction:

```text
get_node(nodeId, roleId)
```

liest aus dem aktuellen committed Snapshot.

Mit Transaction:

```text
get_node(
    nodeId,
    roleId,
    transactionId
)
```

liest aus dem Working Snapshot dieser Transaction.

Dasselbe Prinzip gilt für:

- `list_children`
- `search`
- `validate`
- `export`
- weitere Reads

Alternativ kann für historische Analyse explizit eine `snapshotId` angegeben werden.

---

# 43. Node- und Content-Löschung sind verschieden

Da die Hierarchie global ist, haben folgende Operationen unterschiedliche Bedeutung.

## Node löschen

```text
delete_node(...)
```

entfernt den fachlichen Punkt global aus der aktuellen Wissensstruktur.

Das betrifft damit alle Rollen.

## Rollen-Content löschen

```text
delete_content(
    nodeId,
    roleId
)
```

entfernt lediglich den expliziten Content dieser Rolle.

Danach kann beispielsweise wieder der konfigurierte Rollen-Fallback greifen.

Diese Operationen dürfen nicht verwechselt werden.

Beim Löschen eines Nodes mit Children sollte V1 standardmäßig einen Fehler erzeugen.

Eine Subtree-Löschung muss explizit angefordert werden.

---

# 44. Releases

Ein Snapshot kann zusätzlich als Release markiert werden.

Ein Release ist ein benannter Verweis auf einen bereits committed Snapshot.

Beispiel:

```text
Release:
2026.09

Snapshot:
147
```

Damit wird zwischen zwei Konzepten unterschieden:

```text
Committed Snapshot
```

und:

```text
fachlich freigegebener / benannter Stand
```

Nicht jeder Commit muss automatisch ein Release sein.

Ein Release verändert den referenzierten Snapshot nicht.

---

# 45. Historische Reproduzierbarkeit

Ein alter Snapshot muss später denselben Wissensstand liefern wie zum Zeitpunkt seines Commits.

Deshalb gehören zum versionierten Zustand nicht nur Node-Contents.

Versioniert werden mindestens:

- Nodes
- Hierarchie
- Sortierung
- Rollen
- Role Resolution Orders
- rollenabhängige Contents
- Content-Abhängigkeiten

Wenn sich beispielsweise die Developer-Resolution später von:

```text
Developer
Consultant
Default
```

auf:

```text
Developer
Technical
Default
```

ändert, darf dies einen alten Snapshot nicht nachträglich verändern.

---

# 46. Rollenadministration in V1

V1 benötigt keine grafische Administration.

Die Rolle `Default` und ihre initiale Resolution Order dürfen durch das Seed-Skript
angelegt werden. Laufende Änderungen erfolgen anschließend über transaktionale
Application-/MCP-Operationen, mindestens:

```text
create_role
update_role
delete_role
set_role_resolution
```

Wichtig:

Committed Snapshots dürfen auch dabei niemals direkt verändert werden.

Änderungen müssen einen neuen versionierten Zustand erzeugen und deshalb eine offene
KnowHowTo-AI-Transaction verwenden. Direkte administrative SQL-Änderungen an
versionierten Tabellen sind kein unterstützter V1-Workflow.

Eine spätere Administrationsoberfläche ist vorgesehen, aber nicht Bestandteil von V1.

---

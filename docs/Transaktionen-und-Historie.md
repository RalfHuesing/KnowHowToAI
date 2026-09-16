# Transaktionen und Historie

## Transaktionsablauf

Sämtliche fachlichen Schreiboperationen laufen innerhalb einer
KnowHowTo-AI-Transaction:

```text
begin_transaction()            → transactionId
create_node(transactionId, …)
update_content/replace_content(transactionId, …)
move_node(transactionId, …)
delete_content(transactionId, …)
commit_transaction(transactionId)   oder   discard_transaction(transactionId)
```

Ohne Transaktionskontext existiert keine Schreiboperation am versionierten
Zustand. Damit kann kein Agent versehentlich direkt einen committed Zustand
verändern.

## Keine offene SQL-Transaction

Eine KnowHowTo-AI-Transaction ist **keine** minutenlang offene SQL-Server-Transaction.
Agenten können zwischen zwei MCP-Aufrufen lange rechnen, weitere Tools verwenden,
abstürzen oder beendet werden. Eine durchgehend offene SQL-Transaction würde zu
Locks, Timeouts und blockierenden Verbindungen führen. Stattdessen besteht eine
KnowHowTo-AI-Transaction aus mehreren kurzen, atomaren SQL-Operationen.

## Vollständiger Working Snapshot

Beim Öffnen einer Transaction wird der vollständige aktuelle Wissensstand kopiert:

```text
Current Snapshot = 100
begin_transaction()
→ Snapshot 101, State = Working, BaseSnapshotId = 100
```

Der Inhalt wird per `INSERT … SELECT …` vollständig kopiert, mindestens: Rollen,
Role Resolution Orders, Nodes, NodeContents, ContentDependencies. Die Transaction
referenziert Base- und WorkingSnapshotId; alle weiteren Änderungen erfolgen
ausschließlich auf dem Working Snapshot.

Vollständige Kopien bedeuten: sehr einfaches mentales Modell, jeder Snapshot ist
vollständig, Reads brauchen keine Rekonstruktion aus Deltas, kein Event-Replay,
kein Overlay, Transactions können vollständig verworfen werden, Unterschiede sind
direkt ermittelbar und historische Zustände reproduzierbar. Der höhere
Speicherbedarf ist bewusst akzeptiert
([Entscheidungen](Entscheidungen.md)); eine interne Umstellung auf
Copy-on-write wäre später möglich, ohne die fachliche API zu ändern.

## Snapshot-Zustände

```text
Working    – gehört zur offenen Transaction
Committed  – unveränderlich
Discarded  – wird nie Current, bleibt zur Nachvollziehbarkeit erhalten
```

## Commit

Beim Commit wird in einer kurzen, atomaren SQL-Transaction geprüft und
ausgeführt:

1. Transaction ist noch offen.
2. Working Snapshot ist gültig.
3. Validatoren laufen.
4. Es besteht kein konkurrierender Snapshot-Konflikt.
5. Working Snapshot wird `Committed`.
6. `CurrentSnapshotId` wird auf diesen Snapshot gesetzt.
7. Transaction wird `Committed`.

## Discard

`discard_transaction(transactionId)` setzt Transaction und Working Snapshot auf
`Discarded`. Der globale aktuelle Snapshot bleibt unverändert; es muss nichts
rückwärts ausgeführt werden – der Working Snapshot wird einfach nicht aktiviert.

## Gleichzeitige Transactions und SnapshotConflict

Mehrere Agenten können auf demselben Base Snapshot starten:

```text
Current = 100
Agent A: Base = 100, Working = 101
Agent B: Base = 100, Working = 102
```

Committet A (Current = 101), muss der Commit von B deterministisch fehlschlagen:

```text
SnapshotConflict
BaseSnapshot = 100
CurrentSnapshot = 101
```

B muss anschließend verwerfen, den aktuellen Stand neu laden, eine neue Transaction
starten und die Änderungen erneut anwenden. Es gibt kein automatisches Merge,
Rebase oder Drei-Wege-Merge
([Entscheidungen](Entscheidungen.md)).

## Lesen innerhalb einer Transaction

Read-Funktionen akzeptieren optional eine `transactionId` (Working Snapshot) oder
– für historische Analyse – eine `snapshotId`; ohne Selektor wird der aktuelle
committed Snapshot gelesen. Beide Selektoren zugleich sind unzulässig
(`InvalidReadContext`). Dies gilt einheitlich für `get_root`, `get_node`,
`list_children`, `list_roles`, `search`, `export_tree`, `validate_transaction` und
die Historien-Tools (Verträge: [MCP-API](McpApi.md)).

## Löschsemantik

Historische Wissensstände werden nicht zerstört:

- Fachobjekte mit stabiler Identität (Nodes, Rollen, Rollen-Content) werden per
  Soft-Delete/Tombstone (`IsDeleted`) entfernt.
- Reine Zuordnungszeilen ohne eigene Identität (Role Resolution, Content
  Dependency) dürfen im Working Snapshot atomar ersetzt werden; der Base Snapshot
  enthält die vorherige Fassung vollständig.
- Committed oder historische Snapshots werden niemals physisch verändert.

Zwei verschiedene Operationen:

- `delete_node` entfernt den fachlichen Punkt **global** aus der aktuellen
  Wissensstruktur (alle Rollen). Eine Node mit aktiven Children wird ohne
  `deleteSubtree: true` mit `NodeHasChildren` abgelehnt; die Subtree-Löschung
  tombstoned Nodes und deren Contents atomar.
- `delete_content` entfernt nur den expliziten Content **einer Rolle**; danach
  kann der konfigurierte Rollen-Fallback wieder greifen.

Diese Operationen dürfen nicht verwechselt werden.

## Releases

Ein Release ist ein benannter, unveränderlicher Verweis auf einen bereits
committed Snapshot:

```text
Release: 2026.09  →  Snapshot 147
```

Nicht jeder Commit ist automatisch ein Release; ein Release verändert den
referenzierten Snapshot nicht. `create_release` ist reine Metadatenverwaltung und
benötigt deshalb keine Transaction.

Invarianten:

- Name nicht leer/Whitespace (`ReleaseNameRequired`), eindeutig
  (`ReleaseNameConflict`).
- Der referenzierte Snapshot muss existieren (`SnapshotNotFound`) und `Committed`
  sein (`SnapshotNotCommitted`).
- Existenz- und Zustandsprüfung erfolgen im SQL-Repository im selben kurzen,
  gesperrten Vorgang wie das Insert (`UPDLOCK, HOLDLOCK` auf Snapshot-Zeile und
  Release-Name); fehlgeschlagene Aufrufe legen keinen Release-Datensatz an.

`list_releases` liefert deterministisch nach `ReleaseId ASC`, seitenweise über
einen opaken Keyset-Cursor ([Retrieval](Retrieval.md)). Ein Release darf stale
Derived Content oder übergroßen Content enthalten; Qualitätsbefunde des
referenzierten Snapshots werden transparent als `Findings` zurückgegeben, ohne den
Release-Vorgang abzubrechen. Harte Release-Policies existieren in V1 bewusst
nicht.

## Historische Reproduzierbarkeit

Ein alter Snapshot liefert später denselben Wissensstand wie zum Zeitpunkt seines
Commits. Versioniert werden mindestens: Nodes, Hierarchie, Sortierung, Rollen,
Role Resolution Orders, rollenabhängige Contents und Content-Abhängigkeiten.
Ändert sich beispielsweise eine Resolution Order später, verändert das den alten
Snapshot nicht nachträglich.

## Strukturierter Netto-Diff

`compare_snapshots` vergleicht zwei beliebige committed Snapshots;
`get_transaction_changes` vergleicht den Base-Snapshot einer Transaction mit ihrem
Working- (Status `Open`) oder Committed-Snapshot (Status `Committed`).

Grundsatz: **Netto-Zustandsdifferenz statt Operation-Log.** Der Diff rekonstruiert
kein Event-Log, sondern ermittelt deklarativ den Unterschied zwischen Base und
Target:

- `Added`: im Target aktiv, im Base nicht vorhanden oder gelöscht.
- `Modified`: in beiden aktiv, mindestens ein versionsrelevantes Attribut
  geändert.
- `Deleted`: im Base aktiv, im Target gelöscht oder nicht mehr zugeordnet.

Unveränderte Objekte tauchen nicht auf. Der Diff umfasst fünf strukturierte
Kategorien in fester Reihenfolge:

1. `Roles` (nach `RoleId`)
2. `RoleResolutions` (nach `RequestedRoleId`, dann `CandidateRoleId`)
3. `Nodes` (nach `SortOrder`, dann `NodeId`)
4. `Contents` (nach `NodeId`, dann `RoleId`)
5. `Dependencies` (nach `TargetNodeId`, `TargetRoleId`, `SourceNodeId`,
   `SourceRoleId`)

Große Diffs werden seitenweise über einen opaken `DiffCursor` paginiert, der an
Base-, Target-Snapshot und – bei offenen Transactions – an `ChangeVersion` gebunden
ist; eine zwischenzeitliche Mutation führt deterministisch zu `CursorExpired`.
Jeder Eintrag enthält `kind`, `entityType` und die vollständigen fachlichen
Schlüsselfelder; zwei Dependencies desselben Targets bleiben unterscheidbar
(Source-Felder inklusive). Kosten und bewusste Grenzen des Diff-Pagings:
[Retrieval](Retrieval.md).

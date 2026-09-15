# 65. Konzeptionelles relationales Datenmodell

Die endgültigen Tabellen werden separat entworfen.

Alle KnowHowTo-AI-Tabellen erhalten den Präfix:

```text
KnowHowToAI_
```

Konzeptionell werden mindestens folgende Tabellen benötigt:

```text
KnowHowToAI_Snapshot
KnowHowToAI_Transaction
KnowHowToAI_Release

KnowHowToAI_Node
KnowHowToAI_NodeContent

KnowHowToAI_Role
KnowHowToAI_RoleResolution

KnowHowToAI_ContentDependency
```

Zusätzlich wird ein kleiner Systemzustand benötigt, der unter anderem auf den aktuellen Snapshot verweist.

Beispielsweise:

```text
KnowHowToAI_SystemState
```

---
# 66. Snapshot-Schlüssel

Da jeder Snapshot einen vollständigen Zustand enthält, werden versionierte Tabellen sinngemäß mit:

```text
SnapshotId
```

kombiniert.

Beispiel:

```text
KnowHowToAI_Node

SnapshotId
NodeId
ParentNodeId
Title
Description
SortOrder
IsDeleted
```

Der logische Primärbezug eines Nodes bleibt:

```text
NodeId
```

Die gespeicherte Ausprägung eines Nodes gehört dagegen zu einem konkreten Snapshot.

---

# 67. NodeContent

Konzeptionell:

```text
SnapshotId
NodeId
RoleId
ContentRevisionId
ContentMode
ContentMd
IsDeleted
```

Mögliche Werte für `ContentMode`:

```text
Independent
Derived
```

Fallback benötigt keinen duplizierten Content-Datensatz.

Fallback ist ein Ergebnis der Role Resolution.

---

# 68. ContentDependency

Konzeptionell:

```text
SnapshotId

TargetNodeId
TargetRoleId

SourceNodeId
SourceRoleId
SourceContentRevisionId
```

Damit kann geprüft werden, ob eine abgeleitete Darstellung noch auf den aktuellen Source-Revisions basiert.

---

# 69. Transaction-Metadaten

Für Nachvollziehbarkeit soll eine Transaction mindestens besitzen:

```text
TransactionId
BaseSnapshotId
WorkingSnapshotId
State
CreatedAt
CommittedAt
ChangeVersion
```

`ChangeVersion` beginnt bei `0` und wird bei jeder erfolgreichen Änderung des
Working Snapshots atomar erhöht. Damit können paginierte Reads erkennen, dass ein
Cursor nach einer zwischenzeitlichen Mutation nicht mehr zu demselben Arbeitsstand
gehört.

Zusätzlich sind optionale Metadaten sinnvoll:

```text
Purpose
Actor
Client
CommitMessage
```

Diese Felder sind keine Voraussetzung für die fachliche Semantik, erleichtern aber Audit und spätere Analyse.

---

# 70. Historie auf Transaction-Ebene

V1 muss mindestens den Zustand vor und nach einer Transaction erhalten.

Damit lässt sich feststellen:

```text
Base Snapshot
vs.
Committed Snapshot
```

und daraus der Netto-Unterschied ableiten.

Nicht zwingend benötigt wird in V1 ein vollständiges Eventlog jedes einzelnen MCP-Schreibaufrufs innerhalb einer Transaction.

Falls später Debugging oder detailliertes Audit benötigt wird, kann zusätzlich ein append-only Operation Log eingeführt werden.

Das ist eine mögliche spätere Erweiterung.

---

# 71. Globale Strukturänderungen sind bewusst sichtbar

Da die Hierarchie global ist, wirken folgende Operationen auf alle Rollen:

```text
rename_node
move_node
delete_node
reorder_node
```

Ein Entwickler darf deshalb eine globale Strukturänderung nicht mit einer rein rollenbezogenen Inhaltsänderung verwechseln.

Wenn nur technischer Content geändert werden soll:

```text
replace_content(role = Developer)
```

Wenn der fachliche Node tatsächlich global umstrukturiert werden soll:

```text
move_node(...)
```

Die gemeinsame Struktur ist bewusst ein stärkeres Konsistenzmodell.

---

# 72. Fehlende Rollen-Inhalte sind nicht automatisch Fehler

Wenn eine Rolle keinen expliziten Content besitzt, kann das mehrere Bedeutungen haben:

1. Fallback ist bewusst ausreichend.
2. Der Node ist für diese Rolle irrelevant.
3. Die Dokumentation für diese Rolle wurde noch nicht erstellt.

Deshalb gilt:

```text
kein expliziter Content
```

nicht automatisch als Qualitätsfehler.

Die MCP-Antwort muss jedoch transparent machen, ob:

```text
Explicit
Fallback
None
```

verwendet wird.

Ein Agent kann diese Information in einem späteren Synchronisationsworkflow auswerten.

---

# 73. Endanwender-Dokumentation wird als eigener Flow behandelt

Das System soll ausdrücklich folgenden Workflow ermöglichen:

```text
Consultant-Wissen
        ↓
Entwicklung
        ↓
mehrere technische Iterationen
        ↓
stabiler Implementierungsstand
        ↓
Endanwender-Dokumentation aktualisieren
```

Endanwender-Dokumentation wird nicht bei jedem Entwicklungsschritt automatisch mitgeschrieben.

Das ist ein wesentliches Designprinzip.

---

# 74. Drift-Prinzip

KnowHowTo AI versucht nicht, Drift während laufender Arbeit vollständig zu verhindern.

Stattdessen gilt:

> Drift darf temporär entstehen, aber er darf nicht unbemerkt bleiben.

Das System muss deshalb jederzeit erkennen können:

```text
Dieser abgeleitete Content basiert nicht mehr auf den aktuellen Quellen.
```

Das ist wichtiger als permanente automatische Synchronisation.

---

# 75. Releases und Drift

Ein normaler Working- oder Committed Snapshot darf stale Content enthalten.

Dies ist während laufender Entwicklung normal.

Vor einem fachlichen Release kann ein Agent explizit prüfen:

```text
Welche Inhalte der Rolle EndUser sind stale?
```

und diese aktualisieren.

Eine spätere Erweiterung kann Release Policies anbieten, beispielsweise:

```text
Release für EndUser nur zulassen,
wenn keine stale Derived Contents vorhanden sind.
```

Automatische komplexe Release-Policies sind zunächst nicht zwingender Bestandteil von V1.

---

# 76. Design für agentische Entwicklung

MCP-Funktionen sollen:

- kleine Aufgaben erledigen,
- deterministisch sein,
- klare Parameter besitzen,
- klare Fehler zurückgeben,
- keine versteckte globale Session-Semantik besitzen,
- keine unnötig großen Inhalte zurückliefern.

Der Server soll keine komplexe LLM-Logik enthalten.

Aufgaben wie:

- Wissen interpretieren,
- Dokumentation umformulieren,
- Nodes sinnvoll aufteilen,
- fachliche Inhalte zusammenführen,
- Rollen-Dokumentation erzeugen

übernimmt der Agent.

Der Server stellt dafür sichere Primitive, Versionierung, Validierung und Persistenz bereit.

---

# 77. Bewusst nicht in V1

Folgende Dinge werden aktuell **nicht implementiert**:

- HTTP-MCP
- REST-API
- grafische Administration
- Rollen-Administrationsoberfläche
- Benutzerverwaltung
- Berechtigungs-/ACL-System
- role-spezifische Hierarchien
- role-spezifische Präsentations-Views
- automatisches Merge konkurrierender Transactions
- automatisches Rebase
- Copy-on-write-Snapshots
- Vektordatenbank
- zwingende Embeddings
- automatische permanente Rollensynchronisation
- automatisches Refactoring während normaler Writes
- komplexes Unified-Diff-Patching
- lokale Temp-Datei-Workflows
- Git als notwendiger Storage
- LLM-Logik innerhalb des MCP-Servers
- komplexe Release Policies
- vollständiges Event-Sourcing

---

# 78. Für später architektonisch berücksichtigen

Folgende Erweiterungen sollen nicht umgesetzt werden, dürfen durch das V1-Design aber nicht unnötig verhindert werden:

## Weitere Transporte

```text
MCP über HTTP
REST API
```

## Admin-Oberfläche

Verwaltung von:

- Rollen
- Resolution Orders
- Releases
- Snapshots
- Transactions
- Knowledge-Struktur

Nicht versionierte Validator- und Betriebsparameter bleiben auch bei einer späteren
Oberfläche Anwendungskonfiguration und werden nicht als Wissensinhalt in dieser
Datenbank gespeichert.

## Präsentations-Views

Unterschiedliche Hierarchie- oder Navigationsdarstellungen für verschiedene Zielgruppen auf Basis derselben canonical NodeIds.

## Semantic Search

- Embeddings
- Vektorsuche
- Hybrid Search

## Snapshot-Optimierung

- Copy-on-write
- deduplizierte Inhalte
- inkrementelle physische Speicherung

ohne Änderung der fachlichen Snapshot-Semantik.

## Merge / Rebase

Spätere Unterstützung paralleler Agentenänderungen.

## Operation Log

Detailliertes Audit aller einzelnen Mutationen innerhalb einer Transaction.

## Release Policies

Automatisierte Regeln wie:

```text
EndUser-Release darf keinen stale Derived Content enthalten.
```

## Security

Separates Modell für:

- Authentifizierung
- Autorisierung
- Mandantenfähigkeit
- Benutzerrechte

Content-Rollen bleiben davon getrennt.

---

# 79. Wichtige V1-Invarianten

Die folgenden Regeln sind verbindlich.

1. Jeder Write am versionierten Wissenszustand benötigt eine `TransactionId`; die Registrierung eines Releases ist reine Metadatenverwaltung.
2. Eine KnowHowTo-AI-Transaction ist keine langfristig offene SQL-Transaction.
3. Beim Öffnen einer Transaction wird der vollständige aktuelle Wissensstand kopiert.
4. Committed Snapshots sind unveränderlich.
5. Historische Daten werden nicht physisch gelöscht.
6. Jeder Node besitzt eine stabile logische `NodeId`.
7. Pro Snapshot existiert höchstens ein aktiver Root-Node; ein leerer Snapshot darf keinen besitzen.
8. Die Hierarchie ist in V1 global für alle Rollen.
9. Node-Titel werden nicht als Überschrift oder Ersatztitel in `ContentMd` gespeichert; normale Erwähnungen im Fließtext sind erlaubt.
10. Persistierter Content darf keine Überschriften enthalten.
11. Überschriften werden ausschließlich aus der Node-Hierarchie erzeugt.
12. Heading-Validierung erfolgt mit einem Markdown-Parser.
13. Rollen sind frei definierbar und werden nach dem initialen Seed transaktional gepflegt.
14. Role Resolution Orders sind frei konfigurierbar und deterministisch.
15. Fehlender Rollen-Content darf per Fallback aufgelöst werden.
16. `requestedRole` und `resolvedRole` werden immer transparent zurückgegeben.
17. Identischer Content wird nicht unnötig pro Rolle dupliziert.
18. Eigener Rollen-Content kann als `Independent` oder `Derived` geführt werden.
19. Derived Content speichert Source-Revisions.
20. Änderungen an Source-Revisions oder stale Derived Sources markieren abhängigen Content transitiv als stale.
21. Content Dependencies dürfen keine Zyklen bilden.
22. Stale Content wird nicht automatisch überschrieben.
23. Dokumentationssynchronisation ist ein eigener Workflow.
24. Nodes sollen klein gehalten werden.
25. Zu große Nodes erzeugen standardmäßig ab 4 KiB eine konfigurierbare Qualitätswarnung statt automatischem Wissensverlust.
26. Refactoring erfolgt bewusst und transaktional.
27. Content-Reads sind metadata-first und token-effizient.
28. Reads können gegen Current Snapshot, historischen Snapshot oder Working Transaction laufen.
29. Konkurrierende Commits dürfen keine Änderungen überschreiben.
30. V1 führt kein automatisches Merge aus.
31. Node-Löschung und Rollen-Content-Löschung sind unterschiedliche Operationen.
32. STDIO ist nur der erste Transport; Businesslogik darf nicht darin liegen.

---

# 80. Zielarchitektur V1

```text
                    KnowHowTo AI

                         MCP
                        STDIO
                          │
                          ▼
                ┌─────────────────┐
                │ MCP Adapter     │
                └────────┬────────┘
                         │
                         ▼
                ┌─────────────────┐
                │ Application     │
                │ Services        │
                └────────┬────────┘
                         │
                         ▼
                ┌─────────────────┐
                │ Domain          │
                │                 │
                │ Transactions    │
                │ Snapshots       │
                │ Nodes           │
                │ Roles           │
                │ Resolution      │
                │ Dependencies    │
                │ Validation      │
                │ Export          │
                └────────┬────────┘
                         │
                         ▼
                ┌─────────────────┐
                │ Repository      │
                └────────┬────────┘
                         │
                         ▼
                ┌─────────────────┐
                │ MS SQL Server   │
                └─────────────────┘
```

---

# 81. Fachliches Gesamtmodell

```text
Knowledge Base
│
├── Current Snapshot
│
├── Historical Snapshots
│
├── Releases
│
└── Transactions
      │
      └── Working Snapshot


Snapshot
│
├── Roles
│   └── Resolution Orders
│
├── Global Node Hierarchy
│
├── Role-specific Node Contents
│
└── Content Dependencies
```

---

# 82. Beispiel eines vollständigen Flows

Ausgangslage:

```text
Current Snapshot = 40
```

Consultant besitzt:

```text
Node: Auftragserfassung
Role: Consultant
Revision: C12
```

Developer besitzt:

```text
Node: Auftragserfassung
Role: Developer
Revision: D31
```

EndUser besitzt:

```text
Node: Auftragserfassung
Role: EndUser
Revision: E8

Derived from:
Consultant C12
Developer D31
```

Entwickler beginnt neue Arbeit:

```text
begin_transaction()

Base Snapshot = 40
Working Snapshot = 41
```

Developer-Content wird geändert:

```text
Developer Revision D31
→ Developer Revision D32
```

EndUser bleibt unverändert:

```text
EndUser Revision E8
```

Seine Dependency lautet weiterhin:

```text
Developer D31
```

Aktuelle Developer-Revision ist:

```text
D32
```

Damit:

```text
EndUser
Availability = Explicit
Freshness = Stale
```

Transaction wird committed.

Später erhält ein Dokumentations-Agent den Auftrag:

```text
Aktualisiere die EndUser-Dokumentation.
```

Er startet eine neue Transaction, liest:

```text
Consultant C12
Developer D32
EndUser E8 [STALE]
```

und erzeugt beispielsweise:

```text
EndUser E9

Derived from:
Consultant C12
Developer D32
```

Nach Commit ist die EndUser-Dokumentation wieder aktuell.

Keine automatische Neugenerierung war während der Entwicklungsiteration notwendig.

---

# 83. Beispiel Rollen-Fallback ohne Duplikation

Node:

```text
Systemvoraussetzungen
```

Content:

```text
Default Revision X5
```

Kein eigener Content für:

```text
Developer
Consultant
EndUser
```

Resolution:

```text
Developer:
Developer
Consultant
Default
```

Ergebnis:

```text
requestedRole = Developer
resolvedRole = Default
fallbackUsed = true
contentRevisionId = X5
```

Es existiert nur eine physische Inhaltsfassung.

---

# 84. Beispiel Hierarchie statt Markdown-Heading

Ungültiger Content:

```markdown
Für SQL Server gelten folgende Regeln.

## Voraussetzungen

SQL Server Version ...
```

Richtige Struktur:

```text
SQL Server
│
└── Voraussetzungen
```

Content `SQL Server`:

```markdown
Für SQL Server gelten folgende Regeln.
```

Content `Voraussetzungen`:

```markdown
SQL Server Version ...
```

Export:

```markdown
# SQL Server

Für SQL Server gelten folgende Regeln.

## Voraussetzungen

SQL Server Version ...
```

Die Überschriften entstehen ausschließlich beim Export bzw. bei strukturierter Darstellung aus der Hierarchie.

---

# 85. Architekturentscheidung bezüglich Markdown

Markdown ist in KnowHowTo AI:

```text
Content Serialization Format
```

und nicht:

```text
Document Structure Storage
```

Die Dokumentstruktur liegt im relationalen Modell.

Das ist eine zentrale Architekturentscheidung und darf bei späteren Implementierungen nicht aufgeweicht werden.

---

# 86. Architekturentscheidung bezüglich Versionierung

KnowHowTo AI verwendet in V1:

```text
vollständige, unveränderliche Snapshots
+
Working Snapshot pro Transaction
```

und nicht:

```text
Event Sourcing
Git Commits
Delta Chains
Patch Chains
```

Diese Entscheidung priorisiert:

- Verständlichkeit,
- Sicherheit,
- Reproduzierbarkeit,
- einfache Agenteninteraktion

gegenüber maximaler Speichereffizienz.

---

# 87. Architekturentscheidung bezüglich Drift

KnowHowTo AI verwendet:

```text
Provenienz + ContentRevision + Stale-Erkennung
```

und nicht:

```text
permanente automatische Synchronisation aller Rollen
```

Damit wird berücksichtigt, dass reale Entwicklungsarbeit iterativ und vorübergehend inkonsistent sein kann.

Das System kontrolliert den Drift, statt ihn künstlich vollständig verhindern zu wollen.

---

# 88. Architekturentscheidung bezüglich Agenten

Der MCP-Server soll möglichst deterministisch und fachlich konservativ bleiben.

Der Agent entscheidet beispielsweise:

- wie Wissen formuliert wird,
- welcher Node sinnvoll ist,
- ob neue Nodes benötigt werden,
- wie ein zu großer Node aufgeteilt wird,
- wie Consultant-Wissen technisch umgesetzt wird,
- wie Endanwender-Dokumentation formuliert wird.

Der Server entscheidet:

- ob die Transaction gültig ist,
- welcher Snapshot gelesen wird,
- ob die Hierarchie gültig ist,
- ob Markdown-Headings verboten sind,
- ob eine Rollenauflösung gültig ist,
- ob Dependencies gültig sind,
- ob ein Commit kollidiert,
- wie Daten persistiert und versioniert werden.

Diese Trennung ist beabsichtigt.

---

# 89. Ausgangspunkt für die Implementierung

Die Implementierung sollte aus diesem Konzept in ungefähr folgender Reihenfolge weiter konkretisiert werden:

1. Domänenmodell und Invarianten festlegen.
2. konkretes SQL-Schema entwerfen.
3. Migration Runner und reale SQL-Testumgebung implementieren.
4. Domänenverträge, Validatoren und Invarianten mit FastTests implementieren.
5. Snapshot- und Transaction-Semantik einschließlich vollständiger Snapshot-Kopie implementieren.
6. Node-Hierarchie, Rollen und Role Resolution implementieren.
7. NodeContent, ContentRevision, Dependencies und transitive Stale-Erkennung implementieren.
8. Application Services implementieren.
9. Navigation, Search, Export, Historie und Releases implementieren.
10. MCP-Verträge definieren.
11. STDIO-MCP-Adapter implementieren.
12. Integrationstests für Migrationen, konkurrierende Transactions, Rollenauflösung, Snapshots, Drift und MCP erstellen.
13. End-to-End-Workflow und V1-Abschlussgates ausführen.

Dieses Konzept ist die fachliche Ausgangsbasis. Änderungen an den hier definierten Kerninvarianten sollten bewusst als Architekturentscheidung behandelt werden.

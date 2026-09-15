# KnowHowTo AI – Konzept und Architekturgrundlage

## 1. Zweck dieses Dokuments

Dieses Dokument beschreibt das fachliche und technische Grundkonzept von **KnowHowTo AI**.

Es dient als verbindliche Ausgangsbasis für die agentische Implementierung. Ein neuer Agent oder Entwickler soll anhand dieses Dokuments verstehen:

- welches Problem KnowHowTo AI löst,
- wie Wissen strukturiert und gespeichert wird,
- wie Agenten Wissen lesen und verändern,
- wie Versionierung und Transaktionen funktionieren,
- wie rollenabhängige Inhalte funktionieren,
- wie inhaltlicher Drift zwischen Rollen erkannt wird,
- welche Invarianten zwingend einzuhalten sind,
- welche Funktionen für V1 vorgesehen sind,
- welche Funktionen bewusst noch nicht umgesetzt werden,
- welche Erweiterungen architektonisch bereits berücksichtigt werden sollen.

Das Dokument beschreibt primär das Konzept. Konkrete Klassen, Tabellen-DDLs, MCP-Schemas und Implementierungsdetails werden daraus abgeleitet.

---

# 2. Grundintention

KnowHowTo AI ist eine **hierarchische, versionierte und rollenabhängige Wissensbasis für Agenten und Menschen**.

Das System ist insbesondere für einen KMU-Kontext gedacht, in dem beispielsweise folgende Rollen miteinander arbeiten:

- Consultant
- Entwickler
- Endanwender
- weitere frei definierbare Rollen

Typischer Ablauf:

1. Ein Consultant erarbeitet fachliches Wissen oder ein Umsetzungskonzept.
2. Ein Entwickler verwendet dieses Wissen bei der agentischen Softwareentwicklung.
3. Während der Entwicklung entsteht zusätzlich technisches Wissen.
4. Die Implementierung kann sich während der Entwicklung mehrfach ändern.
5. Erst wenn ein sinnvoller Stand erreicht ist, wird daraus beispielsweise Endanwender-Dokumentation erzeugt oder aktualisiert.
6. Änderungen an einer Wissensquelle müssen sichtbar machen können, dass davon abgeleitete Dokumentation eventuell veraltet ist.

KnowHowTo AI soll diese Arbeitsweise unterstützen, ohne bei jeder technischen Iteration sofort sämtliche Dokumentationsvarianten neu generieren zu müssen.

Zentrale Ziele sind:

- Wissen für Agenten gezielt und token-effizient verfügbar machen.
- Wissen strukturiert statt als große monolithische Markdown-Dateien speichern.
- unterschiedliche Darstellungen desselben fachlichen Sachverhalts für verschiedene Rollen ermöglichen.
- unnötige Inhaltsduplikate vermeiden.
- strukturellen und inhaltlichen Drift kontrollierbar machen.
- sämtliche Änderungen versioniert und nachvollziehbar halten.
- Agenten sichere, kleine und deterministische Änderungsoperationen anbieten.
- unabhängig davon funktionieren, ob ein Agent lokalen Dateizugriff besitzt.
- als Grundlage sowohl für lokale als auch später zentrale Nutzung dienen.

KnowHowTo AI ist **kein Dateisystem für Markdown-Dateien**.

Markdown ist das Inhaltsformat einzelner Wissenselemente. Struktur, Versionierung, Rollen und Beziehungen liegen im Datenmodell.

---

# 3. Technischer Grundstack

Für V1:

- C#
- .NET in einer zum Implementierungszeitpunkt aktuellen unterstützten Version
- MS SQL Server
- MCP-Server
- Transport zunächst ausschließlich MCP über STDIO
- aktuelle, gepflegte NuGet-Pakete

Konkrete Paketversionen werden nicht in diesem Konzept festgeschrieben.

Für Markdown-Verarbeitung soll eine etablierte Markdown-Bibliothek verwendet werden. Ein eigener Markdown-Parser soll nicht implementiert werden.

Für SQL-Zugriff, MCP und sonstige Infrastruktur sollen aktuelle, gepflegte Standardpakete verwendet werden.

---

# 4. Transport und Systemgrenzen

## 4.1 V1

Der erste MCP-Server arbeitet ausschließlich über:

```text
STDIO
```

Der Agent greift ausschließlich über MCP-Funktionen auf KnowHowTo AI zu.

Es gibt **keinen Workflow über lokale temporäre Markdown-Dateien**.

Insbesondere gibt es nicht:

```text
Datenbank
→ temporäre Datei
→ Agent editiert Datei
→ Datei zurück in Datenbank
```

Alle Lese- und Schreibvorgänge erfolgen direkt über die MCP-API.

Vorteil:

Das System funktioniert mit grundsätzlich jedem Client oder Agenten, der MCP verwenden kann. Es hängt nicht davon ab, ob der Agent:

- lokal läuft,
- Dateisystemzugriff besitzt,
- Git verwenden kann,
- Unified-Diffs anwenden kann.

## 4.2 Spätere Transporte

Die Geschäftslogik darf nicht an STDIO gekoppelt werden.

Die Architektur muss grundsätzlich folgende spätere Adapter ermöglichen:

```text
MCP STDIO ─┐
           │
MCP HTTP ──┼── Application / Domain Layer ── Repository ── SQL Server
           │
REST API ──┘
```

HTTP-MCP und REST werden in V1 **nicht implementiert**.

Die MCP-Handler selbst enthalten möglichst keine fachliche Logik.

Sinngemäß:

```csharp
return await knowledgeService.UpdateContent(...);
```

Die eigentliche Logik liegt in Application-/Domain-Services.

---

# 5. Wissensbasis und Deployment

Ein MCP-Server wird über eine SQL-Verbindung mit einer KnowHowTo-AI-Datenbank verbunden.

Für verschiedene Wissensbestände können einfach mehrere MCP-Server-Prozesse mit unterschiedlichen Connection Strings gestartet werden.

Beispiel:

```text
MCP Server A
→ SQL DB Produkt A

MCP Server B
→ SQL DB Produkt B

MCP Server C
→ SQL DB Internes Wissen
```

V1 benötigt deshalb keine komplexe Mandantenverwaltung innerhalb einer einzelnen MCP-Instanz.

Ein SQL Server kann trotzdem zentral betrieben werden.

Damit können beispielsweise mehrere Entwickler und Consultants jeweils einen lokalen STDIO-MCP-Prozess verwenden, der auf dieselbe zentrale Datenbank zugreift.

---

# 6. Zentrale Domänenobjekte

KnowHowTo AI besitzt konzeptionell folgende zentrale Objekte:

```text
Knowledge Base
│
├── Snapshot
├── Transaction
├── Release
├── Node
├── NodeContent
├── Role
├── RoleResolution
└── ContentDependency
```

---

# 7. Globale hierarchische Struktur

Das Wissen liegt in einer Baumstruktur.

Beispiel:

```text
Sage 100
├── Administration
│   ├── Benutzerverwaltung
│   ├── Rollen und Rechte
│   └── Datensicherung
│
└── Verkauf
    ├── Auftragserfassung
    └── Preisfindung
```

Jeder Punkt der Hierarchie ist ein `Node`.

Ein Node besitzt mindestens:

```text
NodeId
ParentNodeId
Title
SortOrder
Description
```

`NodeId` ist eine stabile logische ID.

Sie bleibt über Snapshots und Änderungen hinweg identisch.

Beispiel:

```text
NodeId = 7A92...
```

repräsentiert dauerhaft denselben fachlichen Wissenspunkt.

Titel, Parent, Reihenfolge oder Inhalt können sich ändern, die logische Identität bleibt jedoch bestehen.

---

# 8. Eine globale Hierarchie für alle Rollen

Alle Rollen verwenden dieselben Nodes und dieselbe Hierarchie.

Es gibt in V1 **keine getrennte Entwickler-, Consultant- und Endanwender-Hierarchie**.

Beispiel:

```text
Auftragserfassung
```

ist genau ein globaler Node.

Dieser kann unterschiedliche Inhalte besitzen:

```text
Auftragserfassung
├── Consultant Content
├── Developer Content
└── EndUser Content
```

Dadurch bleibt die fachliche Zuordnung zwischen den unterschiedlichen Darstellungen erhalten.

Folgendes soll explizit vermieden werden:

```text
Developer:
Architektur
└── OrderEntry

Consultant:
Verkauf
└── Auftrag

EndUser:
Aufträge
└── Neuen Auftrag anlegen
```

wenn anschließend nicht mehr eindeutig feststellbar ist, welche Punkte fachlich zusammengehören.

Die gemeinsame Hierarchie ist ein wesentliches Mittel gegen strukturellen Drift.

---

# 9. Einschränkung der globalen Hierarchie

Unterschiedliche Zielgruppen können langfristig unterschiedliche Navigationsstrukturen benötigen.

Beispielsweise denkt ein Entwickler eventuell in:

```text
Authentifizierung
├── Token
└── Session
```

während ein Endanwender eher in:

```text
Anmelden
Passwort ändern
Sitzung abgelaufen
```

denkt.

V1 akzeptiert bewusst die Einschränkung einer globalen Hierarchie.

Die Architektur soll jedoch nicht verhindern, später zusätzliche **Views / Präsentationshierarchien** einzuführen.

Eine mögliche spätere Erweiterung wäre:

```text
Canonical Nodes
│
├── Default View
├── Developer View
└── EndUser View
```

Dabei würden die Views weiterhin auf dieselben stabilen Node-IDs verweisen.

Diese Funktion wird in V1 **nicht umgesetzt**.

---

# 10. Node-Titel und Content sind getrennt

Der Titel eines Nodes ist Bestandteil der Hierarchie.

Er gehört **nicht** in den Markdown-Content.

Beispiel:

```text
Title:
SQL Server

Content:
Für den Betrieb wird ...
```

Nicht:

```markdown
# SQL Server

Für den Betrieb wird ...
```

Diese Trennung ist eine harte Systeminvariante.

---

# 11. Überschriften im gespeicherten Markdown sind verboten

Der von Agenten gespeicherte Markdown-Content darf **keine Überschriften enthalten**.

Folgendes ist ungültig:

```markdown
Hier steht etwas.

## Voraussetzungen

Weitere Informationen.
```

Wenn der Agent einen Unterabschnitt `Voraussetzungen` benötigt, muss dafür ein neuer Node angelegt werden:

```text
SQL Server
└── Voraussetzungen
```

Der Text von `Voraussetzungen` wird anschließend im Content dieses Nodes gespeichert.

Die komplette Dokumentstruktur entsteht ausschließlich aus der Node-Hierarchie.

---

# 12. Heading-Validierung

Das Heading-Verbot muss über einen echten Markdown-Parser geprüft werden.

Eine einfache Suche nach `#` ist nicht zulässig.

Folgendes muss beispielsweise erlaubt bleiben:

```markdown
C#
```

sowie:

```csharp
#if DEBUG
#endif
```

Heading-Validatoren müssen echte Markdown-Headings erkennen.

Verboten sind insbesondere:

```markdown
# Heading
## Heading
### Heading
```

und Setext-Headings:

```markdown
Heading
=======
```

bzw.

```markdown
Heading
-------
```

HTML-Heading-Konstrukte wie:

```html
<h2>Heading</h2>
```

sollen ebenfalls nicht verwendet werden, da sie die zentrale Struktur umgehen würden.

Fenced Code Blocks werden selbstverständlich nicht als Dokumentüberschriften interpretiert.

---

# 13. Markdown-Inhalt

Node-Content darf normales Markdown verwenden, beispielsweise:

- Absätze
- Listen
- Tabellen
- Links
- Inline-Code
- Code-Blöcke
- Hervorhebungen
- Blockquotes

Nicht erlaubt sind insbesondere:

- Markdown-Headings
- HTML-Headings
- persistiertes Front Matter für Systemmetadaten

Systemmetadaten werden separat gespeichert.

---

# 14. Front Matter und MCP-Metadaten

Persistierter Node-Content enthält kein System-Front-Matter.

Informationen wie:

```text
NodeId
RequestedRole
ResolvedRole
SnapshotId
ContentRevisionId
Freshness
```

gehören zur strukturierten MCP-Antwort.

Beispiel:

```json
{
  "nodeId": "...",
  "requestedRole": "Developer",
  "resolvedRole": "Consultant",
  "fallbackUsed": true,
  "contentRevisionId": "...",
  "content": "..."
}
```

Falls später für bestimmte Ausgabeformen Front Matter sinnvoll ist, kann dieses **dynamisch erzeugt** werden.

Es wird nicht Bestandteil des gespeicherten Node-Contents.

---

# 15. Nodes sollen klein sein

Ein Node ist eine kleine fachliche Wissenseinheit und kein klassisches großes Dokumentkapitel.

Ungünstig:

```text
Administration
→ 30 KB Markdown
```

Besser:

```text
Administration
├── Benutzerverwaltung
├── Rollen und Rechte
├── Mandanten
├── Datenbankverbindung
└── Sicherung
```

Gründe:

- geringerer Tokenverbrauch,
- gezielteres Retrieval,
- kleinere Änderungen,
- kleinere Replace-Operationen,
- bessere Wiederverwendbarkeit,
- bessere Drift-Erkennung,
- besseres agentisches Arbeiten.

Es wird zunächst kein universelles hartes Größenlimit festgelegt.

Die Größe soll über konfigurierbare Validatoren bewertet werden.

Ein zu großer Node erzeugt zunächst eine Warnung und wird zum Refactoring-Kandidaten.

Wissen soll nicht verloren gehen, nur weil die Struktur gerade nicht optimal ist.

Grundprinzip:

```text
Capture first, structure later.
```

---

# 16. Refactoring von Wissen

Wissensaufnahme und Wissensstrukturierung sind getrennte Tätigkeiten.

Während einer Entwicklungsphase darf ein Node zunächst anwachsen.

Später kann ein eigener Refactoring-Workflow ausgeführt werden.

Beispiel:

```text
Authentifizierung
```

enthält irgendwann Wissen zu:

```text
OAuth
API Keys
JWT
Benutzerrollen
```

Ein Refactoring-Agent kann daraus machen:

```text
Authentifizierung
├── OAuth
├── API Keys
├── JWT
└── Benutzerrollen
```

Hierfür wird in V1 keine komplexe automatische `refactor_everything()`-Funktion benötigt.

Der Agent verwendet normale Primitive wie:

```text
create_node
move_node
update_content
delete_content
delete_node
```

innerhalb einer eigenen Transaktion.

Optional kann später eine Abfrage wie:

```text
list_refactoring_candidates
```

hinzukommen.

---

# 17. Rollenmodell

Rollen sind vollständig frei definierbar.

Beispiele:

```text
Default
Developer
Consultant
EndUser
Administrator
Support
AI
```

V1 kann initial eine Rolle `Default` anlegen.

Es darf jedoch keine fest in der Geschäftslogik kodierte Rollenstruktur geben.

Neue Rollen können grundsätzlich hinzugefügt werden.

Eine Administrationsoberfläche für Rollen ist in V1 nicht Bestandteil des Projekts.

Die Rollen können initial über Datenbankkonfiguration, Seed-Daten oder administrative SQL-Mechanismen gepflegt werden.

Auch bei manueller Pflege dürfen bereits committed Snapshots nicht nachträglich verändert werden.

---

# 18. Wissensrolle ist keine Berechtigungsrolle

Eine KnowHowTo-AI-Rolle beschreibt:

```text
Für welche Zielgruppe ist dieser Inhalt geschrieben?
```

Sie beschreibt **nicht**:

```text
Wer darf diesen Inhalt lesen oder ändern?
```

`Developer`, `Consultant` oder `EndUser` sind deshalb keine Security-Rollen.

Authentifizierung, Autorisierung und ACLs sind ein separates zukünftiges Thema.

Diese Trennung muss erhalten bleiben.

---

# 19. Rollenabhängiger Content

Ein Node kann für jede Rolle eigenen Content besitzen.

Beispiel:

```text
Node: Auftragserfassung

Default:
  allgemeine fachliche Beschreibung

Consultant:
  Prozess- und Konfigurationswissen

Developer:
  technische Implementierungsdetails

EndUser:
  Bedienungsanleitung
```

Es ist ausdrücklich erlaubt, dass für eine Rolle kein eigener Content existiert.

Beispiel:

```text
Developer: vorhanden
Consultant: vorhanden
EndUser: nicht vorhanden
```

---

# 20. Rollenauflösung

Für jede angefragte Rolle existiert eine frei konfigurierbare geordnete Liste von Kandidaten.

Beispiel:

```text
RequestedRole = Developer

Resolution order:
1. Developer
2. Consultant
3. Default
```

Für Consultant könnte unabhängig davon gelten:

```text
RequestedRole = Consultant

Resolution order:
1. Consultant
2. Developer
3. Default
```

Es handelt sich bewusst nicht um klassische objektorientierte Vererbung.

Die Reihenfolge ist eine **Role Resolution Order**.

Sie wird nicht rekursiv aufgelöst.

Dadurch entstehen keine komplizierten zyklischen Fallback-Ketten.

Für jede angefragte Rolle ist die vollständige Kandidatenreihenfolge explizit gespeichert.

Regeln:

- die angefragte Rolle steht normalerweise an erster Position,
- eine Rolle darf innerhalb einer Resolution Order nicht mehrfach vorkommen,
- die Reihenfolge ist deterministisch,
- es kann vorkommen, dass für keine Rolle Content existiert.

---

# 21. Ergebnis einer Rollenauflösung

Bei einer Anfrage:

```text
get_node(
    nodeId,
    role = Developer
)
```

kann beispielsweise zurückkommen:

```text
requestedRole = Developer
resolvedRole = Consultant
fallbackUsed = true
```

Damit weiß der Agent eindeutig, dass er gerade Consultant-Wissen erhalten hat.

Die tatsächlich verwendete Rolle darf niemals implizit verborgen werden.

---

# 22. Vermeidung unnötiger Duplikate

Rollen-Fallback dient unter anderem dazu, identischen Content nicht mehrfach zu speichern.

Wenn derselbe Text für alle Zielgruppen geeignet ist:

```text
Default:
"Die Anwendung wird über Menü X gestartet."

Developer:
kein eigener Content

Consultant:
kein eigener Content

EndUser:
kein eigener Content
```

Dann können alle Rollen denselben Default-Content verwenden.

Es soll nicht automatisch für jede Rolle eine Kopie desselben Textes erzeugt werden.

Eigener Rollen-Content wird nur gespeichert, wenn die Darstellung für diese Rolle tatsächlich abweicht.

---

# 23. Rollen-Fallback löst keinen inhaltlichen Drift

Fallback beantwortet nur:

```text
Welchen Content soll ich verwenden, wenn für diese Rolle kein eigener vorhanden ist?
```

Fallback beantwortet nicht:

```text
Ist ein bereits vorhandener Rollen-Content noch fachlich aktuell?
```

Beispiel:

```text
Developer:
Version 18 beschreibt A, B und C.

EndUser:
älterer eigener Text beschreibt nur A und B.
```

Da EndUser eigenen Content besitzt, wird kein Fallback verwendet.

Trotzdem ist die EndUser-Dokumentation veraltet.

Dafür wird ein separater Mechanismus benötigt.

---

# 24. Content-Revisions

Jeder explizite Rollen-Content besitzt zusätzlich eine logische `ContentRevisionId`.

Beispiel:

```text
NodeId = ABC
RoleId = Developer
ContentRevisionId = REV-17
```

Wird ein Snapshot vollständig kopiert, bleibt die `ContentRevisionId` identisch, solange sich der Inhalt nicht ändert.

Wird der Inhalt verändert, entsteht eine neue `ContentRevisionId`.

Beispiel:

```text
Snapshot 100:
Developer ContentRevision = 17

Snapshot 101:
unverändert
Developer ContentRevision = 17

Snapshot 102:
Content geändert
Developer ContentRevision = 18
```

Dadurch lässt sich feststellen, ob sich eine fachliche Quelle wirklich geändert hat, unabhängig davon, wie viele Snapshots inzwischen erstellt wurden.

---

# 25. Content-Abhängigkeiten und Provenienz

Expliziter Rollen-Content kann von anderen Wissensinhalten abgeleitet sein.

Beispiel:

```text
EndUser / Auftragserfassung

based on:

Developer / Auftragserfassung / Revision 18
Consultant / Auftragserfassung / Revision 9
```

Diese Abhängigkeiten werden strukturiert gespeichert.

Konzeptionell:

```text
Target:
  NodeId
  RoleId

Source:
  NodeId
  RoleId
  ContentRevisionId
```

Eine Abhängigkeit kann auch auf einen anderen Node zeigen.

Damit ist das System nicht darauf beschränkt, nur unterschiedliche Rollen desselben Nodes miteinander zu vergleichen.

---

# 26. Independent und Derived Content

Expliziter Rollen-Content kann konzeptionell zwei Bedeutungen haben.

## Independent

Der Inhalt ist für diese Rolle eigenständig maßgeblich und soll nicht automatisch von anderen Rollen als abgeleitet betrachtet werden.

```text
ContentMode = Independent
```

## Derived

Der Inhalt wurde aus anderen Wissensinhalten abgeleitet.

```text
ContentMode = Derived
```

Dann werden die verwendeten Source-Revisions gespeichert.

Beispiel:

```text
EndUser Content
Mode = Derived

Sources:
- Consultant Revision 12
- Developer Revision 31
```

Wenn sich später eine Source-Revision ändert, wird der abgeleitete Inhalt stale.

---

# 27. Stale-Erkennung

Beispiel:

EndUser wurde erstellt auf Basis von:

```text
Developer Revision 17
```

Später wird Developer geändert:

```text
Developer Revision 18
```

Dann gilt:

```text
EndUser dependency:
expected Developer Revision 17

current Developer Revision:
18

=> EndUser = Stale
```

Der EndUser-Text wird **nicht automatisch verändert**.

Das System signalisiert lediglich:

```text
Dieser Content basiert auf einem älteren Wissensstand.
```

---

# 28. Content-Zustände

Es müssen mindestens zwei Dimensionen unterschieden werden.

## Availability

```text
Explicit
Fallback
None
```

## Freshness

Für expliziten abgeleiteten Content beispielsweise:

```text
Current
Stale
```

Für unabhängigen Content kann die Abhängigkeitsprüfung entfallen.

Damit ist beispielsweise folgende Aussage möglich:

```text
RequestedRole: EndUser
Availability: Explicit
Freshness: Stale
```

oder:

```text
RequestedRole: Developer
Availability: Fallback
ResolvedRole: Consultant
Freshness: Current
```

Fallback und Freshness sind getrennte Konzepte.

---

# 29. Drift wird nicht permanent automatisch repariert

KnowHowTo AI versucht ausdrücklich nicht, bei jeder Änderung alle Rollen sofort zu synchronisieren.

Das wäre für reale Entwicklungsabläufe ungeeignet.

Beispiel:

```text
Implementierung 1
→ EndUser-Doku regenerieren

Implementierung verworfen

Implementierung 2
→ EndUser-Doku erneut regenerieren

erneute Änderung

Implementierung 3
→ EndUser-Doku wieder regenerieren
```

Das erzeugt:

- unnötige LLM-Aufrufe,
- unnötige Änderungen,
- Rauschen,
- schlechte Nachvollziehbarkeit.

Stattdessen wird Drift sichtbar gemacht und später bewusst bearbeitet.

---

# 30. Typischer Arbeitsflow

## Phase 1: Consultant

```text
Consultant
→ fachliches Konzept erstellen
→ Wissen aktualisieren
→ Transaction committen
```

## Phase 2: Entwicklung

```text
Developer
→ Consultant-Wissen lesen
→ implementieren
→ Developer-Wissen ergänzen
→ technische Sackgassen erkennen
→ Inhalte mehrfach verändern
→ mehrere Transactions / Snapshots möglich
```

Währenddessen werden eventuell abhängige Rollen-Inhalte stale.

Sie werden jedoch nicht automatisch geändert.

## Phase 3: Dokumentationssynchronisation

Später explizit:

```text
"Aktualisiere die Endanwender-Dokumentation."
```

Ein Agent erhält dann beispielsweise:

- stale EndUser-Inhalte,
- neue relevante Nodes,
- fehlende Inhalte,
- aktuelle Developer-Inhalte,
- aktuelle Consultant-Inhalte.

Der Agent erzeugt eine neue eigene Transaction und aktualisiert gezielt die EndUser-Dokumentation.

Danach werden neue Dependencies mit den aktuellen Source-Revisions gespeichert.

---

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

Es ist ausreichend, wenn Rollen und ihre Resolution Orders initial über:

- Seed-Daten,
- Datenbankskripte,
- administrative SQL-Prozeduren

gepflegt werden können.

Wichtig:

Committed Snapshots dürfen auch dabei niemals direkt verändert werden.

Änderungen müssen einen neuen versionierten Zustand erzeugen.

Eine spätere Administrationsoberfläche ist vorgesehen, aber nicht Bestandteil von V1.

---

# 47. Export

KnowHowTo AI kann einen Teilbaum als Markdown exportieren.

Beispielhierarchie:

```text
Produkt
└── Installation
    └── SQL Server
        └── Voraussetzungen
```

Wird `SQL Server` als Export-Root ausgewählt, beginnt der Export mit:

```markdown
# SQL Server
```

und nicht mit der ursprünglichen globalen Tiefe:

```markdown
### SQL Server
```

Der ausgewählte Root ist immer Heading-Level 1.

Kinder werden entsprechend:

```markdown
## Voraussetzungen
```

usw.

---

# 48. Export ist trivial, weil Content keine Headings enthält

Da gespeicherter Content niemals eigene Überschriften enthält, muss der Exporter keine im Content enthaltenen Heading-Level verschieben.

Er erzeugt Überschriften ausschließlich aus:

```text
Node.Title
+
Hierarchietiefe relativ zum Export-Root
```

Beispiel:

```text
SQL Server
└── Voraussetzungen
    └── Hardware
```

wird:

```markdown
# SQL Server

...

## Voraussetzungen

...

### Hardware

...
```

---

# 49. Rollenauflösung beim Export

Ein Export erfolgt für eine bestimmte Rolle.

Beispiel:

```text
export_tree(
    rootNodeId,
    roleId = EndUser
)
```

Für jeden Node wird die konfigurierte Rollenauflösung durchgeführt.

Ein Node wird im Export berücksichtigt, wenn:

1. für ihn ein Content aufgelöst werden kann,

oder

2. mindestens ein exportierter Nachfahre existiert.

Damit kann ein Struktur-Node ohne eigenen Content erhalten bleiben.

Beispiel:

```text
Administration
├── Interne API
└── Auftragserfassung
```

Wenn für EndUser nur `Auftragserfassung` relevant ist, kann der Export werden:

```markdown
# Administration

## Auftragserfassung

...
```

Der vollständig irrelevante Zweig `Interne API` wird nicht ausgegeben.

---

# 50. Retrieval muss token-effizient sein

Agenten sollen nicht standardmäßig große Teilbäume oder komplette Dokumentationen erhalten.

Normaler Retrieval-Flow:

```text
list_children
→ Metadaten prüfen
→ relevante Nodes auswählen
→ get_node / get_content
```

Nicht:

```text
gesamte Knowledge Base in den Kontext laden
```

---

# 51. Navigation-Metadaten

Ein Node soll neben `Title` eine kurze rollenunabhängige `Description` bzw. `Purpose` besitzen.

Beispiel:

```text
Title:
Preisfindung

Description:
Berechnung und Priorisierung von Verkaufspreisen.
```

Diese Beschreibung enthält nur Navigationskontext und kein umfangreiches Fachwissen.

`list_children` kann beispielsweise zurückgeben:

```text
NodeId
Title
Description
ChildCount
ContentSize
HasExplicitContent
ResolvedRole
FallbackUsed
Freshness
```

Der Agent kann dadurch entscheiden, welche Nodes er tatsächlich laden muss.

---

# 52. Search

Zusätzlich zur Hierarchienavigation soll eine Suchfunktion vorgesehen werden.

Sie soll primär:

- Node-Titel,
- Descriptions,
- geeignete Content-Felder

durchsuchen können.

Suchergebnisse sollen zunächst Metadaten und kleine Trefferkontexte liefern, nicht automatisch den vollständigen Content sämtlicher Treffer.

Die konkrete Suchtechnologie wird nicht unnötig vorweggenommen.

V1 benötigt keine Vektordatenbank.

Mögliche spätere Erweiterungen:

- SQL Full Text Search
- semantische Suche
- Embeddings
- hybride Suche

Embeddings sind **kein V1-Zwang**.

---

# 53. Schreiben von Content

Da Nodes bewusst klein gehalten werden, ist ein vollständiger Replace eine zulässige Standardoperation.

Beispiel:

```text
replace_content(
    transactionId,
    nodeId,
    roleId,
    content,
    dependencies
)
```

Die alte Version bleibt über den Base-Snapshot erhalten.

Der komplette Node-Content muss deshalb nicht zusätzlich als eigenes Delta gespeichert werden.

---

# 54. Partielle Änderungen

Für kleine punktuelle Änderungen soll zusätzlich eine einfache Textoperation existieren.

Beispiel:

```text
replace_text(
    transactionId,
    nodeId,
    roleId,
    oldText,
    newText
)
```

Regeln:

```text
oldText genau 1x gefunden
→ ersetzen

oldText 0x gefunden
→ Fehler

oldText >1x gefunden
→ Fehler
```

Damit entsteht ein robuster kleiner Patch-Mechanismus ohne Unified-Diff-Komplexität.

---

# 55. Kein Unified-Diff als Kernmechanismus

Unified Diff wird in V1 nicht als primäre MCP-Schreibschnittstelle verwendet.

Zu vermeidende Probleme:

- unterschiedliche Line Endings,
- Context-Mismatch,
- fehlerhafte Diff-Syntax,
- Diff-Parser-Unterschiede,
- Änderungen zwischen Read und Patch,
- unnötige Abhängigkeit von Dateisemantik.

Falls später Bedarf besteht, kann `apply_patch` zusätzlich angeboten werden.

Es ist kein Bestandteil des V1-Kerns.

---

# 56. Kein lokales Dateiediting

KnowHowTo AI exportiert Wissen nicht temporär in Dateien, nur damit Agenten diese anschließend wie normale Dateien bearbeiten.

Damit wird bewusst vermieden:

- Dateisystemabhängigkeit,
- lokale Workspace-Anforderungen,
- Synchronisationsprobleme zwischen Temp-Datei und Storage,
- plattformspezifische Newline-Probleme.

Ein expliziter Markdown-Export ist davon unabhängig und dient der Ausgabe, nicht dem internen Editing-Workflow.

---

# 57. Textnormalisierung

Für gespeicherten Content wird ein kanonisches Format verwendet.

Empfohlen:

```text
UTF-8
LF
```

Line-Ending-Unterschiede sollen nicht Teil der fachlichen Änderungssemantik sein.

---

# 58. Validatoren

Validatoren werden in zwei Kategorien getrennt.

## Harte Invarianten

Ein Commit bzw. die konkrete Operation darf bei Verletzung nicht erfolgreich sein.

Beispiele:

- Markdown enthält Heading
- HTML-Heading vorhanden
- ungültige Parent-ID
- Hierarchiezyklus
- Node existiert nicht
- Rolle existiert nicht
- Transaction ist geschlossen
- `replace_text` findet keinen Match
- `replace_text` findet mehrere Matches
- ungültige Content-Dependency
- ungültige Role Resolution
- Commit basiert auf veraltetem Base Snapshot

## Qualitätswarnungen

Die Änderung darf gespeichert werden.

Beispiele:

- Node ist sehr groß geworden
- zu viele Children
- Content ist möglicherweise redundant
- abgeleiteter Content ist stale
- Refactoring empfohlen
- auffällig großer Content-Replace
- ungewöhnlich tiefe Hierarchie

---

# 59. Wissen darf wegen Qualitätswarnungen nicht verloren gehen

Beispiel:

```text
Node = 14 KB
```

Der Server soll nicht zwingend sagen:

```text
Speichern verboten.
```

Sondern eher:

```text
Speichern erfolgreich.

Warning:
NodeTooLarge
RefactoringRecommended
```

Dies unterstützt den Workflow:

```text
Wissen zuerst erfassen
→ später gezielt strukturieren
```

---

# 60. Validation-API

Neben automatischer Validierung während Schreiboperationen und Commit soll eine explizite Prüfung möglich sein.

Beispiel:

```text
validate_transaction(transactionId)
```

liefert:

```text
errors
warnings
staleContents
refactoringCandidates
```

Optional später:

```text
validate_snapshot(snapshotId)
```

---

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

Mindestens konzeptionell sinnvoll:

```text
get_snapshot
list_releases
compare_snapshots
get_transaction_changes
```

Historienabfragen können teilweise nachgelagert implementiert werden, die Datenstruktur muss sie jedoch ermöglichen.

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
```

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
- Validator-Konfiguration
- Knowledge-Struktur

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

1. Jeder fachliche Write benötigt eine `TransactionId`.
2. Eine KnowHowTo-AI-Transaction ist keine langfristig offene SQL-Transaction.
3. Beim Öffnen einer Transaction wird der vollständige aktuelle Wissensstand kopiert.
4. Committed Snapshots sind unveränderlich.
5. Historische Daten werden nicht physisch gelöscht.
6. Jeder Node besitzt eine stabile logische `NodeId`.
7. Die Hierarchie ist in V1 global für alle Rollen.
8. Node-Titel gehören nicht in `ContentMd`.
9. Persistierter Content darf keine Überschriften enthalten.
10. Überschriften werden ausschließlich aus der Node-Hierarchie erzeugt.
11. Heading-Validierung erfolgt mit einem Markdown-Parser.
12. Rollen sind frei definierbar.
13. Role Resolution Orders sind frei konfigurierbar und deterministisch.
14. Fehlender Rollen-Content darf per Fallback aufgelöst werden.
15. `requestedRole` und `resolvedRole` werden immer transparent zurückgegeben.
16. Identischer Content wird nicht unnötig pro Rolle dupliziert.
17. Eigener Rollen-Content kann als `Independent` oder `Derived` geführt werden.
18. Derived Content speichert Source-Revisions.
19. Änderungen an Source-Revisions können abhängigen Content als stale markieren.
20. Stale Content wird nicht automatisch überschrieben.
21. Dokumentationssynchronisation ist ein eigener Workflow.
22. Nodes sollen klein gehalten werden.
23. Zu große Nodes erzeugen zunächst Qualitätswarnungen statt automatischem Wissensverlust.
24. Refactoring erfolgt bewusst und transaktional.
25. Content-Reads sind metadata-first und token-effizient.
26. Reads können gegen Current Snapshot, historischen Snapshot oder Working Transaction laufen.
27. Konkurrierende Commits dürfen keine Änderungen überschreiben.
28. V1 führt kein automatisches Merge aus.
29. Node-Löschung und Rollen-Content-Löschung sind unterschiedliche Operationen.
30. STDIO ist nur der erste Transport; Businesslogik darf nicht darin liegen.

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
3. Snapshot- und Transaction-Semantik implementieren.
4. Node-Hierarchie implementieren.
5. Rollen und Role Resolution implementieren.
6. NodeContent und ContentRevision implementieren.
7. Markdown-Validator implementieren.
8. Content Dependencies und Stale-Erkennung implementieren.
9. Application Services implementieren.
10. MCP-Tools definieren.
11. STDIO-MCP-Adapter implementieren.
12. Export implementieren.
13. Navigation und Search implementieren.
14. weitere Qualitätsvalidatoren ergänzen.
15. Integrationstests für konkurrierende Transactions, Rollenauflösung, Snapshots und Drift erstellen.

Dieses Konzept ist die fachliche Ausgangsbasis. Änderungen an den hier definierten Kerninvarianten sollten bewusst als Architekturentscheidung behandelt werden.
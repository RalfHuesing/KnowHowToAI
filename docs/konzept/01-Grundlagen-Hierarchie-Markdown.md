# 1. Zweck dieses Dokuments

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

## 3.1 Anwendungskonfiguration

Die KnowHowTo-AI-Datenbank enthält ausschließlich Wissen und die zu seiner
Versionierung, Bearbeitung und Freigabe benötigten Daten. Dazu gehören beispielsweise
Snapshots, Transactions, Rollen, Role Resolution Orders und Releases. Nicht
versionierte Betriebs-, Retrieval- oder Quality-Konfiguration wird nicht in der
Wissensdatenbank gespeichert.

Konfiguration wird in V1 wie folgt getrennt:

- Harte fachliche Invarianten sind nicht abschaltbar. Dazu gehören insbesondere das
  Heading-Verbot, die Transaction-Pflicht, unveränderliche committed Snapshots,
  höchstens ein Root sowie zyklenfreie Hierarchie und Content Dependencies.
- Qualitätsgrenzen wie Content-Größe, Child-Anzahl und Hierarchietiefe sind
  Anwendungskonfiguration. Sie erzeugen Warnungen und können pro Serverinstanz
  unterschiedlich eingestellt werden.
- Retrieval-, Paging-, Timeout- und Migrationsparameter sind ebenfalls
  Anwendungskonfiguration.
- Die Datenbankverbindung steht bewusst in der eigenen, versionierten
  `DatabaseConnection`-Sektion von `appsettings.json`. Sie enthält `Server`,
  `Database`, `UserName`, `Password` und `UseWindowsAuthentication`. Für den
  Greenfield-Stand ist kein alternativer Connection-String aus Environment-Variablen,
  User Secrets oder einem Deployment-Secretstore vorgesehen.
- Der Wert `DatabaseConnection:Server` darf Windows-Umgebungsplatzhalter enthalten.
  Der versionierte Default `%COMPUTERNAME%\MSSQLSERVER2022` wird erst zur Laufzeit
  expandiert; der Platzhalter ist keine Konfigurationsquelle und überschreibt keine
  anderen Einstellungen.
- Die Datenbank wird manuell bereitgestellt und als gegeben angenommen. Der
  SQL-Integrationstest-Harness verbindet sich ausschließlich mit dieser Datenbank und
  führt niemals `CREATE DATABASE` oder `DROP DATABASE` aus.

Die Datenbankverbindung wird ausschließlich aus dieser AppSettings-Sektion gelesen.
Die übrigen Defaults stehen zentral in `appsettings.json`. Überschreibungen für diese
Policies erfolgen in der üblichen Reihenfolge über umgebungsspezifische Appsettings,
Environment-Variablen und Kommandozeilenargumente. Die Konfiguration wird beim
Prozessstart in immutable typed Options gebunden und vollständig validiert. Ungültige
Werte führen zu einem klaren Startfehler. V1 lädt Konfigurationsänderungen nicht live
nach; sie werden nach einem Prozessneustart wirksam.

Domain, Application Services und Storage greifen nicht direkt auf `IConfiguration`
oder frei verteilte Schlüssel zu. Der Composition Root übergibt fertig validierte
Options-/Policy-Records. Dadurch bleiben Defaults, gültige Bereiche und Overrides an
einer zentralen Stelle nachvollziehbar und im Fachcode entstehen keine Magic Numbers.

Da eine MCP-Serverinstanz genau eine KnowHowTo-AI-Datenbank bedient, kann jede
Wissensbasis durch eine eigene App-Konfiguration abweichende Quality-Policies nutzen,
ohne Betriebsparameter in der Datenbank zu speichern.

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

Pro Snapshot existiert höchstens ein aktiver persistierter Root-Node. Der initiale
leere Snapshot darf noch keinen Root besitzen. Sobald ein Root angelegt wurde, bilden
alle weiteren aktiven Nodes genau einen von ihm ausgehenden Baum.

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

Er darf im Markdown-Content nicht nochmals als Dokumenttitel, Überschrift oder
alleinstehender Ersatztitel gespeichert werden. Eine normale sprachliche Erwähnung
des Titels im Fließtext ist dagegen zulässig und oft unvermeidbar. Echte Markdown-
oder HTML-Headings werden hart abgelehnt. Nur heuristisch erkennbare Ersatztitel
erzeugen wegen möglicher Fehlalarme eine Qualitätswarnung.

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
======
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

sind ebenfalls verboten, da sie die zentrale Struktur umgehen würden.

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

Für V1 gilt als konfigurierbarer Standardwert eine Warnschwelle von 4 KiB für den
normalisierten UTF-8-Content eines einzelnen Nodes. Die Warnung enthält Ist-Größe,
Schwelle und die Empfehlung, fachliche Unterpunkte als Child-Nodes anzulegen.

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

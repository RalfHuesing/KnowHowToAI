# Wissenshierarchie

## Baumstruktur und Nodes

Das Wissen liegt in einer Baumstruktur. Beispiel:

```text
Sage 100
├── Administration
│   ├── Benutzerverwaltung
│   ├── Zielgruppen und Rechte
│   └── Datensicherung
│
└── Verkauf
    ├── Auftragserfassung
    └── Preisfindung
```

Jeder Punkt der Hierarchie ist ein `Node` mit mindestens:

```text
NodeId
ParentNodeId
Title
SortOrder
Description
```

`Title` enthält 1 bis 200 Zeichen; die optionale `Description` enthält höchstens
1000 Zeichen. Diese Grenzen werden vor der Persistenz validiert und entsprechen
den Datenbankgrenzen.

`NodeId` ist eine stabile logische ID (GUID) und bleibt über Snapshots und
Änderungen hinweg identisch. Titel, Parent, Reihenfolge und Inhalt können sich
ändern; die logische Identität bleibt bestehen. Pro Snapshot existiert höchstens
ein aktiver persistierter Root-Node; der initiale leere Snapshot besitzt keinen
Sobald ein Root existiert, bilden alle weiteren aktiven Nodes genau einen von ihm
ausgehenden Baum. `get_root` liefert bei leerer Wissensbasis `availability = None`
statt einen künstlichen Node.

Strukturmutationen mit einer expliziten Zielposition behandeln `Before` und
`After` als Einfügeoperationen: Die Quelle wird zuerst aus ihrer bisherigen
Geschwistergruppe entfernt, anschließend an der sichtbaren Zielposition eingefügt
und die betroffenen Gruppen wieder auf die lückenlose Reihenfolge `0..N-1`
normalisiert. Die NodeId ist dabei nur ein Tie-Breaker für bereits ungültige
Gleichstände, nicht die Semantik einer neuen Verschiebung.

Im Wissensbaum lassen sich der erste Root und Unterknoten anlegen sowie Titel
und Beschreibung ändern. Nodes können sichtbar vor, nach oder unter ein Ziel
verschoben werden.
Der erste bestätigte Save oder Move aus Current beginnt den sichtbaren Entwurf.
Details zur Oberfläche und zum Reload-Verhalten stehen im
[Web-UI-Gesamtbild](WebUi.md).

## Eine globale Hierarchie für alle Zielgruppen

Alle Zielgruppen verwenden dieselben Nodes und dieselbe Hierarchie. Es gibt keine
getrennte Entwickler-, Consultant- oder Endanwender-Hierarchie.

```text
Auftragserfassung
├── Consultant Content
├── Developer Content
└── EndUser Content
```

Dadurch bleibt die fachliche Zuordnung zwischen den unterschiedlichen Darstellungen
erhalten. Getrennte Strukturen pro Zielgruppe würden den fachlichen Zusammenhang
zerstören; die gemeinsame Hierarchie ist ein wesentliches Mittel gegen
strukturellen Drift.

Unterschiedliche Zielgruppen können langfristig unterschiedliche
Navigationsstrukturen benötigen. V1 akzeptiert bewusst die Einschränkung einer
globalen Hierarchie; das Datenmodell verhindert spätere
Views/Präsentationshierarchien auf denselben Node-IDs nicht
([Entscheidungen](Entscheidungen.md)).

## Titel und Content sind getrennt

Der Titel eines Nodes ist Bestandteil der Hierarchie. Er darf im Markdown-Content
nicht nochmals als Dokumenttitel, Überschrift oder alleinstehender Ersatztitel
gespeichert werden. Eine normale sprachliche Erwähnung des Titels im Fließtext ist
zulässig und oft unvermeidbar. Echte Markdown- oder HTML-Headings werden hart
abgelehnt (`HeadingNotAllowed`); nur heuristisch erkennbare Ersatztitel erzeugen
wegen möglicher Fehlalarme die Qualitätswarnung `PossibleEmbeddedHeading`. Diese
Trennung ist eine harte Systeminvariante.

In der Wissensoberfläche werden Titel und Beschreibung ausdrücklich am
Knotendokument bearbeitet und unabhängig vom Content gespeichert. Ein erster
Save beginnt ohne vorgeschaltete Transaktionsmaske eine Working Transaction;
weitere Node- und Content-Saves schreiben in denselben Entwurf. Die UI hält
Titel und Markdown getrennt und speichert keine Dokumentüberschrift im Content
([Web-UI-Gesamtbild](WebUi.md)).

## Heading-Verbot

Gespeicherter Markdown-Content enthält **keine Überschriften**. Benötigt ein Agent
einen Unterabschnitt wie `Voraussetzungen`, wird dafür ein neuer Node angelegt:

```text
SQL Server
└── Voraussetzungen
```

Die komplette Dokumentstruktur entsteht ausschließlich aus der Node-Hierarchie;
Überschriften werden erst beim Export aus ihr erzeugt
([Retrieval](Retrieval.md)).

## Heading-Validierung

Das Heading-Verbot wird über einen echten Markdown-Parser geprüft; eine einfache
Suche nach `#` ist unzulässig. Erlaubt bleiben beispielsweise:

```markdown
C#
```

```csharp
#if DEBUG
#endif
```

Erkannt und abgelehnt werden ATX-Headings (`#` bis `######`), Setext-Headings
(`===`/`---`-Unterstreichung) und HTML-Heading-Konstrukte (`<h2>` …). Fenced Code
Blocks werden nicht als Dokumentüberschriften interpretiert.

## Erlaubtes Markdown

Node-Content nutzt normales Markdown: Absätze, Listen, Tabellen, Links,
Inline-Code, Code-Blöcke, Hervorhebungen, Blockquotes. Nicht erlaubt sind
Markdown-Headings, HTML-Headings und persistiertes Front Matter für
Systemmetadaten. Systemmetadaten (`NodeId`, `RequestedAudience`, `ResolvedAudience`,
`SnapshotId`, `ContentRevisionId`, `Freshness`, …) sind Bestandteil der
strukturierten MCP-Antwort, nie des Contents. Falls eine Ausgabeform Front Matter
braucht, wird sie dynamisch erzeugt und nicht gespeichert.

## Kleine Nodes

Ein Node ist eine kleine fachliche Wissenseinheit, kein klassisches
Dokumentkapitel. Kleine Nodes bedeuten geringeren Tokenverbrauch, gezielteres
Retrieval, kleinere Änderungs- und Replace-Operationen, bessere
Wiederverwendbarkeit, bessere Drift-Erkennung und besseres agentisches Arbeiten.

Es gibt kein hartes Größenlimit. Die konfigurierbare Standardwarnschwelle
(`NodeTooLarge`) beträgt 4 KiB für den normalisierten UTF-8-Content eines Nodes
und nennt Ist-Größe, Schwelle und die Empfehlung, fachliche Unterpunkte als
Child-Nodes anzulegen ([Konfiguration und
Betrieb](Konfiguration-und-Betrieb.md)).

```text
Capture first, structure later.
```

Wissen geht nicht verloren, nur weil die Struktur gerade nicht optimal ist.

## Refactoring von Wissen

Wissensaufnahme und Wissensstrukturierung sind getrennte Tätigkeiten. Ein Node darf
während einer Entwicklungsphase anwachsen; ein späterer, eigener
Refactoring-Workflow teilt ihn mit den normalen Primitiven (`create_node`,
`move_node`, `update_node`, `replace_content`, `delete_content`, `delete_node`)
innerhalb einer eigenen Transaction. Eine automatische
`refactor_everything()`-Funktion existiert bewusst nicht; eine spätere Abfrage
`list_refactoring_candidates` ist möglich Erweiterung.

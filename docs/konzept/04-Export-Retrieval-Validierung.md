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

### Strategie für Hierarchietiefen größer als sechs Ebenen

Standard-Markdown (CommonMark) unterstützt ATX-Überschriften syntaktisch bis maximal Level 6 (`######`).
Übersteigt die relative Tiefe im exportierten Teilbaum sechs Ebenen:

1. **Clamping auf Level 6**: Alle Überschriften ab relativer Tiefe 6 erhalten das maximale Level 6 (`######`). Dadurch bleibt die Ausgabe in sämtlichen Markdown- und HTML-Parsern standardkonform und syntaktisch valide (keine Pseudotags oder als Fließtext degradierte `#######`).
2. **Transparente Qualitätswarnung**: Der Exporter fügt dem Ergebnis die Qualitätswarnung `HierarchyTooDeep` mit der tatsächlichen Tiefe hinzu.

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

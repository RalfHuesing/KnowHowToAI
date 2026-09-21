# Retrieval

## Metadata-First und Token-Effizienz

Agenten erhalten nicht standardmäßig große Teilbäume oder komplette
Dokumentationen. Der normale Retrieval-Flow:

```text
list_children → Metadaten prüfen → relevante Nodes auswählen → get_node
```

nicht: *gesamte Knowledge Base in den Kontext laden*. Suchergebnisse und Listen
liefern schlanke Metadaten und kleine Trefferkontexte, nie automatisch den
vollständigen Content aller Treffer.

## Navigation-Metadaten

Ein Node besitzt neben `Title` eine kurze zielgruppenunabhängige `Description` mit
nur Navigationskontext, keinem umfangreichen Fachwissen. `list_children` liefert
je Kind: `nodeId`, `title`, `description`, `sortOrder`, `childCount`,
`contentSizeBytes`, `availability`, `resolvedAudienceId`, `freshness`. Der Agent
entscheidet daran, welche Nodes er tatsächlich laden muss.

## Paginierung und Cursors

Große Mengen (Kind-Nodes, Zielgruppen, Suchergebnisse, Diffs, committed Snapshots, Releases) werden
seitenweise über **opake Keyset-Cursors** paginiert, gesteuert über die
`RetrievalPolicy` (`DefaultPageSize`, `MaximumPageSize`, `SearchPageSize`,
`SearchMaximumPageSize`; [Konfiguration und
Betrieb](Konfiguration-und-Betrieb.md)):

- `limit` gilt einheitlich: fehlend oder ≤ 0 ergibt die konfigurierte
  Standardseitengröße, Werte über dem Maximum werden auf das Maximum geklemmt.
- Cursor-Strings bleiben opak und werden unverändert weitergereicht.
- Der Cursor ist an Snapshot, Suchtext, Zielgruppe, die normalisierte Suchfilterauswahl und – bei Working Reads – an die
  `ChangeVersion` der Transaction gebunden. Eine zwischenzeitliche Mutation oder
  ein Wechsel des Current Snapshots führt stabil zu `CursorExpired`; eine falsche
  Snapshot-/Filterbindung zu `InvalidCursor`.
- `list_audiences` sortiert deterministisch nach `AudienceId` ordinal aufsteigend;
  `list_children` nach `sortOrder`, dann `NodeId`.
- Die Historienübersicht liest ausschließlich committed Snapshots, absteigend nach
  `SnapshotId`, über einen opaken Keyset-Cursor. Working und verworfene Snapshots
  gehören nicht zu dieser unveränderlichen Historie.

## Export

`export_tree(rootNodeId, audienceId, Selektor)` exportiert einen Teilbaum als
Markdown. Der ausgewählte Root ist immer Heading-Level 1; Kinder erhalten
entsprechend ihrer relativen Tiefe tiefere Level:

```markdown
# SQL Server
## Voraussetzungen
### Hardware
```

Weil gespeicherter Content niemals eigene Überschriften enthält, erzeugt der
Exporter Headings ausschließlich aus `Node.Title` und der relativen Hierarchietiefe
– ohne im Content enthaltene Level verschieben zu müssen.

Bei relativen Tiefen über sechs Ebenen: **Clamping auf Level 6** (CommonMark
erlaubt maximal `######`), damit die Ausgabe in jedem Parser standardkonform
bleibt, plus die transparente Qualitätswarnung `HierarchyTooDeep` mit der
tatsächlichen Tiefe.

Zielgruppenauflösung beim Export: für jeden Node wird die konfigurierte Zielgruppenauflösung
durchgeführt. Ein Node wird berücksichtigt, wenn für ihn Content auflösbar ist
**oder** mindestens ein exportierter Nachfahre existiert – Struktur-Nodes ohne
eigenen Content bleiben so erhalten, irrelevante Zweige entfallen vollständig.

## Search

Die Suche durchsucht Node-Titel, Descriptions und den aktiven auflösbaren Content
gemäß Zielgruppenauflösung.

Zielgruppenbezug:

- Ohne `audienceId` werden ausschließlich `Title` und `Description` durchsucht; es
  findet keine stille Auswahl eines Zielgruppen-Contents statt (Treffer ohne Zielgruppe:
  `Availability = None`).
- Mit `audienceId` werden die angefragte Zielgruppe und ihre vollständige Resolution Order
  mit denselben Regeln und stabilen Fehlercodes wie die Zielgruppenauflösung geprüft
  (`RequestedAudienceNotFound`, `RequestedAudienceDeleted`, `CandidateAudienceNotFound`,
  `CandidateAudienceDeleted`, `DuplicateCandidateAudience`, `DuplicatePriority`,
  `InvalidPriority`). Ein Auflösungsfehler ist ein Fachfehler, nie ein leeres
  Suchergebnis.
- Mit `audienceId` wird je Node exakt die erste Content-Auswahl der Resolution Order
  verwendet (`Explicit` für die angefragte Zielgruppe, `Fallback` für die erste
  Kandidatenzielgruppe mit aktivem Content). Ohne konfigurierte Order wird nur in
  `Title` und `Description` gefunden.

Deterministisches Ranking:

1. Rang 1: Treffer im `Title` (kein Snippet)
2. Rang 2: Treffer in der `Description` (Snippet aus Description)
3. Rang 3: Treffer im aktiven `Content` (Snippet zentriert um den Treffer)

Bei gleichem Rang erfolgt die Sortierung stabil nach `sortOrder` aufsteigend, dann
nach `NodeId`.

Die Suche kann Treffer zusätzlich nach der aufgelösten Content-Zielgruppe,
`Availability`, `Freshness` und vorhandenen Findings filtern. Mehrere Werte
derselben Facette gelten als Oder; unterschiedliche Facetten als Und. Ein
fehlender oder leerer Filter ist identisch zur ungefilterten Suche. Die Filterung
findet im Repository vor der SQL-Keyset-Seite statt; der Search-Use-Case reicht
die unveränderte Filterauswahl nur durch. Der bestehende Search-Finding-Vertrag
enthält den Befund `StaleDerivedContent` für den tatsächlich aufgelösten,
veralteten Derived Content; weitere transaktionsweite Qualitätswarnungen gehören
nicht zu einem einzelnen Suchtreffer.
Cursor sind deshalb an die normalisierte Filterauswahl gebunden. Ein Cursor einer
anderen Filterauswahl ist `InvalidCursor` und darf nicht für die neue Seite
weiterverwendet werden.

Sicherheit und Snippets: alle Abfragen sind ausnahmslos parametrisiert, es gibt
keine dynamische SQL-Konkatenation; SQL-LIKE-Sonderzeichen (`%`, `_`, `[`, `\`)
werden mit `ESCAPE '\'` escaped. Snippets besitzen eine konfigurierbare
Maximallänge (`SnippetMaximumCharacters`, Standard 300 Zeichen), werden zentriert
um den Suchbegriff extrahiert, Zeilenumbrüche auf Leerzeichen normalisiert und
Auslassungen mit `...` gekennzeichnet.

Bewusste V1-Grenze: die Textsuche ist eine exakte parametrisierte
Substring-Suche – kein Stemming, keine Lemmatisierung, keine Tippfehlertoleranz,
keine Vektorsuche ([Entscheidungen](Entscheidungen.md)).

## Content schreiben

Da Nodes bewusst klein sind, ist ein vollständiger Replace die
Standardoperation:

```text
replace_content(transactionId, nodeId, audienceId, contentMode, contentMd, sources?)
```

Die alte Version bleibt über den Base-Snapshot erhalten; ein zusätzliches
Delta-Format ist nicht nötig.

Für kleine punktuelle Änderungen existiert:

```text
replace_text(transactionId, nodeId, audienceId, oldText, newText)
```

Regeln: `oldText` genau 1x gefunden → ersetzen; 0x → `TextNotFound`;
>1x → `MultipleTextMatches` (ordinal, exakt). Das ergibt einen robusten
Patch-Mechanismus ohne Unified-Diff-Komplexität.

Unified Diff ist bewusst keine Kernoperation (Probleme mit Line Endings,
Context-Mismatch, Diff-Parser-Unterschieden, Dateisemantik); ein späteres
`apply_patch` wäre additiv möglich. Wissen wird ebenfalls nicht temporär in
lokale Dateien exportiert, damit Agenten sie bearbeiten (kein
DB→Temp-Datei→Edit→DB-Workflow, keine Dateisystemabhängigkeit, keine
Newline-/Synchronisationsprobleme). Der Markdown-Export dient der Ausgabe, nicht
dem internen Editing.

## Textnormalisierung

Gespeicherter Content besitzt ein kanonisches Format: UTF-8 mit LF.
Line-Ending-Unterschiede sind nicht Teil der fachlichen Änderungssemantik.

## Validatoren

Validatoren sind in zwei Kategorien getrennt:

**Harte Invarianten** – Operation oder Commit schlägt fehl:

- Raw HTML, Markdown- oder HTML-Heading im Content
- Front Matter, nicht erlaubtes Linkziel oder Markdown-/HTML-Bild im Content
- ungültige Parent-ID, Hierarchiezyklus, nicht existierende Node/Zielgruppe
- geschlossene Transaction
- `replace_text` ohne oder mit mehreren Matches
- ungültige Content-Dependency, ungültige Audience Resolution
- Commit auf veraltetem Base Snapshot

**Qualitätswarnungen** – die Änderung wird gespeichert:

- `NodeTooLarge`, `TooManyChildren`, `PossibleEmbeddedHeading`,
  `LargeContentReplace`, `HierarchyTooDeep`, `StaleDerivedContent`

Wissen darf wegen Qualitätswarnungen nicht verloren gehen: ein zu großer Node
wird mit Warnung gespeichert statt abgelehnt („Capture first, structure later",
[Wissenshierarchie](Wissenshierarchie.md)).

Neben der automatischen Validierung bei Schreiboperationen und Commit prüft
`validate_transaction(transactionId)` explizit und read-only: `isValid`, `errors`,
`warnings`, `staleContents` und `refactoringCandidates`. Harte Fehlerbefunde
erscheinen dort als Erfolg mit `isValid: false` im Payload.

## Messgestützte Performance-Entscheidungen

Die folgenden Entscheidungen sind mit realen SQL-Messungen gegen eine
repräsentative Datenmenge (401 Nodes, 440 Contents, 20 Dependencies, 3 Zielgruppen,
2 Resolution Orders; erzeugt ausschließlich über die produktiven
Working-Transaction/Commit-Pfade) getroffen und in den Abnahme-Integrationstests
`SqlSearchAbnahmeTests` (`SqlServer/Abnahme/`) laufend neu messbar:

- **Keine Search-Index-Migration.** Maximale 1795 logische Reads und ~23 ms
  Wall-Clock pro Suchseite, Ausführung < 1 ms; der Plan arbeitet mit Index Seeks
  und skaliert mit der Treffermenge pro Zielgruppe, nicht mit dem Gesamtbestand. Ein
  nicht sargbares `LIKE '%…%'` könnte Head-Lookups nur marginal verbessern.
- **Transitive Freshness in der Search-CTE.** Die rekursive SQL-CTE bewertet den
  vollständigen Derived-Dependency-Graphen vor Filterung und Keyset-Paging. Bei
  20 Derived-Treffern verursacht sie 7039 logische Reads und ~31 ms Wall-Clock
  (SQL-Ausführung < 1 ms); ein nachgelagertes Laden oder eine nur auf die Seite
  begrenzte Freshness-Bewertung würde die Parität zum allgemeinen Freshness-Use-Case
  gefährden. Wiederholungspunkt: etwa Faktor 100 größerer Content-Bestand.
- **Keine Kategorie-Offsets im `DiffCursor`.** Das History-Repository lädt pro
  Cursor-Seite beide Snapshots vollständig; Kosten pro Seite sind konstant
  (69 logische Reads, ~3 ms SQL, ~38 ms für den vollständigen 8-Seiten-Durchlauf
  über 373 Diff-Einträge), alle Statements greifen per Clustered Index Seek auf
  `SnapshotId` zu. Kategorie-Offsets würden die SQL-Ladung nicht reduzieren.

Bei deutlich größerer Datenmenge (etwa Faktor 100) sind die Messungen mit
denselben Testklassen zu wiederholen; erst wenn die genannten Kostenanteile
dominant werden, ist die jeweilige Optimierung zu implementieren.

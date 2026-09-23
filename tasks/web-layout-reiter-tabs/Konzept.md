---
status: ready
---

# Einheitliches Reiterlayout im Wissensbereich

## Intention

Im Wissensbereich sollen die fachlichen Reiter und anschließend ihr Inhalt die
visuelle Orientierung bestimmen. Da die Wissensbasis mit der Zeit wächst und
überwiegend gelesen werden wird, ist „Lesen“ die erste und beim Knotenwechsel
anfänglich aktive Ansicht. Ein gemeinsames Layoutmuster soll die
Reiterleiste oben, den primären Inhalt, davon getrennte ergänzende Inhalte und
die zugehörigen Aktionen auf den betroffenen Ansichten gleich anordnen. Der
Lese- oder Arbeitskontext bleibt erkennbar, ohne die Reiter zu überstrahlen.

## Belegter Ausgangspunkt

- Die Wissensroute hat im gemeinsamen `PageFrame` genau ein `h1`; die
  Knotendetails liegen rechts neben dem Wissensbaum. Die Reiterleiste steht
  derzeit hinter `NodeDetails`; die Standardansicht „Lesen“ rendert dadurch
  ihren vollständigen Inhalt vor den Reitern, während die anderen Ansichten
  nach den Reitern stehen
  ([KnowledgePage](../../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor),
  [NodeDetailsWorkspace](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Node/NodeDetailsWorkspace.razor),
  [NodeDetails](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Node/NodeDetails.razor)).
- Es gibt dort derzeit vier Ansichten: „Lesen“, „Titel und Beschreibung“,
  „Editor“ und „Technische Details“. Sie verwenden bereits native Schaltflächen mit
  eindeutigem Aktivzustand. Der Editor erhält nach dem Öffnen Fokus; einmal
  geöffnete Bearbeitungsansichten bleiben für dieselbe Node und Zielgruppe
  montiert, damit ungespeicherte Eingaben erhalten bleiben
  ([Web-UI-Gesamtbild](../../docs/WebUi.md)).
- Im Inhaltseditor steht „Speichern“ derzeit oberhalb der Eingabefläche; im
  Metadatenformular liegt die Aktionsgruppe darunter links
  ([ContentEditor](../../src/KnowHowToAI.Server/Web/Features/Content/ContentEditor.razor),
  [NodeMetadataEditor](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Components/NodeMetadataEditor.razor)).
- Der Inhaltseditor zeigt bisher zwei große Schaltflächen für „WYSIWYG“ und
  „Markdown-Quelle“ oberhalb der Textfläche. Seine Crepe-Werkzeugleiste
  konfiguriert Fett, Kursiv, Durchstreichen, Inline-Code und Link mit
  Buchstaben- bzw. Zeichenkürzeln; beim Einstieg ist keine feste
  Formatierungsleiste sichtbar
  ([ContentEditor](../../src/KnowHowToAI.Server/Web/Features/Content/ContentEditor.razor),
  [Toolbar-Konfiguration](../../src/KnowHowToAI.Server/Web/Features/Content/content-editor.js)).
- Die Knotenansichten sind derzeit die einzige produktive Reitergruppe der
  Weboberfläche. Der gemeinsame Seitenrahmen und die Entwurfsseiten besitzen
  kein solches Reitermuster.

## Betroffene Routen und Zustände

| Route | Reiterbereich | Zu prüfen |
|---|---|---|
| `/knowledge` | Bei gewähltem Knoten; sonst Leer-, Fehler- oder Initialzustand ohne Reiter | Current/Draft, Zielgruppe, Wechsel in eine Knotenauswahl |
| `/knowledge/{NodeId:guid}` | Gewählter Knoten einschließlich Direktaufruf und Reload | Alle vier Ansichten, Current/Draft, normaler/Fallback/fehlender/Derived Content |

Die Entwurfsrouten `/drafts` und `/drafts/{TransactionId:guid}` bleiben ohne
Reiter und dienen nur als Regression der bestehenden Navigation in den
Wissensbereich.

## Scope

### Muss

- Der Bereich mit Reitern beginnt innerhalb der rechten Knotenfläche mit der
  Reiterleiste. Der gemeinsame Seitenkopf mit dem einzigen `h1` bleibt als
  Seitenrahmen darüber erhalten. Auf die Reiterleiste folgt der Inhalt der
  aktiven Ansicht; kein aktiver Hauptinhalt erscheint davor.
- Die Reiter heißen und stehen in dieser Reihenfolge: „Lesen“, „Bearbeiten“,
  „Titel“, „Technische Details“. „Titel“ umfasst weiterhin Titel und
  Beschreibung. Bei neuer Node- oder Zielgruppenauswahl ist „Lesen“ aktiv.
  Die Leseansicht räumt dem eigentlichen Wissensinhalt den größten visuellen
  Raum ein; der Status „Nur lesen“ ist keine konkurrierende Überschrift.
- Die vorhandenen Knotenansichten verwenden eine gemeinsame, rein darstellende
  Reiterlayout-Komponente: oben eine Leiste mit Beschriftung und eindeutigem
  Aktivzustand, darunter die Bereiche für primären Inhalt, erkennbar
  separierte Zusatzinformationen und ansichtsbezogene Aktionen. Die
  Zusatzinformationen stehen unter dem Hauptinhalt der jeweiligen Ansicht;
  wo es keine gibt, entfällt dieser Bereich. „Technische Details“ bleibt
  ein eigener Reiter und wird nicht in den anderen wiederholt. Wichtige
  Fallback-/Derived-Hinweise stehen zusätzlich direkt beim betroffenen Inhalt
  oder der Bearbeitung, damit dessen Bedeutung sofort klar ist. Die Komponente
  ist für spätere Reiterflächen wiederverwendbar. Der Knoten-Workspace behält
  die Auswahl der aktiven Ansicht und den Lebenszyklus der einmal geöffneten
  Bearbeitungskomponenten; die Layout-Komponente übernimmt weder fachlichen
  Zustand noch Speicherlogik.
- Der Read-/Draft-Kontext („Nur lesen“/„Arbeitskopie“) steht bei ausreichender
  Breite dezent rechts neben den Reitern, bei schmaler Breite in einer eigenen
  Zeile darunter. Er bleibt sichtbar und eindeutig, tritt aber in der
  visuellen Hierarchie hinter Reitern und Inhalt zurück.
- Speichern steht in jeder schreibbaren Ansicht, die eine Speichern-Aktion
  besitzt, unter ihrem Inhalt ganz rechts. Weitere Aktionen derselben Ansicht
  stehen in der rechtsbündigen Aktionsgruppe davor; im Titel-Formular steht
  „Abbrechen“ damit links von „Speichern“. Statusmeldungen bleiben in der
  Nähe der Aktionen erkennbar. Metadaten und Inhalt behalten getrennte
  Speichern-Aktionen und ihre bisherigen fachlichen Verträge. In rein lesenden
  Zuständen erscheint keine wirkungslose Speichern-Aktion.
- „Bearbeiten“ hat eine ruhige, durchgehende Reihenfolge: kompakte
  Werkzeugleiste, großzügige Textfläche, darunter eine Abschlusszeile.
  Die Werkzeugleiste gehört sichtbar zum Editor und bietet im visuellen Modus
  die bisher verfügbaren Formatierungen Fett, Kursiv, Durchstreichen,
  Inline-Code und Link als verständliche, konsistent gestaltete Symbole.
  Jedes Symbol hat einen eindeutigen zugänglichen Namen und einen erkennbaren
  aktiven Zustand für die aktuelle Textauswahl. Ein Klick wendet die
  Formatierung auf diese Auswahl an und lässt den Editor anschließend weiter
  bedienbar; bloße Buchstaben-Kürzel sind nicht die Zielgestaltung.
  Eine zweite, gleichzeitig auftauchende Formatierungsleiste bleibt aus.
  Eine zusätzliche Icon-Bibliothek ist dafür nicht erforderlich.
- Der Ansichtswechsel steht als kompakte, beschriftete Auswahl am rechten
  Ende der Werkzeugleiste: „Visuell“ und „Markdown-Quelle“ statt zweier großer
  Modusschaltflächen. Im Quellmodus werden keine wirkungslosen visuellen
  Formatierungsschaltflächen angeboten. Der Wechsel erhält den gesamten
  ungespeicherten Inhalt, Dirty-State und die bisherige Fokusführung.
- Die Abschlusszeile zeigt den Speicherstatus links und „Speichern“ ganz
  rechts. Sie steht direkt unter der Textfläche im normalen Dokumentfluss und
  wird beim Scrollen nicht angeheftet. „Gespeichert“, „Ungespeicherte
  Änderungen“ und „Wird gespeichert…“ bleiben als Text erkennbar.
  Paste-Reduktion, Validierungswarnungen und Fehler erscheinen separat beim
  Editor und verdrängen den Speicherstatus nicht.
- Die Bearbeiten-Ansicht verwendet die vorhandenen Design-Tokens für eine
  zusammengehörige Werkzeugleiste und Textfläche mit zurückhaltenden
  Begrenzungen, klaren Abständen und einheitlichen Buttonmaßen. Bei schmaler
  Breite darf die Leiste umbrechen; alle Werkzeuge und die Ansichts-Auswahl
  bleiben ohne horizontalen Seiten-Scrollbalken erreichbar.
- Node- und Zielgruppenwechsel, Dirty-Schutz, Editor-Fokus, Fallback-/Derived-
  Einordnung, Fehlermeldungen und Warnungen bleiben funktionsfähig und an der
  jeweiligen Ansicht auffindbar.
- Die Reiter bleiben native Schaltflächen mit klar erkennbarer aktiver Ansicht.
  Es werden keine ARIA-Tabrollen ohne das dazugehörige Tastaturmuster und keine
  eigene Reiter-Tastatursteuerung eingeführt.
- Das Muster nutzt die bestehenden Design-Tokens und die gemeinsame
  Seitenbreite. Es bleibt bei Desktopbreite sowie beim vorgegebenen 200-/400-%-
  Zoom ohne abgeschnittene Reiter, Inhalte oder Aktionen bedienbar.

### Inhalt je Reiter

| Reiter | Primärer Bereich | Separierter Zusatzbereich | Untere Aktion |
|---|---|---|---|
| Lesen | Gerendertes Dokument oder verständlicher Leerzustand; Fallback-/Derived-Kennzeichnung unmittelbar beim Inhalt | Gewählte Zielgruppe und aufgelöste Fallback-Zielgruppe | Keine |
| Bearbeiten | Sichtbare Werkzeugleiste und Crepe-Textfläche oder Markdown-Quelle; bei Derived Content stattdessen die vorhandene Lesesperren-Erklärung, bei Fallback eine leere eigene Fassung mit Erklärung; Eingabefehler unmittelbar bei der Bearbeitung | Weitere vorhandene, nicht handlungsentscheidende Hinweise | Speicherstatus links und Speichern rechts, nur wenn Schreiben zulässig ist |
| Titel | Titel- und Beschreibungsfelder mit Validierung; bei fehlender Schreibbarkeit lesbare Werte | Kein leerer Platzhalter | Abbrechen links von Speichern, beide rechtsbündig, nur wenn Schreiben zulässig ist |
| Technische Details | Vorhandene Angaben zu Zielgruppe, Verfügbarkeit, Aktualität, Inhaltsmodus, Position und Revision; ohne zusätzliche aufklappbare Wiederholung des Reiternamens | Vorhandene Herkunftsliste, wenn Quellen existieren | Keine |

Die Bereiche bleiben im jeweiligen Reiter; es gibt keinen gemeinsamen
Speichern-Button für mehrere Reiter. In einem nicht schreibbaren Kontext
bleiben Lesen, Titel, Bearbeiten und Technische Details erreichbar, aber
Bearbeiten und Titel bieten keine wirkungslosen Mutationsaktionen.

### Nicht

- Keine neue Wissensfunktion, kein anderer Persistenz-, Transaktions-,
  Zielgruppen- oder Routenvertrag.
- Keine neuen Formatierungsarten, keine neue Editorbibliothek und kein
  zusätzlicher visueller Editor neben dem bestehenden Crepe-/Markdown-Modus.
- Kein Umbau von Wissensbaum, Shell oder Entwurfsseiten ohne dort vorhandenes
  Reitermuster. Kein neues Tastaturmuster für visuelle Reiter.
- Keine vorgezogene Implementierung weiterer möglicher Reiterseiten allein,
  um die Wiederverwendbarkeit zu demonstrieren.

## Technische Struktur für die Umsetzung

Die gemeinsame Darstellung liegt unter
`src/KnowHowToAI.Server/Web/Components/Shared/Tabs/` im Namespace
`KnowHowToAI.Server.Web.Components.Shared.Tabs`:

| Baustein | Verantwortung |
|---|---|
| `TabDefinition.cs` | Unveränderlicher Wert aus stabilem Schlüssel und sichtbarer Beschriftung; keine fachlichen Node- oder Editorbegriffe. |
| `TabLayout.razor`, `.razor.cs`, `.razor.css` | Nimmt Reiterdefinitionen, aktiven Schlüssel, Auswahl-Callback, Kontext-Fragment und Panel-Fragment entgegen. Rendert daraus native Reiterschaltflächen mit aktivem Zustand, den Kontext rechts und danach die Panels. Ein Klick meldet nur den gewählten Schlüssel an den Aufrufer. Keine eigene Auswahlpersistenz, kein Editor-Lebenszyklus. |
| `TabPanelLayout.razor`, `.razor.cs`, `.razor.css` | Nimmt Sichtbarkeit und Fragmente für primären Inhalt, getrennte Zusatzinformationen, Status links und Aktionen rechts entgegen. Ein verborgenes Panel bleibt montiert; fehlende Fragmente erzeugen keine leeren sichtbaren Regionen. |

Die Komponenten übernehmen nur die Anordnung und das visuelle System. Sie
kennen keine `NodeId`, `TransactionId`, `ChangeVersion`, Speichern-Methode oder
Crepe-Instanz. Die Reiter bleiben eine beschriftete Navigation aus nativen
Buttons mit `aria-pressed`; `role="tablist"`/`role="tab"` und eigene
Pfeiltastensteuerung werden nicht eingeführt.

Die fachliche Einbindung bleibt in den vorhandenen Dateien:

- [`NodeDetailsWorkspace.razor[.cs|.css]`](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Node/NodeDetailsWorkspace.razor)
  definiert die vier Reiter, übersetzt den gewählten Schlüssel in die
  bestehende `NodeView` und behält Initialansicht, Node-/Zielgruppenwechsel,
  Lazy-Mount und verborgen montierte Editoren. Es rendert `TabLayout` vor
  sämtlichen aktiven Panelinhalten.
- [`NodeDetails.razor`](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Node/NodeDetails.razor)
  liefert Leseinhalt und technische Angaben in die passenden Panels; seine
  bisherige vorangestellte Status-Kopfzeile wird in den Kontext-Slot von
  `TabLayout` verschoben. [`KnowledgePage.razor`](../../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor)
  und `PageFrame` behalten das einzige Seiten-`h1` und die Baum-/Dokumentgrenze.
- [`NodeMetadataEditor.razor[.css]`](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Components/NodeMetadataEditor.razor)
  behält sein Formular und die getrennte Mutation und nutzt `TabPanelLayout`;
  die Aktionsgruppe wandert in dessen unteren Bereich, mit „Abbrechen“ vor
  „Speichern“.
- [`ContentEditor.razor[.cs|.css]`](../../src/KnowHowToAI.Server/Web/Features/Content/ContentEditor.razor)
  behält Markdown-Wert, Dirty-State, Moduswechsel und Speichermethode. Sein
  sichtbarer Aufbau nutzt `TabPanelLayout` mit kompakter Werkzeugleiste und
  Crepe-Fläche oder Quell-Textarea als primärem Inhalt sowie Status/Aktion
  unten. Die native
  `select`-Auswahl ruft die vorhandenen Wechselpfade auf. Die Formatbefehle
  werden über [`ContentEditor.razor.js`](../../src/KnowHowToAI.Server/Web/Features/Content/ContentEditor.razor.js)
  an die bereits in [`content-editor.js`](../../src/KnowHowToAI.Server/Web/Features/Content/content-editor.js)
  verwendeten Milkdown-Commands angebunden; die bisher kontextuell
  erscheinende Crepe-Leiste wird für diesen Editor deaktiviert, damit keine
  doppelte Werkzeugleiste entsteht. Repo-eigene kleine SVG-Symbole mit
  Textnamen/Tooltip ersetzen die bloßen Kürzel; keine neue Icon-Abhängigkeit.

`NodeDetails` verwendet `TabPanelLayout` ebenfalls für Lesen und Technische
Details. Die Feature-Komponenten behalten ihre eigenen fachlichen Hinweise;
die gemeinsamen Layoutkomponenten entscheiden nicht, welche Hinweise wichtig
sind oder wann eine Speichern-Aktion erlaubt ist.

Die neue Tab-Struktur wird an der einzigen vorhandenen Reitergruppe benutzt.
Künftige Bereiche können dieselben Komponenten verwenden, ohne Knoten- oder
Editorlogik zu übernehmen. Die geänderte Ist-Beschreibung gehört beim
Umsetzungsschritt in [`docs/WebUi.md`](../../docs/WebUi.md), nicht schon in
dieses Soll-Konzept.

Prüfankerpunkte sind
[`NodeDetailsPaneTests.cs`](../../tests/KnowHowToAI.Web.Tests/Features/Knowledge/NodeDetailsPaneTests.cs)
für Reiterwechsel und erhaltene Eingaben,
[`ContentEditorTests.cs`](../../tests/KnowHowToAI.Web.Tests/Features/Content/ContentEditorTests.cs)
für Modus, Status und Speichern,
[`ContentEditorSourceSmokeTests.cs`](../../tests/KnowHowToAI.BrowserTests/Editor/ContentEditorSourceSmokeTests.cs)
für den echten Markdown-Wechsel,
[`KnowledgeDirectEditingSmokeTests.cs`](../../tests/KnowHowToAI.BrowserTests/Transactions/KnowledgeDirectEditingSmokeTests.cs)
für Dirty-/Speicherabläufe und
[`PageFrameSmokeTests.cs`](../../tests/KnowHowToAI.BrowserTests/ReadOnly/PageFrameSmokeTests.cs)
für die Browserbreiten. Die gemeinsamen Tabs erhalten eigene schmale
Komponententests unter `tests/KnowHowToAI.Web.Tests/Components/Shared/Tabs/`.

## Verifikation

- Die Reiterleiste ist in der rechten Knotenfläche die erste Zeile; in jeder
  betroffenen Ansicht folgen primärer Inhalt, getrennte Zusatzinformation und
  ggf. rechtsbündige Aktionen in konsistenter Reihenfolge.
- Die Reiter heißen in der festgelegten Reihenfolge „Lesen“, „Bearbeiten“,
  „Titel“, „Technische Details“; der erste ist bei Knoten- und Zielgruppenwechsel
  aktiv. „Titel“ enthält weiterhin Titel und Beschreibung.
- „Bearbeiten“ zeigt im visuellen Modus die kompakte Formatierungsleiste mit
  Symbolen und Ansichts-Auswahl über der Textfläche. Die fünf vorhandenen
  Formatierungen wirken auf die ausgewählte Stelle, und ihr Aktivzustand passt
  zur Textauswahl. Im Quellmodus bleibt die
  Auswahl erreichbar und der vollständige Markdown-Wert erhalten. Unter der
  Textfläche stehen Speicherstatus links und „Speichern“ rechts; Fehler und
  Warnungen bleiben separat lesbar.
- Normaler, Fallback-, fehlender und Derived Content sowie Current-/Draft-
  Kontext zeigen verständliche Hinweise und nur zulässige Aktionen.
- Die vorhandenen Wechsel- und Speicherabläufe erhalten ungespeicherte Werte;
  die Wissensrouten behalten genau ein `h1` und ihre bisherigen Funktionen.
- Komponententests prüfen die gemeinsame Reiter- und Panelstruktur sowie den
  aktiven Zustand; die bestehenden Tests für Knotenansichten und Inhaltseditor
  prüfen Reiterwechsel, verborgen erhaltene Eingaben, Moduswechsel,
  Formatierungsaktionen und getrennte Speichern-Pfade.
- Browserprüfung der betroffenen Routen und Zustände mit Computed Styles,
  Innenkanten, vertikaler Reihenfolge, Overflow, Umbruch und erreichbaren
  Aktionen bei 1280, 1024, 640 und 320 CSS-Pixeln; gemeinsame Sichtprüfung
  der Screenshots. Die manuelle Zoom-Checkliste ergänzt dies.

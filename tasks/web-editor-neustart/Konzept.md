---
status: ready
---

# Web-Fundament und Editor für die Wissenspflege

## Intention

Ein Mensch soll eine Node im Baum öffnen, ihren Text oder Titel ohne Vorarbeit
ändern und zwei Nodes an eine andere Stelle ziehen können. Die nötige
Versionierung schützt das Wissen, ohne vor einer kleinen Änderung einen
Transaction-Workflow zu verlangen. Das erste Ergebnis ist bewusst klein:
ein gut bedienbarer Wissensarbeitsplatz mit einem verlässlichen Entwurf und
einem Abschluss für die Änderungen. Die gemeinsame Navigation, der
Seitenrahmen und der URL-/Zustandsvertrag sind dabei das Fundament für
spätere Funktionen. Zusätzliche Seiten sollen sich dort einfügen können,
ohne das Layout oder die Bedienung des Editors neu zu entwerfen.

## Zielbild

```text
Schmale globale Navigation | kontextueller Baum | Node-Dokument
                         |                    | Titel, Beschreibung, Editor
Auf der Entwurfsseite:    | kein Baum           | Änderungen, Prüfung, Abschluss
```

Der Einstieg öffnet den Wissensarbeitsplatz. Die Node ist dort die Hauptfläche.
`Bearbeiten` bringt den Fokus direkt zum Text oder Titel. Beim ersten Speichern
oder erfolgreichen Drag-and-drop beginnt im Hintergrund ein Entwurf, sofern
noch keiner aktiv ist. Weitere Änderungen gehören zu diesem Entwurf.

## Seiten und Navigation

Das erste Ergebnis hat mehrere URLs mit klar getrennten Aufgaben:

| URL | Aufgabe |
|---|---|
| `/` | Öffnet den Wissensarbeitsplatz unter `/knowledge`. |
| `/knowledge` und `/knowledge/{NodeId}` | Baum-Navigation und Node lesen oder bearbeiten; die gewählte Node steht in der URL. Ohne Auswahl zeigt die Hauptfläche eine einfache Einladung zur Auswahl. |
| `/drafts` | Offene Entwürfe finden, fortsetzen oder wechseln; ohne Auth keine Liste „meiner“ Entwürfe behaupten. |
| `/drafts/{TransactionId}` | Änderungen prüfen, validieren, übernehmen oder verwerfen; bei Konflikt den nötigen Diff und nächste Schritte zeigen. |

Die globale Navigation ist von Anfang an als kompakter, erweiterbarer Bereich
links vorgesehen. Sie enthält zunächst `Wissen` und `Entwürfe`. Spätere Links
können dort hinzukommen. Der Wissensbaum ist eine zweite, ausschließlich auf
der Wissensseite sichtbare Fläche mit anderer Aufgabe. Es entstehen keine zwei
breiten Navigationsspalten. Auf der Entwurfsseite ist der Raum für Prüfung
und Diff frei.

Alle Seiten nutzen denselben Shell-Inset, Page-Root, Header-/`h1`-Vertrag,
dieselben sichtbaren Innenkanten und dieselben zentralen Tokens für Typografie,
Abstände und Zustände. Der Node-Titel ist das `h1` der geöffneten Wissensseite;
ohne Node und auf Entwurfsseiten gibt es ein eigenes `h1`. Die vorhandenen
Layoutgrundlagen werden geprüft und passend wiederverwendet. Die Hauptflächen
haben je nach Aufgabe unterschiedliche Inhalte.

## Verträge des Fundaments

### Shell und Seiten

- Die Shell besitzt ausschließlich die globale Navigation, den gemeinsamen
  Innenabstand, den Seitenkopf-Vertrag und globale Rückmeldungen. Sie kennt
  keine Node-, Tree- oder Transaction-Fachlogik. Ein späterer Bereich ergänzt
  Navigation und Route, ohne das Shell-Raster zu ändern.
- Jede routbare Seite besitzt genau einen Page-Root über die volle verfügbare
  Breite, genau ein semantisches `h1` und dieselben Innenkanten. Die Seite
  liefert Titel, Breadcrumbs und Aktionen an dafür vorgesehene Bereiche.
  Begrenzte Lesebreite gilt nur für Prosa und Editorinhalt, nicht für Baum,
  Formulare, Diff oder Aktionsflächen. Auch beim Inline-Edit des Node-Titels
  bleibt ein eindeutiges `h1` erhalten.
- Es gibt ein `main`-Landmark der Shell. Wissensbaum und Node-Dokument sind
  darin benannte Bereiche, kein zweites `main`. Skip-Link, Fokusführung,
  Lade-, Leer-, Fehler- und Dialogzustände gelten für alle Routen gleich.
- Globale Navigation und Baum sind getrennt. Der Baum gehört allein zur
  Wissensseite. Auf schmaler Breite und bei Zoom bleiben beide über klar
  beschriftete Auslöser erreichbar; sie verdrängen den Editor nicht.
- Die vorhandenen CSS-Tokens, Reset-, Accessibility- und Basisprimitive sowie
  `page-frame` bilden den Ausgangspunkt. Lokale Seitenstyles dürfen den
  gemeinsamen Seitenvertrag nicht durch eigene Außenabstände oder
  Breitenbegrenzungen übersteuern.

### URL und Arbeitszustand

- `NodeId` steht im Wissenspfad, `TransactionId` im Entwurfspfad. Gewählte
  Zielgruppe und aktiver Entwurf werden in teilbaren Node-URLs über die
  Query-Parameter `audienceId` und `transactionId` abgebildet. Ohne
  `transactionId` wird Current gelesen; ein Direktaufruf oder Reload
  rekonstruiert dieselbe Ansicht.
  Flüchtiger Blazor-Circuit-State beschleunigt die Bedienung, ist aber keine
  fachliche Wahrheit.
- Ein Entwurf gilt auf Wissens- und Entwurfsseite als derselbe Arbeitsstand.
  Seine gespeicherten Änderungen bleiben beim Node-Wechsel erhalten.
  Browserlokale Präferenzen dürfen den Wiedereinstieg erleichtern, aber keinen
  anderen offenen Entwurf stillschweigend auswählen.
- Unsaved/Dirty schützt auch Wechsel über die globale Navigation, Browser-Zurück
  und Zielgruppenwechsel. Ein Commit oder Discard bezieht sich ausschließlich
  auf den sichtbar ausgewählten Entwurf. Fehler nach dem Anlegen eines Entwurfs
  dürfen dessen Existenz nicht verbergen.
- Eine einzige verfügbare Zielgruppe wird sichtbar ausgewählt und in der URL
  festgehalten; das initiale SQL-Seed enthält `Default`. Bei mehreren
  Zielgruppen wird die ausdrücklich gewählte oder zuletzt genutzte verwendet.
  Fehlt beides, zeigt der Inhaltsbereich eine Auswahl am Arbeitsort, statt
  still Content einer Zielgruppe anzuzeigen. Struktur und Titel bleiben
  bis dahin navigierbar und bearbeitbar.

### Wiederverwendung und Austausch

- Vor der Umsetzung werden gemeinsame Tokens, `page-frame`, Dialoge,
  Feedback- und Dirty-Schutz, die Editor-Anbindung sowie Tree-Paging,
  Pfadladen und Move-Use-Case auf ihre konkreten Verträge geprüft. Passende
  Bausteine werden weiterverwendet oder angepasst.
- Die bestehende Zusammensetzung von `MainLayout`, Knowledge-Seite und
  technischen Kontextflächen ist kein zu konservierender Layoutvertrag.
  Gleichartige Verantwortung wird genau einmal verortet: Shell-Inset,
  Page-Root, Feature-Bereich, Prosa-Maß.
- Entfernte Routen und ihre Tests werden bewusst auf den neuen Routenumfang
  abgestimmt. Bestehende visuelle Baselines werden nicht blind aktualisiert.

## Bestandsgrenze und Umbau

Die folgende Grenze gilt für die **Web-Präsentation**, nicht für fachliche
Fähigkeiten des Gesamtsystems. „Behalten“ bedeutet Vertrag und geeigneten
Baustein erhalten, nicht jede heutige Implementierungszeile unverändert lassen.

| Bereich im aktuellen Stand | Entscheidung für das erste Ergebnis |
|---|---|
| `Core`, `Storage.SqlServer`, MCP, Host und ihre fachlichen Tests | Behalten. Die Oberfläche nutzt vorhandene Application-Use-Cases; keine fachlichen Dienste entfernen, weil eine Web-Seite entfällt. `DummyCurrentUserService` liefert weiterhin den Actor. |
| `Web/Components/App.razor`, `Routes.razor`, `Layout/Shell/ReconnectModal*`, `Layout/Shell/NavigationProtection*` | Behalten und an den neuen Routen- und Dirty-Vertrag anpassen. Genau eine Shell und ein `main`. |
| `Web/Components/Layout/Shell/MainLayout*`, `Layout/PageRegions/*` | Das gegenwärtige breite Navigations-/Kontext-Raster ersetzen. `PageRegionState`, Breadcrumbs und Aktionen nur übernehmen, soweit sie den einheitlichen Seitenkopf ohne Fachlogik tragen; Navigation als kompakte globale Leiste neu zusammensetzen. |
| `Web/Components/Layout/Context/*`, `Web/State/ContextSelectorState.cs`, `WebReadContextResolver*` | Technischen Snapshot-/Release-/Transaction-Kontextselektor aus der neuen Wissensoberfläche entfernen. Zielgruppenwahl und sichtbarer Entwurf werden am Arbeitsort bzw. im schmalen Shell-Indikator angeboten. Nicht benötigte UI-Adapter nach Umstellung entfernen. |
| `Web/Features/Knowledge/*` | `KnowledgePage`, `NodeDetails*`, `Components/NodeDetailsPane*`, `NodeMetadataEditor*` und `RootNodeEditor*` als dokumentzentrierten Arbeitsplatz neu zusammensetzen. Tree-Paging, Pfadladen, Request-Koordination, Move-Logik, sicherer Markdown-Renderer und Breadcrumb-Daten nach Vertragsprüfung gezielt übernehmen. `NodeDeletionEditor*` gehört nicht zum ersten Umfang und entfällt als Web-UI. Der Baum bleibt eine eigene Feature-Fläche, nicht Teil der Shell. |
| `Web/Features/Content/ContentEditor*` und zugehöriges JS | Vorhandene Editor-Anbindung als Ausgangspunkt behalten und für den direkten Speichern-/Dirty-Ablauf anpassen; keine zweite Editor-Technologie einführen. |
| `Web/Features/Transactions/*`, `Web/Components/Shared/Diffs/*` | Vorhandene Listen-, Diff- und Validierungsfunktionen als fachliche Bausteine prüfen; die technischen Transaction-Seiten durch `/drafts` und `/drafts/{TransactionId}` ersetzen. Die sichtbare Sprache und Aufgabenfolge heißen Entwurf, prüfen, übernehmen, verwerfen. Keine bloße Umbenennung der alten Seite. |
| `Web/Components/Shared/{Dialogs,Feedback,States}/*`, `Web/State/ToastState.cs` | Passende, allgemein nutzbare Zustands-, Dialog- und Feedbackbausteine behalten und am neuen Seitenvertrag prüfen. |
| `Web/State/WorkspaceState.cs`, `BrowserAudienceStorageService.cs` | Auf kleinen flüchtigen UI-Zustand und eine optionale letzte Zielgruppe zuschneiden. URL, persistierte Transaction und Serverantwort bleiben maßgeblich; keine versteckte globale Auswahl eines fremden Entwurfs. |
| `Web/Features/{Dashboard,Search,History,Audiences}/*` | Web-Seiten samt exklusiven Präsentationsmodellen und Routen entfernen. Ihre Core-/MCP-Fähigkeiten und nötige Zielgruppen-Leseauswahl bleiben bestehen. `/` leitet nach `/knowledge`; alte URLs erhalten keinen Kompatibilitätsweg. |
| `Web/Endpoints/MarkdownDownload*` | Web-Exportendpunkt, Registrierung, zugehörige UI und exklusive Tests entfernen. `WebEndpointRegistration` selbst bleibt für Host-, Asset- und Razor-Routen. Keine Entfernung eines etwaigen MCP-Exports. |
| `wwwroot/css/{tokens,base,app.css}` | Tokens und tragfähige Reset-, Accessibility-, Button- und Layoutprimitive erhalten; alte route- oder kontextspezifische Regeln bereinigen. Feature-CSS bleibt lokal. |
| Web-, Browser- und HTTP-Tests sowie `docs/` | Bestehende Nachweise für weiter geltende Verträge anpassen, obsolete UI-/Export-Tests entfernen und neue Abläufe belegen. `docs/` erst mit implementiertem Verhalten aktualisieren. Keine blinde Screenshot-Baseline-Übernahme. |

Die Dateiinventur vor jedem Löschschritt muss Referenzen in Host, Tests und
Dokumentation prüfen. Ein exklusiv entfallener Web-Baustein wird entfernt;
ein von verbleibenden Abläufen genutzter Baustein wird passend umgesetzt.
Alte UI-Routen bleiben im Endzustand weder navigierbar noch als versteckte
Paralleloberfläche bestehen. Rückbau und Neuaufbau erfolgen in grünen,
benutzbaren Slices, nicht durch eine vorausgehende Massenlöschung.

## Zielstruktur und Ownership

Die Struktur bleibt unter `src/KnowHowToAI.Server/Web/`; Namespace und Ordner
spiegeln einander. Sie ist eine Verantwortungsgrenze, kein Auftrag, passende
Dateien nur aus kosmetischen Gründen zu verschieben:

```text
Web/
  Components/
    App.razor, Routes.razor
    Layout/
      Shell/             MainLayout, Reconnect, NavigationProtection
      Navigation/        globale Navigation, Entwurfsindikator
      PageRegions/       gemeinsamer Seitenkopf, Breadcrumbs, Aktionen
    Shared/
      Dialogs/, Feedback/, States/, Diffs/
  Features/
    Knowledge/
      KnowledgePage
      Tree/              Baumdarstellung, Paging, Pfad, Move
      Node/              Dokumentansicht, Titel/Beschreibung, Editierfluss
    Content/             bestehende Editor-Anbindung
    Drafts/              Übersicht, Detail, Prüfung, Diff, Abschluss
  Workflow/              gemeinsames Begin/Resume/Write für Web-Aktionen
  State/                 nur routeübergreifender flüchtiger UI-Zustand
  WebServiceRegistration.cs
  WebEndpointRegistration.cs
```

Die Shell darf keine Tree-, Node- oder Transaction-Use-Cases besitzen.
`Features/Knowledge/Tree` verwaltet Navigation und Strukturinteraktion;
`Features/Knowledge/Node` die geöffnete Node und ihre Eingaben;
`Features/Drafts` die explizite Prüfung und den Abschluss. Eine schlanke,
gemeinsam genutzte Start-/Fortsetzungskoordination für Entwürfe gehört nach
`Web/Workflow` und wird vom Baum und Node-Editor benutzt, nicht
in beiden Komponenten dupliziert. Sie führt ausschließlich die bestehenden
Transaction-Use-Cases aus. C#-Namespaces folgen dem tatsächlichen Pfad,
z. B. `KnowHowToAI.Server.Web.Features.Knowledge.Tree` und
`KnowHowToAI.Server.Web.Features.Drafts`. Zentrale CSS-Dateien besitzen
Tokens und Grundprimitiven, `.razor.css` jeweils nur die Komponente.

## Kernabläufe

### Text und Node-Daten ändern

1. Node im Baum auswählen. Titel, Beschreibung und Inhalt der gewählten
   Zielgruppe erscheinen unmittelbar.
2. `Bearbeiten` öffnet den Editor am Inhalt. Titel und Beschreibung lassen
   sich ebenfalls am Ort ihres Erscheinens bearbeiten. Es gibt kein
   vorgeschaltetes Formular für Transaction, Actor, Client oder Zweck.
3. `Speichern` persistiert explizit in den aktiven Entwurf. Ohne aktiven
   Entwurf wird er bei diesem ersten Write angelegt. Die Oberfläche zeigt
   danach den Entwurf und die gespeicherten Änderungen an.
4. Ungespeicherte Eingaben werden vor Node-Wechsel, Navigation und Schließen
   geschützt. Es wird weder still gespeichert noch still verworfen.

Vor dem ersten Write vergleicht die Oberfläche den aktuell maßgeblichen
Snapshot mit dem Stand, aus dem der angezeigte Node und Baum geladen wurden.
Hat sich Current inzwischen geändert, wird der Write angehalten und eine
bewusste Neuladung angeboten; die lokale Eingabe wird nicht heimlich auf den
neueren Stand geschrieben. Gleichzeitige erste Aktionen im selben
Arbeitsbereich erzeugen höchstens einen aktiven Entwurf. Ist der Entwurf
angelegt, schlägt aber die Mutation fehl, bleibt er sichtbar und erreichbar.

### Baum ändern

Ein Node lässt sich direkt im Baum anlegen, umbenennen und per Drag-and-drop
sichtbar vor, nach oder unter eine andere Node bewegen. Ein Drop ohne aktiven
Entwurf beginnt ihn und speichert die Verschiebung. Ein abgelehnter Drop
stellt den bestätigten Serverzustand wieder her und erklärt den Fehler.
Baum, Auswahl und Editor bleiben nach der Änderung synchron.
Große Bäume werden Ast für Ast geladen; der sichtbare Pfad zur ausgewählten
Node bleibt beim Navigieren und nach Strukturänderungen nachvollziehbar.
Ist die Wissensbasis noch ohne Root, bietet dieselbe Hauptfläche die Aktion
`Ersten Node anlegen` an; auch diese Anlage beginnt den Entwurf erst beim
Speichern.

### Änderungen abschließen

Ein klarer Entwurfsindikator führt zur eigenen Entwurfsseite mit
`Änderungen übernehmen` und `Entwurf verwerfen`. Die Prüfung und der Commit
sind explizit, aber keine Voraussetzung, um mit der nächsten Node im selben
Entwurf weiterzuarbeiten. Validierungsfehler und Snapshot-Konflikte nennen
die betroffenen Änderungen und zeigen einen sicheren nächsten Schritt.

Nach einem Reload kann der Mensch den aktiven Entwurf wiederfinden. Andere
offene Entwürfe werden nicht still übernommen; sie sind unter `/drafts`
sichtbar. Ohne Auth gibt es keine verlässliche Aussage über „meine“ Entwürfe.
Die bestehende Fake-User-Implementierung liefert den Actor; Anmeldung und
Rechte kommen später. Die unsichtbaren Begin-Metadaten sind für diesen
Webfluss fest: `Client = "Web UI"`, `Purpose = "Wissenspflege"`, Actor aus
`ICurrentUserService`. Sie dürfen den ersten Edit nicht blockieren.

## Fachliche Leitplanken

- `Entwurf` bezeichnet eine offene KnowHowToAI-Transaction. Jeder persistierte
  Write benötigt weiterhin eine `TransactionId`; Current und historische
  Snapshots bleiben unveränderlich
  ([Invarianten](../../docs/Invarianten.md),
  [Transaktionen und Historie](../../docs/Transaktionen-und-Historie.md)).
- Der erste einfache Editierweg gilt für eigenen, unabhängigen Content.
  Für Fallback und fehlenden Content gibt es eine ausdrückliche Aktion zum
  Anlegen einer eigenen unabhängigen Fassung für die gewählte Zielgruppe.
  Fallback-Text wird nicht automatisch dupliziert. Derived Content bleibt im
  ersten Ergebnis lesbar und zeigt Quelle und Grund der gesperrten Bearbeitung.
  Die Oberfläche überschreibt nie unbemerkt eine andere Zielgruppe oder
  Provenienz ([Zielgruppen und Content](../../docs/Zielgruppen-und-Content.md)).
- Node-Titel bilden die Dokumentstruktur; `ContentMd` erhält keine
  Überschriften ([Wissenshierarchie](../../docs/Wissenshierarchie.md)).
- Es gibt kein automatisches Merge oder Rebase. Bei `SnapshotConflict` bleiben
  Entwurf und Diff nachvollziehbar
  ([Transaktionen und Historie](../../docs/Transaktionen-und-Historie.md)).
- Web und MCP nutzen dieselben Application-Use-Cases; Domain, Storage und
  MCP-Verträge werden für die neue Oberfläche nicht umgebaut
  ([Architektur](../../docs/Architektur.md)).

## Scope

### Muss

- Eine ruhige, durchgängig konsistente Shell mit kompakter globaler Navigation
  und Platz für spätere Links; Nutzung der vorhandenen Layoutgrundlagen für
  Breite, Header, `h1`, Fokus, Reflow und zentrale CSS-Tokens.
- Einen stabilen Seiten-, Slot- und URL-Vertrag für spätere Routen ohne
  Knowledge-spezifische Logik in der Shell oder doppelte Layout-Ownership.
- Getrennte URLs und Hauptflächen für Wissensarbeit, Entwurfsübersicht und
  Entwurfsabschluss. Der Baum erscheint nur im Wissensarbeitsplatz.
- Ein Wissensarbeitsplatz mit Baum, Node-Auswahl, Titel, Beschreibung und
  Inhaltseditor als Hauptansicht. Der Baum lädt große Hierarchien schrittweise
  und erhält Auswahl und sichtbaren Pfad.
- Node-Anlage, Umbenennung, Drag-and-drop-Verschiebung und Sortierung im Baum.
- Ein direkter Editierweg mit explizitem Speichern und automatisch begonnenem
  Entwurf beim ersten Write, ohne vorgeschaltete Transaction-Eingaben und ohne
  stilles Überschreiben eines inzwischen neueren Current Snapshots.
- Sichtbarer Entwurfszustand, Wiederaufnahme, Wechsel, Änderungsprüfung,
  Commit und Discard; klare Behandlung von Fehlern und Konflikten.
- Zielgruppe und Content-Verfügbarkeit sind sichtbar, ohne den Editor mit
  technischen Metadaten zu überladen. Fallback und fehlender Content bieten
  eine bewusste eigene Fassung; Derived Content erklärt seine Lesesperre.
- Browser-Abnahme mit realistischen Bäumen und den Zuständen Lesen,
  Bearbeiten, Dirty, Entwurf, leerer Baum, Drop-Erfolg/-Fehler, Commit und
  Konflikt. Sie umfasst alle neuen Routen bei 1280 × 720 und 1024 × 720
  sowie Zoom-/Reflow-Zustände bei 200 und 400 Prozent.

### Nicht

- Suche als eigener Workflow, Such-Overlay oder Suchfilter.
- Dashboard, vollständige Historien-/Release-Oberfläche und
  Zielgruppenverwaltung als Teil dieser ersten Basis.
- Node-Löschung als Web-Aktion; die fachliche Löschfähigkeit bleibt erhalten.
- PDF- oder Markdown-Export, Publikationsoberfläche und neue Medienverwaltung.
- Anmeldung, Berechtigungen oder Ableitung von Besitzrechten aus dem Fake User.
- Autosave, automatisches Merge/Rebase und automatische Aktualisierung von
  Derived Content.
- Änderung des Datenmodells oder der MCP-Verträge nur für das Weblayout.

Die Konzept- und Roadmap-Arbeit enthält keinen Code und keine Löschung.
Die spätere Umsetzung ersetzt die nicht mehr benötigten Webteile
anhand einer geprüften Dateiinventur in funktionsfähigen Slices. Nicht mehr
angebotene Routen und Navigationseinträge bleiben nicht als inkonsistente
Altoberfläche stehen.

## Verifikation des fertigen Produkts

- Vom Start aus kann ein Nutzer eine Node im Baum finden, ihren Text und Titel
  ändern, speichern und zur nächsten Node gehen, ohne vorher eine Transaction
  anzulegen oder Metadaten einzugeben.
- Zwei Nodes lassen sich durch sichtbares Drag-and-drop verschieben. Die
  Reihenfolge und der Parent stimmen danach mit dem Serverzustand überein.
- Ein großer, tiefer Baum bleibt ohne vollständiges Vorabladen navigierbar;
  die ausgewählte Node ist nach Wechsel und Verschiebung wiederzufinden.
  Eine leere Wissensbasis kann ohne vorgelagerte Transaction-Maske ihren
  ersten Node erhalten.
- Gespeicherte Änderungen an mehreren Nodes erscheinen in einem Entwurf.
  Die eigene Entwurfsseite erlaubt Prüfen, Commit und Discard; Reload und
  Konflikt führen nachvollziehbar dorthin.
- Die verbleibenden Seiten und Zustände stimmen in Innenkanten, Breite,
  Header-/`h1`-Vertrag und Zustandsdarstellung überein. Browserprüfungen
  belegen bei den vereinbarten Viewports und Zoomstufen Reflow, Fokus,
  Tastaturaktionen und die Drag-and-drop-Zielanzeige.
- Die globale Navigation kann später weitere Ziele aufnehmen, ohne den Baum
  oder das Node-Dokument zu verkleinern oder die Shell neu zu erfinden.
- Direktaufruf und Reload einer Node-URL stellen Node, Zielgruppe und
  ausgewählten Entwurf korrekt her; Dirty-Schutz gilt auch für globale
  Navigation und Browser-Zurück.
- Wenn Current zwischen Laden und erstem Write wechselt, bleibt die lokale
  Eingabe erhalten und die Änderung wird nicht auf den neueren Snapshot
  angewendet. Zwei schnelle erste Aktionen erzeugen nur einen Entwurf.

## Dokumentationsfolge

Dieses Konzept beschreibt den beabsichtigten Zustand. `docs/` beschreibt
weiterhin nur den implementierten Ist-Zustand. Bei der späteren Umsetzung
wird jede Verhaltens- oder Architekturänderung im selben Slice in das
zuständige Dokument unter `docs/` eingearbeitet. Nach Abschluss beschreibt
`docs/` die tatsächlich gebaute Weboberfläche; das Task-Konzept hält die
Produktentscheidung fest.

Die Formulierungen in `docs/README.md` und
`.agents/rules/DokuRichtlinien.mdc`, nach denen kein separates Konzept oder
Roadmap-Dokument existiert, sowie die Links in
`.agents/rules/WebUiHtmlCss.mdc` auf den entfernten Webfrontend-Task werden
vor der Codearbeit mit dem neuen Drei-Schritte-Workflow vereinbart. Dabei
darf geplantes UI-Verhalten nicht als implementierter Ist-Zustand erscheinen.

# Roadmap: Einheitliches Reiterlayout im Wissensbereich

Verbindliche Grundlage ist das [freigegebene Konzept](Konzept.md). Diese
Roadmap führt ausschließlich dessen Soll-Delta aus. Der bisherige Ist-Zustand
steht in [Web-UI-Gesamtbild](../../docs/WebUi.md) und
[Architektur](../../docs/Architektur.md). Für jeden Codepunkt gelten
`AGENTS.md`, die Web-UI-Guardrails sowie Build-, Test-, Linter- und
Dokumentationsregeln unter `.agents/rules/`. Die Umsetzung beginnt erst mit
einem gesonderten Auftrag für Workflow-Schritt 3.

Die Punkte laufen nacheinander in einem gemeinsamen Worktree. Jeder
Codepunkt beginnt mit `git status` und dem kleinsten passenden grünen
Baseline-Test, umfasst seinen Code, seine Tests und seine zwingenden
Dokumentationsfolgen, führt die in den Projektregeln verlangten Gates aus und
endet mit einem atomaren deutschen Conventional Commit nur seines Scopes.
Builds laufen über `pwsh -NoProfile -File scripts/build.ps1`; bei einem roten
Exitcode ist `temp/build.log` zu lesen. Ein selbst verursachter roter Stand
bleibt nicht als Übergabe zurück.

## 1. Gemeinsame Reiter- und Panelstruktur

- [x] **P1 – Wiederverwendbare Tab-Komponenten bereitstellen**
  - **Intention:** Die spätere Wissensansicht und weitere Reiterflächen teilen
    dieselbe Layout- und Zustandsdarstellung, ohne fachliche Logik zu teilen.
  - **Scope:** Unter
    `src/KnowHowToAI.Server/Web/Components/Shared/Tabs/` die im
    [Konzept, Technische Struktur](Konzept.md#technische-struktur-für-die-umsetzung)
    festgelegten `TabDefinition`, `TabLayout` und `TabPanelLayout` mit
    Namespace `KnowHowToAI.Server.Web.Components.Shared.Tabs` erstellen.
    `TabLayout` rendert Definitionen in Reihenfolge als native Buttons,
    `aria-pressed`, Kontext rechts/unterhalb bei Reflow und danach die
    Panels; es meldet den gewählten Schlüssel zurück. `TabPanelLayout`
    strukturiert primären Inhalt, Zusatzinhalt und Footer mit Status links
    und Aktionen rechts. Verborgene Panels bleiben montiert; fehlende
    Fragmente erzeugen keine leeren sichtbaren Bereiche. Farben/Abstände
    verwenden bestehende Tokens und scoped CSS.
  - **Nicht:** Keine Node-/Editorbegriffe in den Shared-Typen, keine
    Auswahlpersistenz, keine Speichermethode, kein `role="tablist"` und
    keine eigene Pfeiltastenbedienung; noch keine Änderung der Routen.
  - **Abnahme:** Schmale bUnit-Tests unter
    `tests/KnowHowToAI.Web.Tests/Components/Shared/Tabs/` belegen
    Definitionen/Reihenfolge, aktive Schaltfläche und Auswahl-Callback,
    Slot-Reihenfolge, fehlende Slots sowie Erhalt eines verborgen
    montierten Panelinhalts beim Umschalten. Passender FastTests-Lauf,
    Build und Linter-Incremental-Gate sind grün. Abschlussnachweis nennt
    Dateien, Befehle/Ergebnisse und Commit.
  - **Abschlussnachweis:** `TabDefinition`, `TabLayout` und `TabPanelLayout`
    liegen unter `src/KnowHowToAI.Server/Web/Components/Shared/Tabs/`;
    bUnit-Tests liegen unter
    `tests/KnowHowToAI.Web.Tests/Components/Shared/Tabs/`. Verifiziert mit
    `pwsh -NoProfile -File scripts/build.ps1` (Exitcode 0),
    `verify(targetPath)` (pass, 10.0, 0 Verstöße) und
    `pwsh -NoProfile -File scripts/test-fast.ps1 -Filter Category=Unit`
    (Frontend 5/5; FastTests 1.016/1.016). `verify(targetPath, scope: "solution")`
    meldet 9.1/1 wegen `AIContextFootprint` in der unveränderten
    `NodeDetailsWorkspace.razor.cs` außerhalb dieses Scopes. Commit:
    `feat: gemeinsame Reiter- und Panelkomponenten bereitstellen`.

## 2. Knotenansichten auf die Struktur setzen

- [x] **P2 – Vier Wissensreiter und einheitliche Inhaltsbereiche**
  - **Intention:** Auf beiden Wissensrouten stehen die Reiter vor jedem
    Knoteninhalt, und die häufig genutzte Leseansicht beginnt klar und ruhig.
  - **Voraussetzung:** P1 abgeschlossen.
  - **Scope:** `NodeDetailsWorkspace.razor[.cs|.css]` verwendet `TabLayout`
    mit den Beschriftungen und der Reihenfolge **Lesen, Bearbeiten, Titel,
    Technische Details**. Es behält die Auswahl bei `NodeView`, den Start
    mit Lesen, Node-/Zielgruppenwechsel, Lazy-Mount und die verborgen
    erhaltenen Editoren. `NodeDetails.razor[.cs|.css]` liefert Lesen und
    technische Angaben nach der Reiterleiste über `TabPanelLayout`;
    „Nur lesen“/„Arbeitskopie“ wandert dezent in den Kontextbereich rechts.
    Der Leseinhalt steht vor der separierten Zielgruppenangabe;
    Fallback-/Derived-Hinweise bleiben direkt beim Inhalt. Technische
    Details behalten die vorhandenen Daten und Herkunft, verlieren aber
    den redundanten aufklappbaren Reiternamen. `NodeMetadataEditor.razor[.cs|.css]`
    und der lesende Titelzustand verwenden `TabPanelLayout`; im
    schreibbaren Titel-Panel stehen Abbrechen und Speichern unten rechts,
    Speichern ganz rechts. Der `PageFrame` behält das einzige `h1`.
    `docs/WebUi.md` und `docs/Architektur.md` werden im selben Commit an
    dieses implementierte Delta angepasst.
  - **Nicht:** Kein Umbau von Baum, Shell, Entwurfsseiten, Editorwerkzeugen,
    Core-, MCP-, SQL- oder Routenverträgen. Keine Änderung der fachlich
    getrennten Mutation für Titel und Inhalt.
  - **Abnahme:** `NodeDetailsPaneTests`, `NodeDetailsTests` und
    `KnowledgePageReadOnlyTests` belegen vier Labels/Reihenfolge, Lesen als
    Initialansicht und erneuten Start mit Lesen nach Node- oder
    Zielgruppenwechsel, Reiter vor Hauptinhalt, Statusplatzierung, das
    einzige `h1`, Fallback-/Derived-Einordnung und erhaltene lokale Eingaben
    beim Reiterwechsel. Die passenden bestehenden Browserabläufe behalten
    ihre Funktion. Build, Linter-Incremental-Gate und passender FastTests-Lauf
    sind grün; Abschlussnachweis und atomarer Commit liegen vor.
  - **Abschlussnachweis:** `NodeDetailsWorkspace` verwendet die vier Reiter
    und behält Startansicht, Auswahlwechsel, Lazy-Mount sowie Dirty-Schutz bei.
    Für die Content-Schreibverantwortung übergibt `ContentEditor` Eingabe und
    Versionsstände über `IContentWriteWorkflow`/`SaveContentCommand` an
    `ContentWriteWorkflow`, das Replace-Request und koordinierte Mutation
    besitzt. Der `ContentEditor` behält Dirty-, Fokus-, Fehler-/Warnungs- und
    JS-Lebenszykluszustand. Ein reiner Editor-Wrapper erhöhte den Footprint
    auf 3004/2500 und wurde zurückgebaut; die getrennte Schreibverantwortung
    senkte den P2-Zwischenstand von 2961/2500 (Projektbaseline vor P2:
    2972/2500) auf 1811/2500. Die Schreibgrenze wurde mit den bestehenden
    Erfolg-/Fehler-, Dirty-, Source-Modus-, Fokus- und Browser-Smokes geprüft;
    `verify(targetPath)` und `verify(targetPath, scope: "solution")` meldeten
    beide `pass`, 10.0, 0 Verstöße. Verifiziert mit
    `pwsh -NoProfile -File scripts/build.ps1` (Exitcode 0), dem fokussierten
    Web-Testfilter (41/41), den Content-Editor- und Workspace-Tests (16/16),
    `pwsh -NoProfile -File scripts/test-fast.ps1 -Filter Category=Unit`
    (Frontend 5/5, FastTests 1.017/1.017),
    `KnowledgeTreeSmokeTests` + `KnowledgeDirectEditingSmokeTests` (5/5) sowie
    `ContentEditorSourceSmokeTests` + `KnowledgeDirectEditingSmokeTests`
    nach der Schreibgrenze (4/4). Die Browserlayout-Abnahme bleibt P4.
    Dokumentation in `docs/WebUi.md` und `docs/Architektur.md` synchronisiert.

## 3. Bearbeiten-Ansicht modernisieren

- [x] **P3 – Sichtbare Formatierung, kompakter Moduswechsel und Footer**
  - **Intention:** Die Bearbeiten-Ansicht bietet die vorhandenen Funktionen
    in einer klaren Reihenfolge: Werkzeuge, Text, Status/Aktion.
  - **Voraussetzung:** P2 abgeschlossen.
  - **Scope:** `ContentEditor.razor[.cs|.css]` nutzt `TabPanelLayout`.
    Über der Textfläche steht eine dauerhaft sichtbare, kompakte Leiste mit
    repo-eigenen SVG-Symbolen und Textnamen/Tooltip für Fett, Kursiv,
    Durchstreichen, Inline-Code und Link. Die Buttons wirken auf die
    aktuelle Auswahl und zeigen deren aktiven Zustand; im Quellmodus und
    bei fehlender Schreibbarkeit erscheinen keine wirkungslosen
    Formatierungsbuttons. Die beschriftete native Ansichtsauswahl in der
    Leiste bietet „Visuell“ und „Markdown-Quelle“ und nutzt die vorhandenen
    Wechselpfade. `ContentEditor.razor.js` und `content-editor.js` binden
    die vorhandenen Milkdown-Commands und die CommonMark-Linkmarkierung an die
    feste Leiste; die bisherige
    kontextuelle Crepe-Leiste wird deaktiviert. Direkt unter der
    Textfläche, im normalen Dokumentfluss, stehen der unabhängige
    Speicherstatus links und Speichern rechts; Warnungen/Fehler zur
    Paste-Reduktion und Validierung bleiben separat beim Editor.
    `docs/WebUi.md` und `docs/Architektur.md` werden im selben Commit
    mit dem implementierten Editoraufbau synchronisiert.
  - **Nicht:** Keine neue Formatierungsart, keine neue Editor- oder
    Icon-Bibliothek, kein Autosave, kein angehefteter Footer, keine
    Änderung an Markdown-Policy, Persistenz oder Transaktionsablauf.
  - **Abnahme:** `ContentEditorTests` und die bestehenden Vitest-Tests
    unter `src/KnowHowToAI.Server/Frontend/tests/` belegen Auswahl-
    Formatierung, Aktivzustand, Moduswechsel, Dirty-State,
    Statusvarianten und getrennte Warnungen. Der echte
    `ContentEditorSourceSmokeTests`-Ablauf erhält Markdown über beide
    Modi bis zum Speichern; `KnowledgeDirectEditingSmokeTests` belegt
    Reiterwechsel, Fokus und Save. Build, Linter-Incremental-Gate und
    passender FastTests-Lauf sind grün; Abschlussnachweis und atomarer
    Commit liegen vor.
  - **Abschlussnachweis:** `ContentEditor` verwendet `TabPanelLayout` mit
    primärem Bereich, getrenntem Feedbackbereich und Footer; im visuellen,
    schreibbaren Modus steht die dauerhafte lokale SVG-/Textleiste für die
    fünf vorhandenen Formatierungen. Fett, Kursiv, Durchstreichen und
    Inline-Code verwenden die vorhandenen Milkdown-Commands; Link setzt die
    vorhandene CommonMark-Linkmarkierung auf die Auswahl und bearbeitet die
    Adresse über den nativen Eingabedialog. Die Formataktionen arbeiten auf
    der Editor-Auswahl, halten diese beim Klick und spiegeln den aktiven
    Markierungszustand. Die Crepe-Kontexttoolbar ist deaktiviert. Die native
    Ansichtsauswahl schaltet zwischen „Visuell“ und „Markdown-Quelle“ über die
    bestehenden Werte- und Speicherpfade. Status und Speichern stehen im
    Footer, Paste-Reduktion, Fehler und Validierungswarnungen separat beim
    Editor. `markdownUpdated` übergibt den aktuellen Wert; Dirty-State wird
    gegen den zuletzt erfolgreich gespeicherten Markdown-Wert abgeglichen, so
    dass ein verspäteter Callback nach dem Speichern den sauberen Zustand
    nicht erneut setzt. `docs/WebUi.md` und `docs/Architektur.md` beschreiben
    den implementierten Aufbau.
    Verifiziert mit `pwsh -NoProfile -File scripts/build.ps1` (Exitcode 0),
    `verify(targetPath)` und `verify(targetPath, scope: "solution")` (beide
    pass, 10.0, 0 Verstöße), `pwsh -NoProfile -File scripts/test-fast.ps1
    -Filter Category=Unit` (Vitest 7/7; .NET-FastTests 1.019/1.019) und
    `dotnet test tests/KnowHowToAI.BrowserTests/KnowHowToAI.BrowserTests.csproj
    --filter "FullyQualifiedName~ContentEditorSourceSmokeTests|FullyQualifiedName~KnowledgeDirectEditingSmokeTests"
    --no-build --no-restore` (4/4). `git diff --check` war sauber.

## 4. Routeübergreifende Abnahme

- [x] **P4 – Browserlayout und Dokumentation abschließen**
  - **Intention:** Die behauptete Reiter- und Editorordnung ist in den
    echten Wissensrouten und relevanten Zuständen belegt.
  - **Voraussetzung:** P3 abgeschlossen.
  - **Scope:** `PageFrameSmokeTests` prüft bei 1280 × 800 CSS-Pixeln (oder
    begründet bei 1280 × 720) und sekundär bei 1024 × 720 Reiter vor Inhalt,
    Kontextposition, Footer mit Status links
    und Speichern rechts, Innenkanten, Computed Styles, Umbruch,
    horizontalen Overflow und erreichbare Aktionen. Die bestehenden
    Knowledge-/Editor-Browser-Smokes werden an die neuen Labels und die
    native Ansichtsauswahl angepasst. `UiAuditScreenshotTests` erfasst
    zusätzlich den Working-Kontext und fehlenden eigenen Content, damit
    alle im Konzept genannten Wissenszustände vertreten sind. Gezielte
    Screenshots dokumentieren nur relevante geänderte Zustände in der jeweils
    nötigen Desktopgröße und werden gemeinsam visuell auf unerklärten Drift
    geprüft. Es gibt keine schmale Viewport-Matrix und keine pauschalen
    FullPage-Screenshots. `docs/WebUi.md` und
    `docs/Architektur.md` werden gegen den tatsächlichen Endzustand
    abgeglichen. Vollständige FastTests, relevante Browser-Smokes,
    Solution-Build und Solution-Linter-Gate laufen nach Projektregeln.
  - **Nicht:** Keine neuen Features, keine blinde Aktualisierung von
    Screenshot-Baselines, kein Umbau unbetroffener Entwurfsrouten.
  - **Abnahme:** Beide Wissensrouten samt Current/Draft, Lesen/Titel/
    Bearbeiten/Technische Details und normalem/Fallback/fehlendem/Derived
    Content sind im kombinierten Browser-/Sichtnachweis vertreten.
    `docs/` beschreibt den implementierten Ist-Zustand. Abschlussnachweis
    enthält Testresultate, Screenshot-Verzeichnis, begründete Abweichungen
    und einen atomaren Commit nur für diesen Punkt.
  - **Abschlussnachweis:** `PageFrameSmokeTests` prüft das ausgewählte
    Wissensdokument bei 1280 × 800 und 1024 × 720: Reihenfolge von Reitern,
    Lesebereich und Inhalt; Kontextposition; Seiten-Innenkanten; Computed
    Styles der Reiter, Editorfläche, Werkzeugleiste und Footer; Umbruch;
    horizontalen Overflow; sowie Speicherstatus links und erreichbares
    Speichern rechts. Reiterschaltflächen und Ansichtsauswahl werden über
    sichtbare Namen bzw. den nativen `select` bedient. Die Browser-Smokes für
    Baum/Deep-Link, direkten Titel- und Content-Edit, Markdown-Moduswechsel,
    Paste-Reduktion und Root-Erstellung verwenden die aktuellen Labels und
    Statusausweise. `UiAuditScreenshotTests` deckt Current-Lesen, Titel,
    visuellen Editor und Footer, Technische Details, Fallback-Lesen/-Editor,
    Derived-Lesen/-Editor sowie Working-Lesen/-Editor ohne eigenen Content ab.
    18 gezielte Screenshots wurden bei 1280 × 800 gemeinsam visuell geprüft;
    Verzeichnis: `temp/ui-audit/2026-09-23_18-25-26`. Kein abgeschnittener
    Inhalt, unerklärter Layoutdrift oder horizontaler Überlauf festgestellt.
    Verifiziert mit dem relevanten Browser-Smoke-Filter (14/14),
    `PageFrameSmokeTests` nach finaler Assertionsteilung (5/5), aktiviertem
    UiAudit (1/1), vollständigen FastTests (Vitest 7/7, .NET 1.019/1.019),
    `pwsh -NoProfile -File scripts/build.ps1` (Exitcode 0),
    `verify(targetPath)` und `verify(targetPath, scope: "solution")`
    (beide `pass`, 10.0, 0 Verstöße) sowie `git diff --check`.
    `docs/WebUi.md` und `docs/Architektur.md` sind mit den Nachweisen
    abgeglichen. Der Audit-Korrekturlauf hat die Screenshots 07 und 15 im
    Verzeichnis `temp/ui-audit/2026-09-23_18-50-08` bei 1280 × 800 neu
    aufgenommen und visuell geprüft; beide zeigen den fokussierten Editor ohne
    Crepe-LinkTooltip-Elemente neben der festen Leiste. Commit wird mit
    diesem P4-Slice erstellt.

## Audit

- [x] Ein begrenzter, lesender Audit prüft nach P1–P4 das Ergebnis gegen
  [Konzept](Konzept.md), Diff, Invarianten,
  Layout-Guardrails, Dokumentation und Nachweise. Befunde werden auf
  konkrete Konzeptverstöße begrenzt. Höchstens eine gezielte
  Korrekturrunde; danach Entscheidung über Abnahme oder neue Planung.

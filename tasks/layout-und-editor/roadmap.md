# Roadmap: Viewport-Locked Layout für Wissensbaum und Editor

Verbindliche Grundlage ist das [freigegebene Konzept](Konzept.md). Diese
Roadmap führt ausschließlich dessen Soll-Delta aus. Der bisherige Ist-Zustand
steht in [Web-UI-Gesamtbild](../../docs/WebUi.md). Für jeden Codepunkt gelten
`AGENTS.md`, die Web-UI-Guardrails ([.agents/rules/WebUiHtmlCss.mdc](../../.agents/rules/WebUiHtmlCss.mdc))
sowie Build-, Test-, Linter- und Dokumentationsregeln unter `.agents/rules/`.
Die Umsetzung beginnt erst mit einem gesonderten Auftrag für Workflow-Schritt 3.

Die Punkte laufen nacheinander in einem gemeinsamen Worktree. Jeder Codepunkt
beginnt mit `git status` und dem kleinsten passenden grünen Baseline-Test,
umfasst seinen Code, seine Tests und seine zwingenden Dokumentationsfolgen,
führt die in den Projektregeln verlangten Gates aus und endet mit einem
atomaren deutschen Conventional Commit nur seines Scopes. Builds laufen über
`pwsh -NoProfile -File scripts/build.ps1`; bei einem roten Exitcode ist
`temp/build.log` zu lesen. Ein selbst verursachter roter Stand bleibt nicht als
Übergabe zurück.

## 1. Arbeitspakete und Leaf-Tasks

- [x] **P1 – Zentrales Eingabefeld- und Fokus-Design etablieren**
  - **Intention:** Eingabefelder, Textareas und zusammengesetzte Editor-Flächen folgen einem generalisierten Fokus-Muster mit einheitlichem Radius und zentralem Fokusring ohne lokale Sonderlocken.
  - **Scope:** In `src/KnowHowToAI.Server/wwwroot/css/base/accessibility.css` (und ggf. `tokens/`) das einheitliche Fokus-Muster für `input[type='text']:focus-visible`, `textarea:focus-visible`, `select:focus-visible` und `.content-editor__surface:focus-within` bereitstellen. Verwendung von `var(--ktai-radius-md)`, `var(--ktai-focus-ring-width)` und `var(--ktai-color-focus)`.
  - **Nicht:** Keine Razor-Markup-Eingriffe an bestehenden Formularen; keine lokalen Hilfsklassen in Komponenten.
  - **Abnahme:** Fokus-Styling auf allen genannten Feldern aktiv; `pwsh scripts/build.ps1` Exitcode 0; `verify` pass; FastTests grün. Atomarer Commit: `style: zentrales Fokus- und Eingabefelddesign etablieren`.

- [x] **P2 – Viewport-Locked Shell und Seitenrahmen-Kaskade umsetzen**
  - **Intention:** Das Browserfenster scrollt als Ganzes niemals; die App-Shell fixiert sich exakt auf die Viewport-Höhe (`100vh`).
  - **Scope:**
    - `src/KnowHowToAI.Server/Web/Components/Layout/Shell/MainLayout.razor.css`: `.shell-root` auf `height: 100vh; max-height: 100vh; overflow: hidden;`. `.shell-workspace` und `.shell-main` auf `height: 100%; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`.
    - `src/KnowHowToAI.Server/Web/Components/Shared/PageFrame.razor.css`: `.page-frame--shared` auf `height: 100%; min-height: 0; display: flex; flex-direction: column;`. Header ist `flex-shrink: 0;`, Content-Slot ist `flex: 1; min-height: 0; overflow: hidden;`.
  - **Nicht:** Kein Umbau der Header-Navigation, kein Umbau von Brand, Reconnect-Modal oder Toasts.
  - **Abnahme:** HTML/Body erzeugen keinen Fensterscrollbalken; `PageFrameSmokeTests` prüfen gemeinsame Landmarken und Innenkanten bei 1280×800 und 1024×720; FastTests grün. Atomarer Commit: `refactor: App-Shell und Seitenrahmen auf Viewport-Locked Layout umstellen`.

- [x] **P3 – Wissensseite und autarken Baum-Scroll ausrichten**
  - **Intention:** Wissensbaum und Content-Pane nutzen die volle Resthöhe; der Wissensbaum scrollt bei vielen Knoten autark in sich selbst ohne Magic Numbers.
  - **Scope:**
    - `src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor.css`: `.knowledge-layout` auf `flex: 1; min-height: 0; height: 100%; display: grid; grid-template-columns: minmax(16rem, 22rem) minmax(0, 1fr); gap: var(--ktai-space-4);`.
    - `.knowledge-sidebar`: `height: 100%; min-height: 0; overflow-y: auto; overflow-x: hidden;` (Entfall von `calc(100vh - 200px)`).
    - `.knowledge-content-pane`: `height: 100%; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`.
    - `src/KnowHowToAI.Server/Web/Components/Shared/Tabs/TabLayout.razor.css`: `.tab-layout` auf `height: 100%; min-height: 0;`. Header `flex-shrink: 0;`, Panels `flex: 1; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`.
    - `src/KnowHowToAI.Server/Web/Components/Shared/Tabs/TabPanelLayout.razor.css`: `.tab-panel-layout` auf `height: 100%; min-height: 0;`. `PrimaryContent` auf `flex: 1; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`. Footer `flex-shrink: 0;`. Lese- und technische Detailansichten erhalten `overflow-y: auto;` im PrimaryContent für autarken Fließtext-Scroll.
  - **Nicht:** Keine Änderung an der Baum-Hierarchie oder den C#-Logiken von `KnowledgeTree` und `NodeDetailsWorkspace`.
  - **Abnahme:** Großer Baum scrollt sauber vertikal; Leseansicht scrollt bei Überlänge intern; Reiterleiste bleibt oben fixiert; FastTests grün. Atomarer Commit: `refactor: Wissensseite und Baum auf autarke vertikale Resthöhe umstellen`.

- [x] **P4 – Editor-Workspace, ProseMirror-Korrekturen und dauerhafte Speichern-Leiste**
  - **Intention:** Der Inhaltseditor füllt die Resthöhe aus, man tippt in Zeile 1 ohne Zeilenversatz, die innere 2-Zeilen-Outline entfällt, die Ansicht-Auswahl bleibt im Quellmodus rechts, und der Speichern-Footer bleibt dauerhaft am unteren Panelrand sichtbar.
  - **Scope:**
    - `src/KnowHowToAI.Server/Web/Features/Content/ContentEditor.razor.css`:
      - `.content-editor__toolbar-row`: Flex-Zeile `flex-shrink: 0;`.
      - `.content-editor__mode-control`: `margin-left: auto;` für stabile Rechtsbündigkeit im visuellen und Quellmodus.
      - `.content-editor__surface`: `flex: 1; min-height: 0; overflow-y: auto; border: 1px solid var(--ktai-color-border); border-radius: var(--ktai-radius-md);`.
      - `.content-editor__surface .ProseMirror`: `min-height: 100%; padding: var(--ktai-space-3); outline: none; box-sizing: border-box;`.
      - `.content-editor__surface .ProseMirror > *:first-child`: `margin-top: 0;` (beseitigt optischen 1-Zeilen-Versatz).
      - `.content-editor__source`: `flex: 1; min-height: 0; height: 100%; resize: none; border: 1px solid var(--ktai-color-border); border-radius: var(--ktai-radius-md);`.
      - `.content-editor--workspace`: `height: 100%; min-height: 0; display: flex; flex-direction: column;`.
    - Der Footer (Speicherstatus links, Speichern rechts) in `TabPanelLayout` bleibt dauerhaft unten verankert (`flex-shrink: 0;`).
  - **Nicht:** Keine Änderungen an Speicher- oder Serialisierungslogiken in C# oder JS.
  - **Abnahme:** Texteingabe startet in Zeile 1; Klick in Textfläche fokussiert sauber; blauer Fokusring umschließt das gesamte Feld; Quellmodus-Combobox steht rechts; Speichern-Button ist bei jeder Textlänge ohne Fensterscrollen erreichbar; FastTests grün. Atomarer Commit: `refactor: ContentEditor auf Fullscreen-Resthöhe und zentriertes Fokus-Design ausrichten`.

- [x] **P5 – Browser-Test-Verifikation und Dokumentation**
  - **Intention:** Automatisierter Nachweis der Viewport-Sperre und der erreichbaren Aktionen bei 1280×800 und 1024×720 sowie Aktualisierung des Ist-Zustands in `docs/WebUi.md`.
  - **Scope:**
    - `tests/KnowHowToAI.BrowserTests/ReadOnly/PageFrameSmokeTests.cs`: Prüfung ergänzen, dass `document.documentElement.scrollHeight <= window.innerHeight` erfüllt ist und `content-editor-save` ohne `ScrollIntoViewIfNeeded` direkt im Viewport liegt.
    - `tests/KnowHowToAI.BrowserTests/Editor/ContentEditorSourceSmokeTests.cs`: Prüfung der Position von `content-editor-view-mode` im Quellmodus.
    - `docs/WebUi.md`: Dokumentation des Viewport-Locked Layouts, des autarken Scrollverhaltens von Baum und Editor sowie der festen Speichern-Leiste aktualisieren.
  - **Nicht:** Keine Tests für Viewports unter 1024px.
  - **Abnahme:** `PageFrameSmokeTests` und `ContentEditorSourceSmokeTests` bei 1280×800 und 1024×720 grün; `scripts/build.ps1` grün; `docs/WebUi.md` synchronisiert. Atomarer Commit: `test: Viewport-Locked Layout und Editor-Positionierung automatisiert absichern`.

## 2. Milestone-Abnahme & Audit

- [ ] **P6 – Milestone-Audit**
  - **Intention:** Unabhängige Prüfung der Umsetzung gegen Konzept, Guardrails und Abschlussgate.
  - **Scope:** `tasks/layout-und-editor/audit.md` anlegen mit Belegen aus `verify(targetPath, scope: "solution")`, Build- und Test-Ergebnissen, manueller Sichtprüfung bei 1280×800 und 1024×720 sowie Vollständigkeitsnachweis aller Checklisten.
  - **Abnahme:** `verdict=pass`, 0 Verstöße; Audit-Report committed; Roadmap-Checkboxen vollständig geschlossen.

# Milestone-Audit – Viewport-Locked Layout für Wissensbaum und Editor

## Ergebnis

Audit ohne Befund bestanden (`verdict=pass`, Score 10.0, 0 Violations). Sämtliche
Muss-Ziele aus [Konzept.md](Konzept.md) und [roadmap.md](roadmap.md) (P1 bis P5)
wurden vollständig und regelkonform umgesetzt, automatisiert getestet und im
Ist-Stand dokumentiert.

## Soll-Ist-Abgleich gegen das Konzept

- **Viewport-Locked Shell & Seitenrahmen (P2, Konzept §3.1)**:
  - Browserfenster scrollt niemals (`height: 100vh; max-height: 100vh; overflow: hidden;` auf `.shell-root`).
  - `.shell-workspace`, `.shell-main` und `.page-frame--shared` bilden eine unterbrechungsfreie Flex-Spaltenkaskade mit `height: 100%; min-height: 0;`.
  - Seitenkopf (`.page-frame__header`) bleibt dauerhaft oben fixiert (`flex-shrink: 0;`), der Inhaltsbereich (`.page-frame__content`) füllt die Resthöhe aus (`flex: 1; min-height: 0; overflow: hidden;`).
- **Autarke Scrollbereiche für Wissensbaum und Pane (P3, Konzept §3.1)**:
  - `.knowledge-layout` füllt die volle vertikale Resthöhe per CSS-Grid aus (`height: 100%; min-height: 0;`).
  - Wissens-Sidebar (`.knowledge-sidebar`) scrollt bei vielen Knoten autark in sich selbst (`height: 100%; min-height: 0; overflow-y: auto; overflow-x: hidden;`); Wegfall der `calc(100vh - 200px)`-Näherung.
  - Rechte Content-Pane (`.knowledge-content-pane`), `TabLayout` und `TabPanelLayout` füllen jeweils `height: 100%; min-height: 0;` aus. Lese- und technische Detailansichten scrollen autark im `PrimaryContent` (`overflow-y: auto;`), während Tab-Reiterleiste und Panel-Footer fest verankert bleiben.
- **Editor-Workspace, ProseMirror-Korrekturen und persistenter Footer (P4, Konzept §3.2–§3.4)**:
  - `.content-editor--workspace` spannt die volle Resthöhe auf (`height: 100%; min-height: 0; display: flex; flex-direction: column;`).
  - Obere Werkzeugleiste (`.content-editor__toolbar-row`) bleibt oben fixiert (`flex-shrink: 0;`).
  - Textflächen (`.content-editor__surface` und `.content-editor__source`) dehnen sich flexibel aus (`flex: 1; min-height: 0; overflow-y: auto;`). Quellmodus-Textarea nutzt `resize: none; height: 100%;`.
  - Unterer Aktionsfooter (Speicherstatus links, Speichern-Button rechts) bleibt im `TabPanelLayout` dauerhaft am unteren Rand sichtbar (`flex-shrink: 0;`), ohne Fenster-Scroll.
  - ProseMirror (`.content-editor__surface ::deep .ProseMirror`):
    - Füllt die gesamte Containerfläche aus (`min-height: 100%; box-sizing: border-box; padding: var(--ktai-space-3);`). Klicks fokussieren an jeder Stelle.
    - Keine innere 2-Zeilen-Outline (`outline: none;` im Scoped CSS).
    - Kein 1-Zeilen-Versatz bei Texteingabe (`> *:first-child { margin-top: 0; }`).
  - Ansichtsauswahl (`.content-editor__mode-control`): Im Quell- wie im visuellen Modus stabil an der rechten Außenkante verankert (`margin-left: auto;`).
- **Zentralisiertes Fokus-Design (P1, Konzept §3.5)**:
  - Einheitliches Fokus-Muster in `src/KnowHowToAI.Server/wwwroot/css/base/accessibility.css` für `input[type='text']:focus-visible`, `textarea:focus-visible`, `select:focus-visible` und `.content-editor__surface:focus-within`.
  - Nutzung der zentralen Tokens `var(--ktai-radius-md)`, `var(--ktai-focus-ring-width)` und `var(--ktai-color-focus)`.
  - Keine lokalen Sonderlocken oder Razor-Markup-Eingriffe; die `DesignTokensTests`-Invariante (kein `outline: none` in globalen CSS-Dateien) bleibt gewahrt.
- **Einhaltung der Nicht-Ziele (Konzept §3.6)**:
  - Keine Änderungen an C#-Datenmodellen, WorkspaceState, MCP-Tools oder Persistenzlogiken.
  - Kein Austausch von Milkdown/Crepe.
  - Keine Tastatur-Navigationsmuster für den Treeview (Desktop-/Mausfokus gewahrt).

## Verifikation & Qualitäts-Gates

- **Build**: `pwsh -NoProfile -File scripts/build.ps1` -> Exitcode 0 (`temp/build.log` sauber).
- **Linter / Static Analysis**: `AiNetLinter verify(targetPath: "KnowHowToAI.slnx", scope: "solution")`:
  - `verdict: pass`
  - `completeness: complete`
  - `score: 10.0`
  - `violationCount: 0`
- **FastTests**:
  - `KnowHowToAI.IntegrationTests`: 286/286 bestanden (0 Fehler, 0 übersprungen).
  - `KnowHowToAI.Web.Tests`: 310/310 bestanden (0 Fehler, 0 übersprungen), inklusive `DesignTokensTests`.
- **Playwright Browser Smoke Tests**:
  - `PageFrameSmokeTests`: 4/4 bestanden bei 1280×800 und 1024×720. Verifiziert `document.documentElement.scrollHeight <= window.innerHeight` (kein Fensterscrollen) und direkte Sichtbarkeit/Klickbarkeit von `#content-editor-save` ohne `ScrollIntoViewIfNeeded`.
  - `ContentEditorSourceSmokeTests`: 2/2 bestanden. Verifiziert Quellmodus-Roundtrip und rechtsbündige Verankerung von `#content-editor-view-mode`.
- **Git-Sauberkeit**:
  - `git diff --check`: Keine Whitespace- oder Formatierungsfehler (Exitcode 0).
  - Arbeitsverzeichnis vor dem Audit-Commit sauber.

## Geprüfter Umfang & Historie

- P1: Commit `8cb9815` – `style: zentrales Fokus- und Eingabefelddesign etablieren`
- P2: Commit `785a8eb` – `refactor: App-Shell und Seitenrahmen auf Viewport-Locked Layout umstellen`
- P3: Commit `0d4f65e` – `refactor: Wissensseite und Baum auf autarke vertikale Resthöhe umstellen`
- P4: Commit `5a2032a` – `refactor: ContentEditor auf Fullscreen-Resthöhe und zentriertes Fokus-Design ausrichten`
- P5: Commit `c5aeb13` – `test: Viewport-Locked Layout und Editor-Positionierung automatisiert absichern`
- P6: Dieser Audit-Bericht schließt den Milestone `tasks/layout-und-editor` ab.

---
status: ready
---

# Viewport-Locked Layout für Wissensbaum und Editor

## Intention

In der Wissensansicht soll die Bedienung dem Standard moderner Desktop-Webanwendungen
(z. B. VS Code, Notion, Azure DevOps) entsprechen: Das Browserfenster scrollt niemals als
Ganzes. Der Wissensbaum links und der Arbeitsbereich rechts nutzen exakt die vertikale
Höhe des Browserfensters und scrollen bei Überlänge jeweils autark in sich selbst. Im
Editor-Reiter („Bearbeiten“) füllt die Textfläche den verfügbaren vertikalen Raum
vollständig aus, während Werkzeugleiste und die Speichern-Aktion am unteren Rand dauerhaft
ohne Fenster-Scrollen im Blick bleiben. Der visuelle Editor und der Markdown-Quellmodus
folgen dabei einem konsistenten, generalisierten Eingabefeld- und Fokusmuster ohne
Sonderlocken.

## Belegter Ausgangspunkt

- **Fenster-Scroll statt Container-Scroll**:
  - `MainLayout.razor.css` definiert `.shell-root` mit `min-height: 100vh` ohne `height: 100vh; overflow: hidden`.
  - `KnowledgePage.razor.css` nutzt für die Sidebar eine starre Näherung (`max-height: calc(100vh - 200px)`), während die Content-Pane unbegrenzt nach unten wächst (`min-height: 300px`).
  - `ContentEditor.razor.css` definiert `.content-editor--workspace` mit `min-height: min(60vh, 38rem)`.
  - Bei langem Text wächst die rechte Pane über den Viewport hinaus, erzeugt einen Scrollbalken auf dem Browserfenster, schiebt Header und Reiter aus dem Bild und lässt den Wissensbaum asynchron oben stehen.
- **Visueller Editor – 2-Zeilen-Rahmen und Zeilenversatz**:
  - Crepe erzeugt in `.content-editor__surface` ein ungestyltes ProseMirror-DOM (`.milkdown > .ProseMirror[contenteditable="true"]`).
  - Beim Fokus greift der globale Fokusring aus `accessibility.css` auf `.ProseMirror`. Da `.ProseMirror` keine eigene Höhenregel (`min-height: 100%`) besitzt, ist es nur so hoch wie der Text (anfangs ca. 2 Zeilen hoch). Der Browser zeichnet die blaue Outline eckig mitten in das große weiße Feld um diese 2 Zeilen.
  - Das ungestylte erste `<p>` in ProseMirror erbt den Standard-Browserabstand (`margin-block: 1em`), wodurch die Texteingabe optisch um eine Zeilenhöhe nach unten versetzt in der „zweiten Zeile“ beginnt.
- **Markdown-Quelle – Versatz der Ansichtsauswahl**:
  - In `ContentEditor.razor` liegt die Leiste in einer Flex-Row mit `justify-content: space-between`.
  - Im Markdown-Modus wird die Formatierungsleiste (`.content-editor__formatting-toolbar`) ausgeblendet. Da nur noch die Ansichtsauswahl (`.content-editor__mode-control`) im Flex-Container verbleibt, springt diese von der rechten Kante ganz nach links.
- **Fokus-Rahmen und Rundungen**:
  - Eingabefelder, Textareas und der Editor-Container besitzen uneinheitliche Fokus-Visualisierungen. Standard-Inputs nutzen native Outlines, während der visuelle Editor eine innere Outline und der Markdown-Editor eine äußere Textarea-Outline nutzt.

## Scope

### Muss

- **Viewport-Locked Shell & Wissens-Layout**:
  - Das gesamte Browserfenster scrollt niemals (`height: 100vh; max-height: 100vh; overflow: hidden;` auf Shell-Ebene). Header und Seitenkopf (`PageFrame`) bleiben dauerhaft sichtbar.
  - `.knowledge-layout` füllt exakt die verbleibende vertikale Höhe zwischen Seitenkopf und Fensterrand aus (`height: 100%; min-height: 0;`).
  - Die Wissens-Sidebar (Baum) links füllt die volle vertikale Höhe aus und besitzt einen eigenen, autarken vertikalen Scrollbereich (`height: 100%; min-height: 0; overflow-y: auto; overflow-x: hidden;`).
  - Die Content-Pane rechts füllt die volle vertikale Höhe aus (`height: 100%; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`).
  - Auch in den Reitern „Lesen“ und „Technische Details“ scrollt der Inhalt bei Überlänge autark innerhalb seines Panels (`overflow-y: auto`).
- **Editor-Höhe und dauerhaft sichtbare Speichern-Aktion**:
  - Im Reiter „Bearbeiten“ nimmt das Editor-Panel die verfügbare Resthöhe ein.
  - Die Werkzeugleiste oben bleibt am oberen Rand des Panels fest verankert (`flex-shrink: 0`).
  - Die Textfläche (sowohl `.content-editor__surface` als auch `.content-editor__source`) dehnt sich flexibel aus (`flex: 1; min-height: 0; overflow-y: auto;`). Bei langem Text scrollt nur die Textfläche intern.
  - Im Quellmodus gilt für die Textarea `resize: none; height: 100%; min-height: 0;`, damit das Flex-Layout stabil bleibt und kein manuelles Aufziehen das Layout sprengt.
  - Die untere Aktionszeile (Speicherstatus links, Button „Speichern“ rechts) bleibt am unteren Rand des Panels dauerhaft im Viewport sichtbar (`flex-shrink: 0`), ohne dass der Benutzer nach unten scrollen muss.
- **Korrektur der Formatierungs- und Ansichtszeile**:
  - Im Markdown-Quellmodus bleibt die Ansicht-Auswahl („Ansicht: Markdown-Quelle / Visuell“) an der rechten Außenkante verankert (`margin-left: auto;`), genau wie im visuellen Modus.
- **Visueller Editor (Crepe / ProseMirror) – Fläche & Eingabe**:
  - `.ProseMirror` füllt den Editor-Container vollständig aus (`min-height: 100%; box-sizing: border-box; padding: var(--ktai-space-3);`). Klicks an beliebiger Stelle der Fläche fokussieren das Dokument.
  - Die störende innere 2-Zeilen-Outline auf `.ProseMirror` entfällt (`outline: none;`).
  - Der obere Paragraf-Margin des ersten Elements in ProseMirror wird neutralisiert (`.ProseMirror > *:first-child { margin-top: 0; }`), sodass man direkt in der ersten Zeile tippt.
- **Zentralisiertes Fokus-Muster für Eingabeflächen**:
  - Vereinheitlichung des Fokusrings für alle textuellen Eingabefelder (`input[type='text']`, `textarea`, `.content-editor__source`, `.content-editor__surface:focus-within`) rein über CSS-Selektoren in `base/accessibility.css` (bzw. Basistokens).
  - Der Fokusrahmen nutzt zentral die Tokens `--ktai-radius-md`, `--ktai-focus-ring-width` und `--ktai-color-focus` ohne lokale Sonderregeln oder Razor-Markup-Eingriffe.

### Nicht

- Kein kompletter Umbau der Shell-Architektur oder der Navigation (`MainLayout`, `PageFrame`, `KnowledgePage`, `WorkspaceState` bleiben in ihrer Struktur und ihren C#-Verträgen erhalten).
- Keine neue Editor-Bibliothek und kein Ersatz von Milkdown/Crepe.
- Keine Änderung an der Datenhaltung, Persistenz, Transaktionen oder den MCP-Werkzeugen.
- Keine Tastatur-Navigationsmuster für den Treeview (bleibt Maus-/Mausrad-fokussiert gemäß Web-UI-Guardrails).

## Technische Architektur & Umsetzungsvorschlag

Die Anpassungen beschränken sich auf gezieltes, architektonisch sauberes CSS-Refactoring und minimale Razor-Klassenanpassungen:

1. **Tokens & Base (`wwwroot/css/`)**:
   - `base/accessibility.css`: Bereitstellung des generalisierten Fokus-Zustands für Eingabefelder und zusammengesetzte Editoren (`input[type='text']:focus-visible, textarea:focus-visible, .content-editor__surface:focus-within`) mit einheitlichem Fokusring und `var(--ktai-radius-md)`.
2. **Shell & PageFrame (`Web/Components/Layout/` und `Web/Components/Shared/`)**:
   - `MainLayout.razor.css`: `.shell-root` wird auf `height: 100vh; max-height: 100vh; overflow: hidden;` fixiert. `.shell-workspace` und `.shell-main` erhalten `height: 100%; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`.
   - `PageFrame.razor.css`: `.page-frame--shared` erhält `height: 100%; min-height: 0; display: flex; flex-direction: column;`. Header ist `flex-shrink: 0;`, Content-Slot ist `flex: 1; min-height: 0; overflow: hidden;`.
3. **KnowledgePage (`Web/Features/Knowledge/`)**:
   - `KnowledgePage.razor.css`: `.knowledge-layout` erhält `flex: 1; min-height: 0; height: 100%; display: grid; grid-template-columns: minmax(16rem, 22rem) minmax(0, 1fr); gap: var(--ktai-space-4);`.
   - `.knowledge-sidebar`: `height: 100%; min-height: 0; overflow-y: auto; overflow-x: hidden;` (Entfall von `calc(100vh - 200px)`).
   - `.knowledge-content-pane`: `height: 100%; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`.
4. **Tabs & Panel-Layout (`Web/Components/Shared/Tabs/`)**:
   - `TabLayout.razor.css`: `.tab-layout` wird Flex-Spalte mit voller Höhe (`height: 100%; min-height: 0;`). Header `flex-shrink: 0;`, Panels-Container `flex: 1; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`.
   - `TabPanelLayout.razor.css`: `.tab-panel-layout` füllt `100%` Höhe aus (`height: 100%; min-height: 0;`). `PrimaryContent` erhält `flex: 1; min-height: 0; display: flex; flex-direction: column; overflow: hidden;`. Footer mit Actions erhält `flex-shrink: 0;`.
   - Leseansicht und Technische Details erhalten im PrimaryContent `overflow-y: auto;`.
5. **ContentEditor (`Web/Features/Content/`)**:
   - `ContentEditor.razor.css`:
     - `.content-editor__mode-control { margin-left: auto; }` garantiert Rechtsbündigkeit im visuellen wie im Quellmodus.
     - `.content-editor__surface`: `flex: 1; min-height: 0; overflow-y: auto; border: 1px solid var(--ktai-color-border); border-radius: var(--ktai-radius-md);`.
     - `.content-editor__surface:focus-within`: erhält den generalisierten Fokusring.
     - `.content-editor__source`: `flex: 1; min-height: 0; height: 100%; resize: none; border: 1px solid var(--ktai-color-border); border-radius: var(--ktai-radius-md);`.
     - ProseMirror-Integration: `.content-editor__surface .ProseMirror { min-height: 100%; padding: var(--ktai-space-3); outline: none; }` und `.content-editor__surface .ProseMirror > *:first-child { margin-top: 0; }`.

## Verifikation

- **Automatisierte Playwright-Tests**:
  - Ausführung von `PageFrameSmokeTests.cs` bei 1280×800 und 1024×720:
    - Verifikation, dass `document.documentElement.scrollHeight <= window.innerHeight` gilt (kein Fenster-Scroll).
    - Verifikation, dass Speicherstatus und Speichern-Aktion ohne `ScrollIntoViewIfNeeded` direkt im Viewport sichtbar und klickbar sind.
  - Ausführung von `ContentEditorSourceSmokeTests.cs`:
    - Umschalten zwischen visuellem Modus und Quellmodus round-trippt wie bisher; Position der Combobox rechts geprüft.
- **Visuelle Abnahme (Desktop 1280×800 & 1024×720)**:
  - Treeview mit vielen Knoten wächst bis zum unteren Rand und scrollt sauber per Mausrad.
  - Editor-Klick fokussiert in Zeile 1; blauer Fokusring umschließt das gesamte Feld mit korrekter Rundung.
  - Beim Einfügen von langem Text wächst der Text innerhalb des Editors mit interner Scrollbar; „Speichern“ bleibt unten fixiert sichtbar.

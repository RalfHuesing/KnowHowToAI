# Arbeitsanweisung & Prompt: Intuitives Drag & Drop im Wissensbaum und Bereinigung der Verschiebe-Buttons

> **Ziel:** Umstellung des Wissensbaums (`KnowledgeTree.razor`) auf ein modernes, visuelles Drag & Drop mit intuitiven Drop-Indikatoren (Vor, Unter, Nach) und vollständige Beseitigung des störenden „Button-Waldes“ (`tree-move-source-btn`, `tree-move-targets`).

---

## 1. Kontext & Intention (Warum machen wir das?)

### 1.1 Das Problem
In Milestone M4.3-T3 wurde die Anforderung *„Tastaturalternative für das Verschieben von Knoten“* sehr hemdsärmelig umgesetzt:
* Jeder Knoten im Baum besitzt permanent einen sichtbaren Button `Verschieben`.
* Sobald ein Knoten verschoben wird (per Klick oder Drag), blendet jeder andere Knoten drei separate Aktions-Buttons ein: `Unter „X“`, `Vor „X“`, `Nach „X“`.
* Diese Buttons dienen gleichzeitig als Klick-Ziele für die Tastatur und als HTML5-Drop-Ziele für die Maus.
* **Ergebnis:** Bei bereits wenigen Knoten ist der Bildschirm mit Dutzenden Buttons („56 Millionen Buttons“) überladen. Die visuelle Ruhe und Übersicht des Baumes wird vollständig zerstört. Ein natürliches Ziehen eines Knotens auf oder zwischen andere Knoten existiert nicht, da man auf winzige Textbuttons zielen muss.

### 1.2 Entscheidungen & Leitplanken
1. **Tastaturbedienung für das Verschieben entfällt:**
   * Gemäß Benutzerentscheidung ist eine gesonderte Tastaturbedienung (mit statischen Buttons im DOM) für das Verschieben von Knoten nicht erforderlich.
   * Der gesamte Button-Ballast (`tree-move-source-btn`, `tree-move-targets` mit den drei Buttons) wird restlos entfernt, um DOM und Code schlank und wartbar zu halten.
2. **Natives Drag & Drop statt externer Bibliothek:**
   * Leitentscheidung [K-022](tasks/webfrontend/README.md#L73) bleibt verbindlich: Es wird **keine** Third-Party-Tree-Bibliothek (wie Radzen, MudBlazor etc.) eingeführt.
   * Der native Baum mit seinem spezifischen 100er-Cursor-Paging, Prev/Next-Seiten und 10-Seiten-LRU-Cache bleibt das Fundament.
   * Intuitives Drag & Drop wird nativ und modern implementiert.
3. **Moderne UX für Drag & Drop:**
   * Ein Knoten kann per Maus an seiner Zeile gegriffen und gezogen werden (`draggable="true"`).
   * Beim Ziehen über einen Zielknoten bestimmt die vertikale Mausposition (`offsetY / height`) die Zielposition:
     * **Obere 25%:** Einfügen *Vor* dem Zielknoten (`TreeMovePosition.Before`) → Visueller Indikator: Blaue Linie an der Oberkante.
     * **Mittlere 50%:** Einfügen *Unter* den Zielknoten (`TreeMovePosition.Parent`) → Visueller Indikator: Umrandung / Flächen-Highlight des Zielknotens.
     * **Untere 25%:** Einfügen *Nach* dem Zielknoten (`TreeMovePosition.After`) → Visueller Indikator: Blaue Linie an der Unterkante.
   * Beim Loslassen (`drop`) wird die bestehende Mutation `TreeMoveCoordinator.MoveAsync` ausgelöst.

---

## 2. Architektur & Lösungsdesign

### 2.1 Entkopplung von Hover-Feedback und Blazor-Server (JS-Interop)
* Bei Blazor Interactive Server führt jedes serverseitige Event zu einem SignalR-Roundtrip. Ein kontinuierliches `dragover`-Event über den Server abzuwickeln, führt zu Latenzen und ruckeliger UI.
* **Lösung:** Ein schlankes JS-Interop-Modul (z. B. `src/KnowHowToAI.Server/wwwroot/js/treeDragDrop.js` oder Scoped JS `KnowledgeTree.razor.js`):
  * Überwacht `dragover`, `dragleave` und `drop` clientseitig im Browser.
  * Berechnet anhand von `event.clientY` relativ zur Bounding-Box des Zielknotens die Drop-Position (`Before`, `Parent`, `After`).
  * Setzt clientseitig sofort flüssig die CSS-Klassen:
    * `is-drop-before`
    * `is-drop-parent`
    * `is-drop-after`
  * Entfernt die Klassen bei `dragleave` und `dragend`.
  * Sendet erst beim tatsächlichen `drop`-Event einen einzigen Aufruf an Blazor zurück (mit `sourceNodeId`, `targetNodeId`, `position`).

### 2.2 Visuelle Gestaltung (CSS)
* Die gezogene Zeile erhält die Klasse `is-dragging` (z. B. `opacity: 0.45;`).
* Die Drop-Ziele nutzen Design-Token (`--ktai-color-primary`):
  * `is-drop-before`: `box-shadow: inset 0 2px 0 0 var(--ktai-color-primary);` oder ein Pseudo-Element `::before` als 2px-Linie oben.
  * `is-drop-after`: `box-shadow: inset 0 -2px 0 0 var(--ktai-color-primary);` oder ein Pseudo-Element `::after` als 2px-Linie unten.
  * `is-drop-parent`: `outline: 2px solid var(--ktai-color-primary); background-color: var(--ktai-color-info-background); border-radius: var(--ktai-radius-sm);`.

### 2.3 Bereinigung alter Komponenten & Zustände
* **Entfernen aus `KnowledgeTree.razor`:**
  * Button `tree-move-source-btn` (Zeilen 100–110).
  * Container `tree-move-targets` mit den drei Buttons `Unter…`, `Vor…`, `Nach…` (Zeilen 164–192).
* **Entfernen aus `KnowledgeTree.razor.cs`:**
  * Feld `private Guid? _moveSourceNodeId`.
  * Eigenschaft `private string MoveSourceTitle`.
  * Methode `private Task BeginMoveAsync(Guid nodeId)`.
  * Methode `private void EndDrag()`.
  * Überflüssige Focus-/Aktionsbutton-Logik für Move.
* **Entfernen aus `KnowledgeTree.razor.css`:**
  * `.tree-move-targets`, `.tree-move-targets.is-hidden`, `.tree-move-target`, `.tree-move-source-btn`.

---

## 3. Betroffene Dateien

| Bereich | Pfad | Beschreibung |
|---|---|---|
| **Roadmap** | `tasks/webfrontend/roadmap/04-transactions-und-strukturpflege.md` | Neuer Task M4.3-T5 verlinkt auf diesen Prompt |
| **Konzept** | `tasks/webfrontend/konzept/02-bedienkonzept-und-ui.md` | Aktualisierung: Tastaturalternative für Verschieben entfällt; natives D&D mit Linien-Indikatoren |
| **Tree Markup** | `src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgeTree.razor` | Entfernen der Buttons; saubere Drag & Drop Attribute |
| **Tree Code** | `src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgeTree.razor.cs` | Bereinigung von Button-Zuständen; Anbindung an Drop-Callback |
| **Tree Styling** | `src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgeTree.razor.css` | Bereinigung alter Button-Styles; Hinzufügen der Drop-Indikator-Styles |
| **JS-Interop** | `src/KnowHowToAI.Server/wwwroot/js/treeDragDrop.js` *(oder Scoped JS)* | Clientseitige Berechnung der Drop-Zone (`Before`/`Parent`/`After`) ohne SignalR-Latenz |
| **Unit-Tests** | `tests/KnowHowToAI.Web.Tests/Features/Knowledge/KnowledgeTreeTests.cs` | Beseitigung der Tests für `tree-move-source` und `tree-move-target` Buttons; Absicherung des neuen Renderings |
| **Browser-Tests** | `tests/KnowHowToAI.BrowserTests/Transactions/KnowledgeTreeMoveSmokeTests.cs` | Playwright E2E-Test: Umstellung von Button-Klicks auf echtes Drag & Drop gegen Zielknoten |

---

## 4. Detaillierte Checkliste für die Umsetzung

### Phase 1: Konzept & Dokumentation vorbereiten
- [ ] **Konzepte anpassen:** In `tasks/webfrontend/konzept/02-bedienkonzept-und-ui.md` den Passus bezüglich *„Aktionsbuttons für Verschieben“* anpassen (Tastatur-Verschiebebuttons entfallen zugunsten eines aufgeräumten DOMs und intuitivem D&D).

### Phase 2: Bereinigung der Altlasten (Button-Wald)
- [ ] **HTML bereinigen (`KnowledgeTree.razor`):**
  - [ ] `tree-move-source-btn` entfernen.
  - [ ] `tree-move-targets` Block mit den drei Buttons entfernen.
- [ ] **C#-Code bereinigen (`KnowledgeTree.razor.cs`):**
  - [ ] `_moveSourceNodeId`, `MoveSourceTitle` und `BeginMoveAsync` entfernen.
  - [ ] Nicht mehr benötigte Event-Handler und Hilfsmethoden entfernen.
- [ ] **CSS bereinigen (`KnowledgeTree.razor.css`):**
  - [ ] Alle `.tree-move-source-btn` und `.tree-move-target*` Selektoren entfernen.

### Phase 3: Intuitives Drag & Drop implementieren
- [ ] **JS-Interop für flüssiges Dragover:**
  - [ ] Modul `treeDragDrop.js` erstellen (oder Komponenten-Interop).
  - [ ] Ermittlung des Y-Offsets:
    - `< 25%`: `Before` (`is-drop-before`)
    - `25% - 75%`: `Parent` (`is-drop-parent`)
    - `> 75%`: `After` (`is-drop-after`)
  - [ ] Klassen-Management bei `dragleave` und `dragend`.
  - [ ] Bei `drop`: Aufruf des Blazor-Callbacks mit `targetNodeId` und `position`.
- [ ] **Blazor-Anbindung (`KnowledgeTree.razor.cs`):**
  - [ ] JS-Modul initialisieren und beim Dispose freigeben.
  - [ ] `[JSInvokable]` Methode oder Event-Callback für den erfolgreichen Drop entgegennehmen und `TreeMoveCoordinator.MoveAsync` ausführen.
  - [ ] Fehlerbehandlung (`_moveErrorMessage`) beibehalten.

### Phase 4: CSS für visuelle Drop-Indikatoren
- [ ] **Styling für gezogenes Element:** `.is-dragging` mit reduzierter Deckkraft (z. B. `opacity: 0.45`).
- [ ] **Styling für Vorher (`.is-drop-before`):** Markante 2px-Linie oben in `--ktai-color-primary`.
- [ ] **Styling für Unter (`.is-drop-parent`):** Umrandung und Flächenhervorhebung in `--ktai-color-primary` / Info-Hintergrund.
- [ ] **Styling für Nachher (`.is-drop-after`):** Markante 2px-Linie unten in `--ktai-color-primary`.

### Phase 5: Testanpassung & Verifikation
- [ ] **bUnit-Tests (`KnowledgeTreeTests.cs`):**
  - [ ] Alte Button-Tests (`KnowledgeTree_MoveActionButtons_*`) entfernen oder umschreiben.
  - [ ] Sicherstellen, dass keine Move-Buttons mehr im gerenderten HTML vorhanden sind.
  - [ ] Verifikation, dass der Baum im Normalzustand sauber rendert.
- [ ] **Playwright E2E-Tests (`KnowledgeTreeMoveSmokeTests.cs`):**
  - [ ] Alten Button-Klick-Test entfernen.
  - [ ] Drag-and-Drop Test aktualisieren: Echtes Ziehen des Quellknotens auf den Zielknoten (mit Positionierung für Before / Parent / After).
  - [ ] Verifizieren, dass der verschobene Knoten im Working Tree und nach Commit am neuen Ort persistiert ist.

### Phase 6: Abschluss & Qualitätsprüfung
- [ ] **Build:** `scripts/build.ps1` ausführen (0 Warnungen, 0 Fehler).
- [ ] **FastTests:** `scripts/test-fast.ps1` ausführen (alle bUnit- und Unit-Tests grün).
- [ ] **IntegrationTests:** `scripts/test-integration.ps1` ausführen.
- [ ] **Audit & Commit:** Saubere Git-Historie, Roadmap-Punkt aktualisieren.

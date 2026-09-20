# M5 – Rollen-Content und Rich Text

[Roadmap-Index](../../Roadmap.md)

- [ ] **M5 abschließen**

Abhängigkeit: [M4](../04-transactions-und-strukturpflege/roadmap.md)

Verbindliche M0-Basis: Editor ist ausschließlich Milkdown `@milkdown/crepe`; Tiptap und eine erneute Editor-Auswahl sind ausgeschlossen. Die Interopgrenze besteht aus `mount`, `readMarkdown`, `focus` und `dispose`. Vitest ist für diesen dünnen Pfad nicht erforderlich. Komponenten- und Browsernachweise verwenden bUnit mit xUnit v3 beziehungsweise Microsoft.Playwright .NET mit der installierten aktuellen Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Rollenabhängiger Markdown-Content kann vollständig und komfortabel ohne Agent gepflegt werden.

Referenzen: [Node-Ansicht und Editor](../../konzept/02-bedienkonzept-und-ui.md#node-ansicht-und-editor), [Rich-Text-Editor](../../konzept/03-content-und-assets.md#rich-text-editor), [Freier Content](../../konzept/03-content-und-assets.md#freier-content-einschließlich-todos), [Rollenverwaltung](../../konzept/02-bedienkonzept-und-ui.md#rollenverwaltung)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M5.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M5.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M4; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Markdown-Quellmodus (O-010), lokale npm-/Bundle-Erzeugung O-029 sowie die noch offenen Rollen-, Fallback-, Derived-Content- und Validierungsabläufe. Das Editorprodukt wird nicht erneut entschieden; die beim Build aufgelöste Version wird im Lockfile festgehalten.
  - Prüfen: produktive Transaction- und Konflikt-UX aus M4, Milkdown-Interopvertrag, sichere Contentpolicy, M0-Golden-Master und tatsächliche Core-Verträge gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M5-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M5.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M5.1 – Editorbasis

- [ ] **M5.1 abschließen**

  - [ ] **M5.1-T1 – [Rich-Text-Editor mit Markdownmodell integrieren](tasks/M5.1-T1.md)**
  - [ ] **M5.1-T2 – [Markdown-Roundtrip absichern](tasks/M5.1-T2.md)**
  - [ ] **M5.1-T3 – [Entscheidung zum Markdown-Quellmodus umsetzen](tasks/M5.1-T3.md)**
## M5.2 – Rollenverwaltung

- [ ] **M5.2 abschließen**

  - [ ] **M5.2-T1 – [Rollen vollständig pflegen](tasks/M5.2-T1.md)**
  - [ ] **M5.2-T2 – [Resolution Orders pflegen](tasks/M5.2-T2.md)**
## M5.3 – Rollen-Content

- [ ] **M5.3 abschließen**

  - [ ] **M5.3-T1 – [Rollen-Content erstellen, ersetzen und löschen](tasks/M5.3-T1.md)**
  - [ ] **M5.3-T2 – [Independent/Derived, Quellen und Freshness integrieren](tasks/M5.3-T2.md)**
## M5.4 – Validierung und freier Text

- [ ] **M5.4 abschließen**

  - [ ] **M5.4-T1 – [Heading- und Contentvalidierung im Editor darstellen](tasks/M5.4-T1.md)**
  - [ ] **M5.4-T2 – [TODOs als normalen Content regressionssicher abnehmen](tasks/M5.4-T2.md)**
## Milestone-Abnahme

- Rollen, Resolution Orders und Rollen-Content sind transaktional ohne Agent pflegbar.
- Markdown bleibt trotz WYSIWYG-Bearbeitung kanonisch und verlustarm.
- Fallback, Provenienz, Revision und Freshness bleiben sichtbar.
- TODOs besitzen keinerlei Sonderworkflow.

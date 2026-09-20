# M5 – Rollen-Content und Rich Text

[Roadmap-Index](../../Roadmap.md)

- [ ] **M5 abschließen**

Abhängigkeit: [M4](../04-transactions-und-strukturpflege/roadmap.md), einschließlich `ChangeVersion`-Schutz, Dirty-State, Navigationsschutz, Reconnect und `SnapshotConflict`-Reapply.

## Ziel

Rollen, Resolution Orders und rollenabhängiger Markdown-Content können in einer Working Transaction ohne Agent gepflegt werden. WYSIWYG- und Markdown-Quellmodus verwenden dasselbe kanonische Markdown, Fallback/Provenienz/Freshness bleiben sichtbar, und jede Mutation ist gegen stale Writes geschützt.

## Verbindliche Leitplanken

- Milkdown `@milkdown/crepe` bleibt der einzige spezialisierte Editor; die Interopgrenze besteht aus `mount`, `readMarkdown`, `focus` und `dispose`.
- Alle Rollen- und Content-Writes übergeben `TransactionId` und `expectedChangeVersion`; stale Writes werden atomar und strukturiert abgelehnt. Die neue `ChangeVersion` wird bis Web-UI und MCP transportiert.
- `RawHtmlNotAllowed`, `HeadingNotAllowed`, `FrontMatterNotAllowed`, `LinkTargetNotAllowed` und `ExternalImageNotAllowed` sind ein gemeinsamer Core-/MCP-/Web-Vertrag. Ungültiger Editorwert bleibt erhalten.
- npm + `package-lock.json` + esbuild erzeugen lokale, selbst gehostete Assets unter `src/KnowHowToAI.Server/wwwroot/generated/content-editor`; Node ist nur Buildvoraussetzung. `node_modules` und Generated Output werden nicht versioniert. Das Lizenzinventar wird mit dem Produktpaket aktualisiert.
- Fallback ist read-only. „Eigenen Inhalt anlegen“ und „Fallback als Ausgangstext übernehmen“ sind explizite Aktionen; kein stilles Bearbeiten oder Duplizieren der Quellrolle. Explizit leerer Content unterdrückt Fallback und erfordert Bestätigung; Löschen ist separat und reaktiviert Fallback.
- Moduswechsel ist explizit. `Derived` benötigt mindestens eine über Node-Suche und Rolle ausgewählte aktive explizite Source und pinnt ihre aktuelle Revision; freie GUID-Eingabe ist ausgeschlossen. Die Resolution-Order-Vorschau verwendet einen auswählbaren Beispiel-Node und sortiert gespeicherte Reihenfolgen nicht still um.
- Vitest wird nur eingeführt, wenn produktiver JS-/TS-Code eigene Zustände, Verzweigungen, Transformationen oder Retry-/Lifecyclelogik besitzt; dünnes Interop bleibt über bUnit/Playwright abgedeckt.

## M5.0 – Manuelle Planung und Konzeptschärfung

- [x] **M5.0 abschließen**
  - Durchführung: 2026-09-20 nach Abschluss von M4, dokumentiert in [planning.md](planning.md).
  - Entschieden: Rollenadministration bleibt Bestandteil von M5/V1; Markdown-Quellmodus wird aufgenommen; npm + Lockfile + esbuild ist die lokale Buildtoolchain; Fallback-, Leerinhalt-, Independent-/Derived- und Resolution-Order-Semantik ist festgelegt.
  - M4-Ergebnisse übernommen: O-025/O-027 bleiben geschlossen; `ChangeVersion`-Parity und bestehende Dirty-/Reconnect-/Konfliktverträge sind Voraussetzungen.
  - Konzepte, Index, Workshop, Webfrontend-Planungshorizont und alle M5-Leaf-Tasks sind aktualisiert. `docs/` bleibt unverändert, weil es nur Ist-Zustand beschreibt.
  - Gate: M5.1 bis M5.5 sind zur sequenziellen Agentenausführung freigegeben; M5.6 bleibt ein manueller Abschlussaudit.

## M5.1 – Gemeinsame Schreib- und Sicherheitsbasis

- [x] **M5.1 abschließen**
  - [x] [M5.1-T1 – Rollen- und Content-Writes gegen ChangeVersion absichern](tasks/M5.1-T1.md)
  - [x] [M5.1-T2 – Zentrale Contentpolicy und MCP-Fehlerverträge vervollständigen](tasks/M5.1-T2.md)
  - [x] [M5.1-T3 – npm-/esbuild-Toolchain und Lizenzinventar produktionsfähig anlegen](tasks/M5.1-T3.md)

## M5.2 – Editorbasis

- [ ] **M5.2 abschließen**
  - [x] [M5.2-T1 – Crepe-Editor, Lifecycle und Dirty-State integrieren](tasks/M5.2-T1.md)
  - [x] [M5.2-T2 – Golden Master, Paste und Sicherheits-Roundtrip abnehmen](tasks/M5.2-T2.md)
  - [ ] [M5.2-T3 – Markdown-Quellmodus integrieren](tasks/M5.2-T3.md)

## M5.3 – Rollenverwaltung

- [ ] **M5.3 abschließen**
  - [ ] [M5.3-T1 – Rollen in der Working Transaction pflegen](tasks/M5.3-T1.md)
  - [ ] [M5.3-T2 – Resolution Orders und Fallback-Vorschau pflegen](tasks/M5.3-T2.md)

## M5.4 – Rollen-Content

- [ ] **M5.4 abschließen**
  - [ ] [M5.4-T1 – Explicit-, Fallback- und Leerinhalt sicher pflegen](tasks/M5.4-T1.md)
  - [ ] [M5.4-T2 – Independent/Derived, Sources und Freshness integrieren](tasks/M5.4-T2.md)

## M5.5 – Ende-zu-Ende-Abnahme

- [ ] **M5.5 abschließen**
  - [ ] [M5.5-T1 – Rollen-/Content-Workflows mit Navigation, Reconnect und Parallelität abnehmen](tasks/M5.5-T1.md)
  - [ ] [M5.5-T2 – Transaktions-, TODO- und Fehlerwert-Abnahme vervollständigen](tasks/M5.5-T2.md)

## M5.6 – Manueller Milestone-Audit

- [ ] **M5.6 – M5-Abschlussaudit manuell mit dem Benutzer durchführen**
  - Durchführung erst nach M5.1–M5.5; kein delegierbarer Implementierungs-Leaf-Task.
  - Prüfen: Ziel, Abnahmekriterien, Invarianten, Sicherheitsverträge, `docs/`-Ist-Doku und belegte Nachweise.

## Milestone-Abnahme

- [ ] Rollen und Resolution Orders sind ohne MCP in einer Working Transaction pflegbar.
- [ ] Explicit, Fallback, explizit leer, Independent und Derived sind nachvollziehbar und sicher bearbeitbar.
- [ ] WYSIWYG und Markdown-Quellmodus erhalten kanonisches Markdown ohne unbemerkten Verlust.
- [ ] Policy-Fehler werden serverseitig stabil abgelehnt; Dirty-State, ChangeVersion, Navigation, Reconnect und Serverfehlerwert-Erhalt bleiben wirksam.
- [ ] TODO bleibt normaler Content ohne Sonderworkflow.

## Audit

- [ ] Begrenzter Audit gegen Ziel, Kriterien, Invarianten und Nachweise
- Ergebnis: offen

# Begrenzter Audit – Einheitliches Reiterlayout

## Ergebnis

Audit bestanden nach einer gezielten Korrekturrunde. Der Befund ist behoben,
die Linkformatierung bleibt über die feste Leiste verfügbar und die neuen
Browsernachweise zeigen keine zweite kontextuelle Linkleiste.

## Befund und Korrektur

- **P2 – Crepe-LinkTooltip blieb zusätzlich zur festen Formatierungsleiste
  sichtbar.** Die ursprünglichen P4-Nachweise
  [`07_knowledge_editor_workspace_1280x800.png`](../../temp/ui-audit/2026-09-23_18-25-26/07_knowledge_editor_workspace_1280x800.png)
  und
  [`15_knowledge_working-missing-own-content-editor_1280x800.png`](../../temp/ui-audit/2026-09-23_18-25-26/15_knowledge_working-missing-own-content-editor_1280x800.png)
  zeigten Link-Vorschau-/Bearbeiten-Schaltflächen und „Paste link…“ im
  fokussierten visuellen Editor.
- Die Crepe-7.22.1-Quellkonfiguration bestätigte die Ursache: `LinkTooltip`
  ist standardmäßig aktiv; `Toolbar`, `TopBar` und `BlockEdit` waren bereits
  deaktiviert. Das Abschalten der `LinkTooltip`-Feature entfernt ihre
  kontextuellen DOM-Elemente. Da der Crepe-Link-Command selbst nur die
  Tooltip-API aufruft, verwendet die feste Link-Schaltfläche nun die bestehende
  CommonMark-Linkmarkierung direkt auf der Auswahl. Die native Adressabfrage
  legt einen Link an, bearbeitet eine vorhandene Adresse oder entfernt den
  Link mit leerer Eingabe. Es wurde keine neue Formatart oder Abhängigkeit
  eingeführt.
- Neue Sichtnachweise bei 1280 × 800:
  [`07_knowledge_editor_workspace_1280x800.png`](../../temp/ui-audit/2026-09-23_18-50-08/07_knowledge_editor_workspace_1280x800.png)
  und
  [`15_knowledge_working-missing-own-content-editor_1280x800.png`](../../temp/ui-audit/2026-09-23_18-50-08/15_knowledge_working-missing-own-content-editor_1280x800.png).
  Beide zeigen den fokussierten Editor ohne `.milkdown-link-preview` und
  `.milkdown-link-edit`; die feste Formatierungsleiste bleibt sichtbar. Die
  Bilder wurden visuell geprüft.

## Verifikation

- Browser-Regression für Link anlegen, Adresse bearbeiten, gespeicherten
  Markdown-Roundtrip und fehlende Tooltip-Controls: 1/1.
- `KnowledgeDirectEditingSmokeTests` und `PageFrameSmokeTests` zusammen mit
  `ContentEditorSourceSmokeTests`: 9/9; erneuter Source-Smoke nach der
  Linkbearbeitungs-Assertion: 1/1.
- Aktivierter `UiAuditScreenshotTests`-Lauf: 1/1.
- Vollständige FastTests: Frontend 8/8; Analyzer 6/6; Core 419/419;
  Integration Unit 286/286; Web 308/308 (insgesamt 1.019/1.019).
- `pwsh -NoProfile -File scripts/build.ps1`: Exitcode 0.
- `verify(targetPath)` und `verify(targetPath, scope: "solution")`: beide
  `pass`, 10.0, 0 Verstöße.
- `git diff --check`: sauber.

## Geprüfter Umfang

Gelesen wurden Konzept, Roadmap, ursprünglicher Audit-Befund, AGENTS.md,
Umsetzungsworkflow, Web-UI-, Linter-, Test- und Dokumentationsregeln sowie
Crepe-Feature-/Link-Tooltip-Konfiguration, Editorinitialisierung, Browser-DOM,
Editor-Smokes und `docs/WebUi.md`/`docs/Architektur.md`. Der vorhandene
P4-Abschlusscommit ist `a756058`; kein zusätzlicher P4-Abschlusscommit war
erforderlich. Der Korrekturscope wird atomar und ohne Push committet.

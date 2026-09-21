# M3 – Read-only Wissenscockpit

[Roadmap-Index](../../Roadmap.md)

- [x] **M3 abschließen**

Abhängigkeit: [M2](../02-designsystem-und-shell/roadmap.md)

Verbindliche M0-Basis: Der Tree ist nativ und lädt Children serverseitig mit opaken Cursors und exakt 100 Einträgen pro Seite; höchstens zehn Seiten liegen gleichzeitig im Circuit. Radzen und eine erneute Tree-Auswahl sind ausgeschlossen. Komponenten- und Browsernachweise verwenden bUnit mit xUnit v3 beziehungsweise Microsoft.Playwright .NET mit der installierten aktuellen Google-Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Menschen können den gesamten vorhandenen Wissensstand, seine Struktur, Zielgruppenauflösung und Historie ohne MCP-Client verstehen.

Referenzen: [Dashboard](../../konzept/02-bedienkonzept-und-ui.md#dashboard), [Wissensbaum](../../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [Historie und Releases](../../konzept/02-bedienkonzept-und-ui.md#historie-und-releases), [Blazor-interne Aufrufe](../../konzept/05-architektur-api-und-mcp.md#blazor-interne-aufrufe)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M3.0 – Manuelle Planung und Konzeptschärfung

- [x] **M3.0 abschließen**
  - Durchgeführt am 2026-09-19 gemeinsam mit dem Benutzer.
  - Entschieden: O-008 (initiale Zielgruppe: letzten gespeicherten `localStorage`-Wert verwenden, Pflichtauswahl bei fehlendem/ungültigem Eintrag), O-007 (Actor über `ICurrentUserService`-Seam, initiale Dummy-Implementierung), O-025 (mehrere gleichzeitige Clients erlaubt, keine Locks, `ChangeVersion`-Ablehnung), O-026 (keine automatische Transaction-Lebensdauer, Alter im Dashboard sichtbar, Warnbadge ab 7 Tagen), O-027 (Undo nur im Editor bis Speichern, kein globaler Undo-Stack).
  - Konzepte aktualisiert: [Bedienkonzept und UI](../../konzept/02-bedienkonzept-und-ui.md), [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md), [Offene Fragen](../../konzept/07-entscheidungen-und-offene-fragen.md).
  - Gate: M3.1 und folgende Arbeitspakete sind durch Implementierungsagenten ausführbar.

## M3.1 – Web-Lesegrenze

- [x] **M3.1 abschließen**

  - [x] **M3.1-T1 – [Transportneutrale Lese-Use-Cases für die UI anbinden](tasks/M3.1-T1.md)**
## M3.2 – Dashboard

- [x] **M3.2 abschließen**

  - [x] **M3.2-T1 – [Wissensdashboard implementieren](tasks/M3.2-T1.md)**
## M3.3 – Hierarchienavigation

- [x] **M3.3 abschließen**

  - [x] **M3.3-T1 – [Lazy-Loading-Datenadapter für den Wissensbaum implementieren](tasks/M3.3-T1.md)**
  - [x] **M3.3-T2 – [Read-only Knowledge Tree und Breadcrumbs implementieren](tasks/M3.3-T2.md)**
## M3.4 – Zielgruppe, Lesekontext und Node

- [x] **M3.4 abschließen**

  - [x] **M3.4-T1 – [Globalen Zielgruppen- und Lesekontext-Selektor implementieren](tasks/M3.4-T1.md)**
  - [x] **M3.4-T2 – [Read-only Node-Detailansicht implementieren](tasks/M3.4-T2.md)**
## M3.5 – Suche

- [x] **M3.5 abschließen**

  - [x] **M3.5-T1 – [Paginierte Wissenssuche implementieren](tasks/M3.5-T1.md)**
  - [x] **M3.5-T2 – [Wissensfilter implementieren](tasks/M3.5-T2.md)**
  - [x] **M3.5-T3 – [Suche gegen den deterministischen Browserbestand stabilisieren](tasks/M3.5-T3.md)**
## M3.6 – Historie und Releases

- [x] **M3.6 abschließen**

  - [x] **M3.6-T1 – [Snapshot- und Releaseübersichten implementieren](tasks/M3.6-T1.md)**
  - [x] **M3.6-T2 – [Snapshot-Diff-Ansicht implementieren](tasks/M3.6-T2.md)**
## M3.7 – Markdown-Export

- [x] **M3.7 abschließen**

  - [x] **M3.7-T1 – [Bestehenden Markdown-Teilbaumexport in der UI bereitstellen](tasks/M3.7-T1.md)**
  - [x] **M3.7-T2 – [Deterministischen Browsernachweis für den Markdown-Download herstellen](tasks/M3.7-T2.md)**
## M3.8 – Fachliche Parität

- [x] **M3.8 abschließen**

  - [x] **M3.8-T1 – [UI- und MCP-Leseergebnisse gegen gemeinsame Use Cases prüfen](tasks/M3.8-T1.md)**
## M3.9 – Audit-Nacharbeiten

- [x] **M3.9 abschließen**
  - Auditbasis: Code- und Nachweisprüfung am 2026-09-19 gegen `603f830`; M4-Arbeitsstand ist nicht Teil des Befunds.

  - [x] **M3.9-T1 – [Tree-Circuit tatsächlich begrenzen und Request-Rennen schließen](tasks/M3.9-T1.md)**
  - [x] **M3.9-T2 – [Direkte Node-Navigation über beliebige Cursorseiten zuverlässig rekonstruieren](tasks/M3.9-T2.md)**
  - [x] **M3.9-T3 – [Web-Lesegrenze für Zielgruppen, Diagnostik und Working-Version vervollständigen](tasks/M3.9-T3.md)**
  - [x] **M3.9-T4 – [Provenienz und Tree-Status aus echten Reads anzeigen](tasks/M3.9-T4.md)**
  - [x] **M3.9-T5 – [Dashboard partiell fehlertolerant und mengenfest machen](tasks/M3.9-T5.md)**
  - [x] **M3.9-T6 – [Historienmetadaten und echten Browser-Diff nachweisen](tasks/M3.9-T6.md)**
  - [x] **M3.9-T7 – [Markdown-Downloadfehler vertragstreu abschließen](tasks/M3.9-T7.md)**
  - [x] **M3.9-T8 – [M3 erneut gesamthaft abnehmen](tasks/M3.9-T8.md)**
## Milestone-Abnahme

- Der vorhandene Wissensstand ist ohne MCP-Client navigierbar, suchbar, historisch einsehbar und als Markdown exportierbar.
- Zielgruppe, Read Context, Fallback, Provenienz und Freshness sind sichtbar.
- UI und MCP verwenden dieselben Application-Use-Cases ohne duplizierte Fachlogik.

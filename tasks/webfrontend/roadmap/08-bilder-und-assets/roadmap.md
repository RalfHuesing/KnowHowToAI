# M8 – Bilder und Assetverwaltung

[Roadmap-Index](../../Roadmap.md)

- [ ] **M8 abschließen**

Priorität: niedrig. Umsetzung nach dem PDF-Export.

Abhängigkeit: [M5](../05-rollen-content-und-rich-text/roadmap.md), [M7](../07-pdf-export/roadmap.md)

Verbindliche M0-Basis: Bilder erweitern den bestehenden Milkdown-Editor `@milkdown/crepe` über dessen bereits vorbereiteten internen Hook; es findet keine neue Editor- oder Komponentenwahl statt. Browserfälle verwenden Microsoft.Playwright .NET mit der installierten aktuellen Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Bilder sind stabil referenzierbar, historisch reproduzierbar und in Editor, Browser, MCP sowie Export konsistent.

Referenz: [Bilder und Assets](../../konzept/03-content-und-assets.md#bilder-und-assets)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M8.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M8.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M7; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Asset-Speicherstrategie und Backupkopplung (O-004), erlaubte Bildtypen und Grenzen (O-016) sowie Upload-, Referenz- und Bereinigungs-UX.
  - Prüfen: produktiven Editor, Browser-/MCP-/Markdown-/PDF-Ausgabepfade, reale Deploymenttopologie und Datenmengen gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M8-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M8.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M8.1 – Daten- und Anwendungsmodell

- [ ] **M8.1 abschließen**

  - [ ] **M8.1-T1 – [Asset-Speichermodell entscheiden](tasks/M8.1-T1.md)**
  - [ ] **M8.1-T2 – [Asset-Domain- und Application-Contracts einführen](tasks/M8.1-T2.md)**
  - [ ] **M8.1-T3 – [SQL-Schema und Migration für Assetmetadaten implementieren](tasks/M8.1-T3.md)**
## M8.2 – Speicherung und Deduplizierung

- [ ] **M8.2 abschließen**

  - [ ] **M8.2-T1 – [Immutable Asset-Speicherung implementieren](tasks/M8.2-T1.md)**
  - [ ] **M8.2-T2 – [Hash-Deduplizierung implementieren](tasks/M8.2-T2.md)**
## M8.3 – Upload und Auslieferung

- [ ] **M8.3 abschließen**

  - [ ] **M8.3-T1 – [Kontrollierten Browserupload bereitstellen](tasks/M8.3-T1.md)**
  - [ ] **M8.3-T2 – [Kontrollierte Asset-Auslieferung bereitstellen](tasks/M8.3-T2.md)**
## M8.4 – Editorintegration

- [ ] **M8.4 abschließen**

  - [ ] **M8.4-T1 – [Bild-Upload im Rich-Text-Editor integrieren](tasks/M8.4-T1.md)**
  - [ ] **M8.4-T2 – [Markdown-Bildreferenzen roundtrip-sicher machen](tasks/M8.4-T2.md)**
## M8.5 – Lebenszyklus und Adapter

- [ ] **M8.5 abschließen**

  - [ ] **M8.5-T1 – [Assetauflösung über alle Ausgabekanäle vereinheitlichen](tasks/M8.5-T1.md)**
  - [ ] **M8.5-T2 – [Referenzierte, historische und verwaiste Assets behandeln](tasks/M8.5-T2.md)**
## M8.6 – Inhalts- und Ressourcengrenzen

- [ ] **M8.6 abschließen**

  - [ ] **M8.6-T1 – [Assetgrenzen und Schadinhaltsschutz absichern](tasks/M8.6-T1.md)**
## Milestone-Abnahme

- Bilder funktionieren über Upload, Editor, Browser, MCP, Markdown- und PDF-Export mit stabilen Referenzen.
- Binärinhalte sind immutable und dedupliziert.
- Historische Snapshots und Releases verlieren ihre Assets nicht.
- Grenzen und Fehlerfälle sind serverseitig durchgesetzt.

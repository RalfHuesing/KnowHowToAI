# M2 – Designsystem und Anwendungsshell

[Roadmap-Index](../../Roadmap.md)

- [x] **M2 abschließen**

Abhängigkeit: [M1](../01-webhost-und-mcp-http/roadmap.md)

Verbindliche M0-Basis: keine allgemeine UI-Bibliothek. M2 verwendet natives Blazor, semantisches HTML, eigenes CSS und nur schmale lokale JS-Isolation für Funktionen wie den nativen `dialog`. Es findet keine erneute Komponenten- oder Testwerkzeugsuche statt.

Ziel: Alle Fachfeatures erhalten eine konsistente, moderne und belastbare UI-Grundlage.

Referenzen: [Visueller Stil](../../konzept/02-bedienkonzept-und-ui.md#visueller-stil), [Grundlayout](../../konzept/02-bedienkonzept-und-ui.md#grundlayout), [Blazor-Betrieb](../../konzept/06-betrieb-sicherheit-und-risiken.md#blazor-betrieb)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M2.1 – Komponentenbasis und Theme

- [x] **M2.1 abschließen**

  - [x] **M2.1-T1 – [Native UI-Basis implementieren](tasks/M2.1-T1.md)**
  - [x] **M2.1-T2 – [Design-Tokens und Business-Theme definieren](tasks/M2.1-T2.md)**
## M2.2 – Anwendungsshell

- [x] **M2.2 abschließen**

  - [x] **M2.2-T1 – [Hauptlayout und Navigation implementieren](tasks/M2.2-T1.md)**
  - [x] **M2.2-T2 – [Globale Wissenskontextleiste implementieren](tasks/M2.2-T2.md)**
## M2.3 – Wiederverwendbare UI-Zustände

- [x] **M2.3 abschließen**

  - [x] **M2.3-T1 – [Lade-, Leer- und Fehlerzustände bereitstellen](tasks/M2.3-T1.md)**
  - [x] **M2.3-T2 – [Warnungs-, Bestätigungs- und Änderungszustände bereitstellen](tasks/M2.3-T2.md)**
## M2.4 – Robustheit und Zugänglichkeit

- [x] **M2.4 abschließen**

  - [x] **M2.4-T1 – [Responsive Mindestdarstellung und Tastaturnavigation absichern](tasks/M2.4-T1.md)**
  - [x] **M2.4-T2 – [Reconnect- und Circuit-Verlust-Oberfläche implementieren](tasks/M2.4-T2.md)**
  - [x] **M2.4-T3 – [Komponenten- und visuelle Smoke-Testbasis erweitern](tasks/M2.4-T3.md)**
## Milestone-Abnahme

- Einheitliche Anwendungsshell ohne fachliche Platzhalterlogik.
- Design, Zustände, Reconnect und grundlegende Zugänglichkeit sind wiederverwendbar abgesichert.
- Alle folgenden Features verwenden dieselbe Komponenten- und Themebasis.

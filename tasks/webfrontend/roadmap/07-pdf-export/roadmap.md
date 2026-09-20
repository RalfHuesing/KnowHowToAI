# M7 – Einfacher PDF-Teilbaumexport

[Roadmap-Index](../../Roadmap.md)

- [ ] **M7 abschließen**

Priorität: niedrig. Umsetzung erst nach dem gehärteten Kernfrontend.

Abhängigkeit: [M6](../06-betrieb-und-qualitaet/roadmap.md)

Verbindliche M0-Testbasis: Browserfälle verwenden weiterhin Microsoft.Playwright .NET ausschließlich mit der installierten aktuellen Google-Chrome-Stable-Version, `Channel = "chrome"` und `Headless = true`. Für PDF wird kein zweites Browserframework eingeführt; Rendererwerkzeuge werden ausschließlich im M7.0-Gate festgelegt und ihre aufgelösten Versionen im Lockfile dokumentiert.

Ziel: Der Benutzer lädt vom aktuellen Node aus dessen gesamten Teilbaum im einzigen konfigurierten Layout als PDF herunter.

Referenz: [Publikation und PDF](../../konzept/04-publikation-und-pdf.md)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M7.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M7.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M6; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: PDF-Basislayout (O-011), verbindliche Zielumgebung der Renderer und der konkrete Benutzerablauf auf Basis des dann fertigen Kernfrontends.
  - Prüfen: aktuellen Markdown-Export, Contentpolicy, Deploymentbedingungen, installierbare Werkzeugversionen und Lizenzpflichten gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M7-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M7.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M7.1 – Werkzeuge und Template

- [ ] **M7.1 abschließen**

  - [ ] **M7.1-T1 – [Einzelnen PDF-Templateordner anlegen](tasks/M7.1-T1.md)**
  - [ ] **M7.1-T2 – [Pandoc und WeasyPrint konfigurierbar validieren](tasks/M7.1-T2.md)**
## M7.2 – Konvertierung

- [ ] **M7.2 abschließen**

  - [ ] **M7.2-T1 – [PDF-Application-Service aus dem bestehenden Teilbaumexport bauen](tasks/M7.2-T1.md)**
  - [ ] **M7.2-T2 – [Template-Ressourcen und Prozessgrenzen absichern](tasks/M7.2-T2.md)**
## M7.3 – Browserablauf

- [ ] **M7.3 abschließen**

  - [ ] **M7.3-T1 – [PDF-Exportaktion und Download implementieren](tasks/M7.3-T1.md)**
## M7.4 – Abnahmetests

- [ ] **M7.4 abschließen**

  - [ ] **M7.4-T1 – [PDF-Pipeline integriert und visuell abnehmen](tasks/M7.4-T1.md)**
## Milestone-Abnahme

- Aktueller Node und alle Nachfahren werden im einzigen Template exportiert.
- Pandoc und WeasyPrint laufen begrenzt und diagnostizierbar auf dem Server.
- Freier Content einschließlich TODOs erscheint unverändert gemäß Exportsemantik.
- Verwaltete Content-Bilder werden erst im nachfolgenden Asset-Milestone integriert.
- Mehrere Profile, Freigabeworkflow und Exporthistorie bleiben ausdrücklich außerhalb des Scopes.

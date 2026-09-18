# M7 – Einfacher PDF-Teilbaumexport

[Roadmap-Index](../Roadmap.md)

- [ ] **M7 abschließen**

Priorität: niedrig. Umsetzung erst nach dem gehärteten Kernfrontend.

Abhängigkeit: [M6](06-betrieb-und-qualitaet.md)

Verbindliche M0-Testbasis: Browserfälle verwenden weiterhin Microsoft.Playwright .NET `1.62.0` ausschließlich mit Google Chrome Stable `152.0.7977.83`, `Channel = "chrome"` und `Headless = true`. Für PDF wird kein zweites Browserframework eingeführt; Rendererwerkzeuge und ihre Versionen werden ausschließlich im M7.0-Gate festgelegt.

Ziel: Der Benutzer lädt vom aktuellen Node aus dessen gesamten Teilbaum im einzigen konfigurierten Layout als PDF herunter.

Referenz: [Publikation und PDF](../konzept/04-publikation-und-pdf.md)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M7.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M7.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M6; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: PDF-Basislayout (O-011), verbindliche Zielumgebung der Renderer und der konkrete Benutzerablauf auf Basis des dann fertigen Kernfrontends.
  - Prüfen: aktuellen Markdown-Export, Contentpolicy, Deploymentbedingungen, installierbare Werkzeugversionen und Lizenzpflichten gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M7-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M7.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M7.1 – Werkzeuge und Template

- [ ] **M7.1 abschließen**

  - [ ] **M7.1-T1 – Einzelnen PDF-Templateordner anlegen**
    - Voraussetzung: O-011 zum PDF-Basislayout ist durch den Benutzer entschieden.
    - Umfang: genau ein HTML-Template, CSS, Logo und optionale Fonts als Deploymentdateien.
    - Prüfen: alle in O-011 entschiedenen Layoutbestandteile sowie lange Titel, Tabellen, Codeblöcke und Seitenumbrüche.
    - Ausschluss: Profilverwaltung, mehrere Templates und UI-Konfiguration.
    - Abnahme: ein repräsentatives statisches Beispieldokument besitzt ein brauchbares Layout.

  - [ ] **M7.1-T2 – Pandoc und WeasyPrint konfigurierbar validieren**
    - Umfang: Pfade/Versionen konfigurieren und Verfügbarkeit beim Start mit klarer Diagnose prüfen.
    - Prüfen: lokale Entwicklung, Zieldeployment, fehlendes Tool, inkompatible Version und Fonts.
    - Dokumentation: Installation und Betriebsvoraussetzungen aktualisieren.
    - Abnahme: fehlerhafte Umgebung scheitert früh und verständlich.

## M7.2 – Konvertierung

- [ ] **M7.2 abschließen**

  - [ ] **M7.2-T1 – PDF-Application-Service aus dem bestehenden Teilbaumexport bauen**
    - Umfang: Node, Rolle und Read Context an `export_tree` übergeben; Markdown mit Template per Pandoc und `--pdf-engine=weasyprint` konvertieren.
    - Semantik: Root exportiert alles, innerer Node nur sich und Nachfahren; TODOs bleiben unverändert enthalten.
    - Tests: Root, Teilbaum, Rollen-Fallback, leerer Inhalt und ungültiger Kontext.
    - Abnahme: Service liefert gültige PDF-Bytes ohne Webabhängigkeit.

  - [ ] **M7.2-T2 – Template-Ressourcen und Prozessgrenzen absichern**
    - Umfang: Template, CSS, Logo und Fonts in einem kontrollierten Arbeitsverzeichnis bereitstellen; Pandoc-/WeasyPrint-Prozesse kapseln.
    - Schutz: Timeout, Abbruch, Ressourcen-/Pfadgrenzen, bereinigte Temporärdaten und gefilterte Fehlerdiagnose.
    - Prüfen: fehlende/beschädigte Template-Ressource, Prozessfehler, große zulässige Ausgabe und parallele Exporte.
    - Abnahme: Konverter erhält keinen unkontrollierten lokalen oder externen Ressourcenzugriff.

## M7.3 – Browserablauf

- [ ] **M7.3 abschließen**

  - [ ] **M7.3-T1 – PDF-Exportaktion und Download implementieren**
    - Umfang: `PDF-Export` am aktuellen Node, Rolle und Read Context; Busy-Indikator ohne erfundene Prozentanzeige.
    - Ausgabe: kontrollierter Download mit geeignetem Dateinamen und `application/pdf`.
    - Zustände: läuft, Erfolg, fachlicher Fehler, Toolfehler und Timeout. Der erste Stand besitzt keinen separaten Abbrechen-Button; Request-Abbruch wird bis zur Prozessgrenze weitergereicht.
    - Abnahme: ein Klick erzeugt und lädt das Dokument; UI-Circuit bleibt bedienbar beziehungsweise sauber blockiert.

## M7.4 – Abnahmetests

- [ ] **M7.4 abschließen**

  - [ ] **M7.4-T1 – PDF-Pipeline integriert und visuell abnehmen**
    - Umfang: Root, Teilbaum, Rolle/Fallback, Tabellen, Code, Seitenumbrüche, Unicode, TODO und Fehlerfälle.
    - Automatisieren: PDF-Signatur, Seiten-/Textinhalte und zentrale Pipelinefehler; keine fragile Bytegleichheit.
    - Manuell/gerendert prüfen: repräsentative Seiten auf Layoutfehler.
    - Abnahme: definierter Beispieldatenstand erzeugt ein lesbares, vollständig herunterladbares PDF.

## Milestone-Abnahme

- Aktueller Node und alle Nachfahren werden im einzigen Template exportiert.
- Pandoc und WeasyPrint laufen begrenzt und diagnostizierbar auf dem Server.
- Freier Content einschließlich TODOs erscheint unverändert gemäß Exportsemantik.
- Verwaltete Content-Bilder werden erst im nachfolgenden Asset-Milestone integriert.
- Mehrere Profile, Freigabeworkflow und Exporthistorie bleiben ausdrücklich außerhalb des Scopes.

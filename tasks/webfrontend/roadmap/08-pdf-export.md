# M8 – Einfacher PDF-Teilbaumexport

[Roadmap-Index](../Roadmap.md)

- [ ] **M8 abschließen**

Priorität: niedrig. Umsetzung erst nach dem gehärteten Kernfrontend.

Abhängigkeit: [M6](06-bilder-und-assets.md), [M7](07-betrieb-und-qualitaet.md)

Ziel: Der Benutzer lädt vom aktuellen Node aus dessen gesamten Teilbaum im einzigen konfigurierten Layout als PDF herunter.

Referenz: [Publikation und PDF](../konzept/04-publikation-und-pdf.md)

## M8.1 – Werkzeuge und Template

- [ ] **M8.1 abschließen**

  - [ ] **M8.1-T1 – Einzelnen PDF-Templateordner anlegen**
    - Umfang: genau ein HTML-Template, CSS, Logo und optionale Fonts als Deploymentdateien.
    - Prüfen: Inhaltsverzeichnis, Seitenformat, Seitenumbrüche, Header/Footer nur soweit mit dem einfachen Ziel nötig.
    - Ausschluss: Profilverwaltung, mehrere Templates und UI-Konfiguration.
    - Abnahme: ein repräsentatives statisches Beispieldokument besitzt ein brauchbares Layout.

  - [ ] **M8.1-T2 – Pandoc und WeasyPrint konfigurierbar validieren**
    - Umfang: Pfade/Versionen konfigurieren und Verfügbarkeit beim Start mit klarer Diagnose prüfen.
    - Prüfen: lokale Entwicklung, Zieldeployment, fehlendes Tool, inkompatible Version und Fonts.
    - Dokumentation: Installation und Betriebsvoraussetzungen aktualisieren.
    - Abnahme: fehlerhafte Umgebung scheitert früh und verständlich.

## M8.2 – Konvertierung

- [ ] **M8.2 abschließen**

  - [ ] **M8.2-T1 – PDF-Application-Service aus dem bestehenden Teilbaumexport bauen**
    - Umfang: Node, Rolle und Read Context an `export_tree` übergeben; Markdown mit Template per Pandoc und `--pdf-engine=weasyprint` konvertieren.
    - Semantik: Root exportiert alles, innerer Node nur sich und Nachfahren; TODOs bleiben unverändert enthalten.
    - Tests: Root, Teilbaum, Rollen-Fallback, leerer Inhalt und ungültiger Kontext.
    - Abnahme: Service liefert gültige PDF-Bytes ohne Webabhängigkeit.

  - [ ] **M8.2-T2 – Assets und Prozessgrenzen absichern**
    - Umfang: Assetreferenzen in kontrolliertem Arbeitsverzeichnis auflösen; Pandoc-/WeasyPrint-Prozesse kapseln.
    - Schutz: Timeout, Abbruch, Ressourcen-/Pfadgrenzen, bereinigte Temporärdaten und gefilterte Fehlerdiagnose.
    - Prüfen: fehlendes/beschädigtes Bild, Prozessfehler, große zulässige Ausgabe und parallele Exporte.
    - Abnahme: Konverter erhält keinen unkontrollierten lokalen oder externen Ressourcenzugriff.

## M8.3 – Browserablauf

- [ ] **M8.3 abschließen**

  - [ ] **M8.3-T1 – PDF-Exportaktion und Download implementieren**
    - Umfang: `PDF-Export` am aktuellen Node, Rolle und Read Context; Busy-Indikator ohne erfundene Prozentanzeige.
    - Ausgabe: kontrollierter Download mit geeignetem Dateinamen und `application/pdf`.
    - Zustände: läuft, Erfolg, fachlicher Fehler, Toolfehler, Timeout und Benutzerabbruch soweit technisch möglich.
    - Abnahme: ein Klick erzeugt und lädt das Dokument; UI-Circuit bleibt bedienbar beziehungsweise sauber blockiert.

## M8.4 – Abnahmetests

- [ ] **M8.4 abschließen**

  - [ ] **M8.4-T1 – PDF-Pipeline integriert und visuell abnehmen**
    - Umfang: Root, Teilbaum, Rolle/Fallback, Bilder, Tabellen, Code, Seitenumbrüche, Unicode, TODO und Fehlerfälle.
    - Automatisieren: PDF-Signatur, Seiten-/Textinhalte und zentrale Pipelinefehler; keine fragile Bytegleichheit.
    - Manuell/gerendert prüfen: repräsentative Seiten auf Layoutfehler.
    - Abnahme: definierter Beispieldatenstand erzeugt ein lesbares, vollständig herunterladbares PDF.

## Milestone-Abnahme

- Aktueller Node und alle Nachfahren werden im einzigen Template exportiert.
- Pandoc und WeasyPrint laufen begrenzt und diagnostizierbar auf dem Server.
- Bilder und freier Content einschließlich TODOs erscheinen unverändert gemäß Exportsemantik.
- Mehrere Profile, Freigabeworkflow und Exporthistorie bleiben ausdrücklich außerhalb des Scopes.

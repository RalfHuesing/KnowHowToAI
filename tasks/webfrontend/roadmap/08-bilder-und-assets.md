# M8 – Bilder und Assetverwaltung

[Roadmap-Index](../Roadmap.md)

- [ ] **M8 abschließen**

Priorität: niedrig. Umsetzung nach dem PDF-Export.

Abhängigkeit: [M5](05-rollen-content-und-rich-text.md), [M7](07-pdf-export.md)

Verbindliche M0-Basis: Bilder erweitern den bestehenden Milkdown-Editor `@milkdown/crepe` `7.22.1` über dessen bereits vorbereiteten internen Hook; es findet keine neue Editor- oder Komponentenwahl statt. Browserfälle bleiben bei Microsoft.Playwright .NET `1.62.0` mit Chrome (installierte aktuelle Version), `Channel = "chrome"`, `Headless = true`.

Ziel: Bilder sind stabil referenzierbar, historisch reproduzierbar und in Editor, Browser, MCP sowie Export konsistent.

Referenz: [Bilder und Assets](../konzept/03-content-und-assets.md#bilder-und-assets)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M8.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M8.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M7; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Asset-Speicherstrategie und Backupkopplung (O-004), erlaubte Bildtypen und Grenzen (O-016) sowie Upload-, Referenz- und Bereinigungs-UX.
  - Prüfen: produktiven Editor, Browser-/MCP-/Markdown-/PDF-Ausgabepfade, reale Deploymenttopologie und Datenmengen gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M8-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M8.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M8.1 – Daten- und Anwendungsmodell

- [ ] **M8.1 abschließen**

  - [ ] **M8.1-T1 – Asset-Speichermodell entscheiden**
    - Umfang: SQL-Metadaten plus Binärspeicheroptionen, Immutable-Semantik, Hash-Deduplizierung, Historienbezug und Backup bewerten.
    - Prüfen: Größenordnung, Transaktionsgrenzen, lokale Entwicklung, Intranetdeployment und spätere Mehrinstanzfähigkeit.
    - Nicht enthalten: Schema oder produktive Implementierung.
    - Abnahme: O-004 wird aus den offenen Fragen entfernt; Zielmodell und verworfene Alternativen stehen im Assetkonzept.

  - [ ] **M8.1-T2 – Asset-Domain- und Application-Contracts einführen**
    - Umfang: `AssetId`, Revision/Hash, MIME-Type, Originalname, Größe, Erstellungsmetadaten und benötigte Ports modellieren.
    - Regeln: immutable Binärinhalt, stabile Referenz und keine Web-/MCP-Abhängigkeit im Core.
    - Tests: Identitäten, Metadatenvalidierung und Ergebnis-/Fehlerverträge.
    - Abnahme: Asset-Use-Cases sind transport- und speicherneutral definiert.

  - [ ] **M8.1-T3 – SQL-Schema und Migration für Assetmetadaten implementieren**
    - Umfang: Tabellen, Schlüssel, Hash-/Identitätsconstraints und Snapshot-/Contentreferenzen gemäß M0-Entscheidung.
    - Prüfen: Up-Migration, leere und bestehende Datenbank, Constraints und Checksumme.
    - Dokumentation: Datenmodell und Migrationen auf den Ist-Stand bringen.
    - Abnahme: Schema ist reproduzierbar migrierbar und verhindert inkonsistente Referenzen.

## M8.2 – Speicherung und Deduplizierung

- [ ] **M8.2 abschließen**

  - [ ] **M8.2-T1 – Immutable Asset-Speicherung implementieren**
    - Umfang: Binärdaten und Metadaten atomar gemäß gewähltem Speichermodell anlegen und lesen.
    - Prüfen: Teilfehler, Wiederholung, parallele Uploads und verwaiste Zwischendaten.
    - Tests: Repository-/Integrationstests mit realen Binärdaten.
    - Abnahme: ein persistiertes Asset wird nicht in-place verändert.

  - [ ] **M8.2-T2 – Hash-Deduplizierung implementieren**
    - Umfang: tatsächlichen Inhalt hashen, identische Uploads erkennen und stabile Referenzen zurückgeben.
    - Prüfen: gleicher Inhalt mit anderem Namen, Hashkonkurrenz, leerer Inhalt und große Datei innerhalb Limits.
    - Tests: Parallelität und DB-Constraint als letzte Konsistenzgrenze.
    - Abnahme: identische Binärinhalte werden nicht mehrfach gespeichert.

## M8.3 – Upload und Auslieferung

- [ ] **M8.3 abschließen**

  - [ ] **M8.3-T1 – Kontrollierten Browserupload bereitstellen**
    - Umfang: zweckgebundener Web-Endpunkt, Streaming/Größenlimit, Metadatenprüfung und Übergabe an Asset-Application-Service.
    - Regeln: keine allgemeine REST-API, keine Data-URLs und keine direkten Speicherzugriffe aus dem Endpoint.
    - Tests: Erfolg, Abbruch, Übergröße, falscher Typ, doppelte Datei und Fehlerbereinigung.
    - Abnahme: Browser kann ein Asset sicher anlegen und erhält eine stabile Referenz.

  - [ ] **M8.3-T2 – Kontrollierte Asset-Auslieferung bereitstellen**
    - Umfang: stabile URL/Endpoint, korrekter MIME-Type, sichere Dateinamenbehandlung, Cachingstrategie und Not Found.
    - Prüfen: historische Referenz, ungültige ID, Range/Streaming nur bei echtem Bedarf und kein Pfadzugriff.
    - Tests: HTTP-Integration und Browserdarstellung.
    - Abnahme: Browser und Exporte können dasselbe Asset zuverlässig auflösen.

## M8.4 – Editorintegration

- [ ] **M8.4 abschließen**

  - [ ] **M8.4-T1 – Bild-Upload im Rich-Text-Editor integrieren**
    - Umfang: Dateiauswahl, Drag-and-drop und Einfügen aus Zwischenablage über den kontrollierten Uploadpfad; erst nach erfolgreicher serverseitiger Anlage setzt der vorhandene Milkdown-Hook die zurückgegebene interne Assetreferenz ein.
    - UX: Busy, Erfolg, Fehler, Wiederholung und Abbruch; keine eingebetteten Data-URLs.
    - Tests: alle drei Eingabewege und Editorwechsel während Upload.
    - Abnahme: erfolgreicher Upload erzeugt eine stabile Markdown-Assetreferenz; Milkdown lädt weder externe Bild-URLs noch Data-URLs und umgeht den Server-Endpunkt nicht.

  - [ ] **M8.4-T2 – Markdown-Bildreferenzen roundtrip-sicher machen**
    - Umfang: Editorimport/-export, Rendering und Quellmodus für das festgelegte Referenzformat.
    - Prüfen: Alt-Text, Titel, Sonderzeichen, mehrfach verwendetes Asset und fehlende Referenz.
    - Tests: Golden Master plus wiederholter WYSIWYG-/Markdown-Roundtrip.
    - Abnahme: Bearbeiten verändert Assetidentität oder Bildsemantik nicht unbemerkt.

## M8.5 – Lebenszyklus und Adapter

- [ ] **M8.5 abschließen**

  - [ ] **M8.5-T1 – Assetauflösung über alle Ausgabekanäle vereinheitlichen**
    - Umfang: zentraler Resolver für Browser, MCP, Markdown-Export und PDF; spätere Adapter bleiben anschließbar.
    - Prüfen: Current, Working, historischer Snapshot, Release und fehlendes Asset.
    - Tests: fachlich gleiche Referenzauflösung je Kanal.
    - Abnahme: kein Adapter implementiert eigene Pfad- oder Speicherlogik.

  - [ ] **M8.5-T2 – Referenzierte, historische und verwaiste Assets behandeln**
    - Umfang: Referenzprüfung und konservative Lebenszyklusregeln; keine automatische Löschung ohne belegte Sicherheit.
    - Prüfen: Working Transaction verworfen, Snapshot historisch, Release referenziert und nie verwendeter Upload.
    - Abnahme: historische Inhalte bleiben reproduzierbar; potenzielle Bereinigung ist getrennt und sicher.

## M8.6 – Inhalts- und Ressourcengrenzen

- [ ] **M8.6 abschließen**

  - [ ] **M8.6-T1 – Assetgrenzen und Schadinhaltsschutz absichern**
    - Voraussetzung: O-016 zu erlaubten Typen, Dateigröße und Pixelzahl ist durch den Benutzer entschieden.
    - Umfang: erlaubte MIME-/Bildtypen, Magic Bytes, Maximalgröße, Bilddimensionen, Decodierbarkeit und Ressourcenlimits.
    - Prüfen: umbenannte Dateien, beschädigte Bilder, Dekompressionsbomben und aktive Inhalte.
    - Tests: positive Formate und repräsentative Ablehnungsfälle.
    - Abnahme: nur definierte Bildinhalte gelangen in den dauerhaften Assetspeicher.

## Milestone-Abnahme

- Bilder funktionieren über Upload, Editor, Browser, MCP, Markdown- und PDF-Export mit stabilen Referenzen.
- Binärinhalte sind immutable und dedupliziert.
- Historische Snapshots und Releases verlieren ihre Assets nicht.
- Grenzen und Fehlerfälle sind serverseitig durchgesetzt.

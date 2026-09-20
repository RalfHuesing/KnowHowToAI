# M0 – Komponenten- und Architekturentscheidungen

[Roadmap-Index](../../Roadmap.md)

- [x] **M0 abschließen**

Ziel: Technische Risiken und produktprägende Fremdkomponenten sind vor der eigentlichen Webimplementierung anhand realistischer Anforderungen entschieden.

Referenzen: [Komponentenstrategie](../../konzept/02-bedienkonzept-und-ui.md#komponentenstrategie), [Offene Fragen](../../konzept/07-entscheidungen-und-offene-fragen.md), [Ein Prozess und ein Port](../../konzept/05-architektur-api-und-mcp.md#ein-prozess-und-ein-port)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## Verbindliches Spikeverfahren

Für M0.2 und M0.3 gilt zusätzlich:

1. Wegwerfcode liegt ausschließlich unter `temp/webfrontend-spikes/<Task-ID>/`. Das Verzeichnis ist bereits per `.gitignore` ausgeschlossen. Der Agent verändert dafür weder die Solution noch zentrale Paketdateien oder Produktionsprojekte.
2. Bei Fremdkomponenten wird die am Ausführungstag aktuelle stabile Version verwendet und mit Prüfdatum, Paketquelle, Quellrepository und Lizenz festgehalten. Preview-, Beta- und Release-Candidate-Versionen sind ausgeschlossen; eine ausdrücklich als Beta bezeichnete Teilfunktion wird als Produktrisiko bewertet.
3. Der Agent verwendet nur die im Task genannte Kandidatenmenge. Er beginnt keine allgemeine Marktanalyse und ergänzt keine weiteren Bibliotheken. Ein Kandidat darf nach einem belegten Knock-out abgebrochen werden; die übrigen Knock-out-Kriterien werden dann als „nicht mehr geprüft“ markiert.
4. Jeder Spike verwendet dieselben taskinternen Fixtures für alle Kandidaten. Befehle, Fixture, aufgelöste Versionen, Messergebnisse, erfüllte und nicht erfüllte Kriterien sowie verworfene Kandidaten werden im fachlich zuständigen Konzept dokumentiert. Die Fassung der Roadmap schreibt diese Versionsnummern nicht für Folgeumsetzungen vor.
5. Direkte und transitive Lizenzen werden gegen M0.1-T2 geprüft. Cloud-, Telemetrie- und CDN-Zwang, kostenpflichtige Funktionen, nicht redistribuierbare Bestandteile sowie ein notwendiger Paketfork sind Knock-outs.
6. Aus Spike-Code wird kein Produktionscode übernommen. Nach dem dokumentierten Ergebnis wird nur das konkrete Spikeverzeichnis entfernt. Produktive Pakete werden erst im zuständigen Umsetzungstask aufgenommen.
7. Erfüllt kein zulässiger Kandidat alle Musskriterien, bleibt der Task offen. Der Agent dokumentiert die genaue Lücke und fragt den Benutzer, statt Kriterien abzuschwächen oder selbst einen neuen Kandidaten einzuführen.

## M0.1 – Ausgangslage

- [x] **M0.1 abschließen**

  - [x] **M0.1-T1 – [Build-, Test- und Linter-Baseline nachweisen](tasks/M0.1-T1.md)**
  - [x] **M0.1-T2 – [MIT-Lizenz und Abhängigkeitsbaseline herstellen](tasks/M0.1-T2.md)**
## M0.2 – Host- und Routing-Spike

- [x] **M0.2 abschließen**

  - [x] **M0.2-T1 – [Blazor und MCP HTTP in einem Host validieren](tasks/M0.2-T1.md)**
  - [x] **M0.2-T2 – [Routing- und Transportmatrix verifizieren](tasks/M0.2-T2.md)**
## M0.3 – UI-Komponenten

- [x] **M0.3 abschließen**

  - [x] **M0.3-T1 – [UI-Komponentenbasis auswählen](tasks/M0.3-T1.md)**
  - [x] **M0.3-T2 – [Knowledge-Tree-Komponente auswählen](tasks/M0.3-T2.md)**
  - [x] **M0.3-T3 – [Markdown-fähigen Rich-Text-Editor auswählen](tasks/M0.3-T3.md)**
  - [x] **M0.3-T4 – [Web-Komponenten- und Browser-Testwerkzeuge auswählen](tasks/M0.3-T4.md)**
## Milestone-Abnahme

- O-001 bis O-003 und O-015 sind geschlossen; die niedrig priorisierte Assetentscheidung O-004 bleibt bis M8 offen.
- Jede gewählte direkte und transitive Abhängigkeit ist kostenlos nutzbar, mit der MIT-Distribution vereinbar und besitzt eine dokumentierte Lizenz-, Pflicht- und Wartungsbewertung.
- Kein Kernmilestone M1 bis M6 hängt von einer unbekannten Kernkomponente oder unbestätigten Hostannahme ab.

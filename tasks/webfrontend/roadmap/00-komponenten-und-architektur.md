# M0 – Komponenten- und Architekturentscheidungen

[Roadmap-Index](../Roadmap.md)

- [ ] **M0 abschließen**

Ziel: Technische Risiken und produktprägende Fremdkomponenten sind vor der eigentlichen Webimplementierung anhand realistischer Anforderungen entschieden.

Referenzen: [Komponentenstrategie](../konzept/02-bedienkonzept-und-ui.md#komponentenstrategie), [Offene Fragen](../konzept/07-entscheidungen-und-offene-fragen.md), [Ein Prozess und ein Port](../konzept/05-architektur-api-und-mcp.md#ein-prozess-und-ein-port)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## Verbindliches Spikeverfahren

Für M0.2 und M0.3 gilt zusätzlich:

1. Wegwerfcode liegt ausschließlich unter `temp/webfrontend-spikes/<Task-ID>/`. Das Verzeichnis ist bereits per `.gitignore` ausgeschlossen. Der Agent verändert dafür weder die Solution noch zentrale Paketdateien oder Produktionsprojekte.
2. Bei Fremdkomponenten wird die am Ausführungstag aktuelle stabile Version verwendet und mit Prüfdatum, Paketquelle, Quellrepository und Lizenz festgehalten. Preview-, Beta- und Release-Candidate-Versionen sind ausgeschlossen; eine ausdrücklich als Beta bezeichnete Teilfunktion wird als Produktrisiko bewertet.
3. Der Agent verwendet nur die im Task genannte Kandidatenmenge. Er beginnt keine allgemeine Marktanalyse und ergänzt keine weiteren Bibliotheken. Ein Kandidat darf nach einem belegten Knock-out abgebrochen werden; die übrigen Knock-out-Kriterien werden dann als „nicht mehr geprüft“ markiert.
4. Jeder Spike verwendet dieselben taskinternen Fixtures für alle Kandidaten. Befehle, Fixture, Versionen, Messergebnisse, erfüllte und nicht erfüllte Kriterien sowie verworfene Kandidaten werden im fachlich zuständigen Konzept dokumentiert.
5. Direkte und transitive Lizenzen werden gegen M0.1-T2 geprüft. Cloud-, Telemetrie- und CDN-Zwang, kostenpflichtige Funktionen, nicht redistribuierbare Bestandteile sowie ein notwendiger Paketfork sind Knock-outs.
6. Aus Spike-Code wird kein Produktionscode übernommen. Nach dem dokumentierten Ergebnis wird nur das konkrete Spikeverzeichnis entfernt. Produktive Pakete werden erst im zuständigen Umsetzungstask aufgenommen.
7. Erfüllt kein zulässiger Kandidat alle Musskriterien, bleibt der Task offen. Der Agent dokumentiert die genaue Lücke und fragt den Benutzer, statt Kriterien abzuschwächen oder selbst einen neuen Kandidaten einzuführen.

## M0.1 – Ausgangslage

- [ ] **M0.1 abschließen**

  - [ ] **M0.1-T1 – Build-, Test- und Linter-Baseline nachweisen**
    - Umfang: aktuellen Build, FastTests, erforderliche Integrationstests und AiNetLinter gemäß Projektregeln ausführen.
    - Ergebnis: reproduzierbare Befehle, Laufzeiten und bekannte Abweichungen festhalten; keine Webänderung.
    - Abnahme: Baseline ist grün oder jede bestehende Abweichung ist vor weiterer Arbeit geklärt und separat behoben.
    - Abschluss: betroffene Ist-Dokumentation nur bei tatsächlicher Änderung aktualisieren; Task und Parentstatus committen.

  - [ ] **M0.1-T2 – MIT-Lizenz und Abhängigkeitsbaseline herstellen**
    - Ausgangslage: Die MIT-`LICENSE` mit `Copyright (c) 2026 Ralf Hüsing` ist im Repository-Root vorhanden.
    - Umfang: vorhandene `LICENSE` auf unveränderten MIT-Text prüfen; vollständigen direkten und transitiven Abhängigkeitsgraph aller Solution-Projekte einschließlich Build- und Testwerkzeugen ermitteln.
    - Ergebnis: reproduzierbares Inventar mit Paket, Version, Quelle, Lizenz und einzuhaltenden Copyright-/Lizenz-/NOTICE-Pflichten in `THIRD-PARTY-NOTICES.md`; erforderliche Originalhinweise beilegen.
    - Abnahme: keine Abhängigkeit ist kostenpflichtig, lizenzseitig ungeklärt oder mit der MIT-Distribution unvereinbar; Abweichungen werden ersetzt oder vor Fortsetzung dem Benutzer vorgelegt.
    - Abschluss: Lizenz-/Inventardateien und die dauerhaft erforderliche Aktualisierungsanweisung dokumentieren und atomar committen.

## M0.2 – Host- und Routing-Spike

- [ ] **M0.2 abschließen**

  - [ ] **M0.2-T1 – Blazor und MCP HTTP in einem Host validieren**
    - Umfang: isolierter Spike für `WebApplication`, Blazor Interactive Server und MCP Streamable HTTP auf einem Kestrel-Port.
    - Prüfen: Endpoint Routing, Blazor-Circuit, MCP-Streaming, DI-Scopes, Start/Stop und Route-Kollisionen.
    - Nicht enthalten: produktive Hostmigration, REST/OpenAPI, UI-Design oder STDIO-Entfernung.
    - Abnahme: technische Machbarkeit und notwendige Hostleitplanken sind belegt.
    - Abschluss: Ergebnis im [Architekturkonzept](../konzept/05-architektur-api-und-mcp.md) festhalten; Spike-Code wird nicht als Produktionscode committed und die produktive Umsetzung beginnt erst in M1.

  - [ ] **M0.2-T2 – Routing- und Proxyannahmen verifizieren**
    - Umfang: `/`, `/mcp` und reserviertes `/api` mit realistischem Reverse-Proxy-Verhalten prüfen; spätere Download-/Assetpräfixe gegen die Fallbackregeln konzeptionell abgleichen.
    - Prüfen: WebSocket-Upgrade, Streaming, Timeouts, Fallback-Routing und ein gemeinsamer Origin.
    - Nicht enthalten: produktives Deployment oder Security-Härtung.
    - Abnahme: der gemeinsame Port ist bestätigt. Ist das nicht möglich, bleibt der Task offen und der Agent fragt den Benutzer, statt selbst auf mehrere Ports auszuweichen.

## M0.3 – UI-Komponenten

- [ ] **M0.3 abschließen**

  - [ ] **M0.3-T1 – UI-Komponentenbasis auswählen**
    - Kandidaten und Reihenfolge: zuerst native Blazor-/HTML-/CSS-Basis. Erfüllt sie alle Musskriterien ohne wiederholte komplexe Eigenimplementierung, wird „keine allgemeine Komponentenbibliothek“ gewählt und der Paketvergleich endet. Nur andernfalls werden zusätzlich die aktuellen stabilen Versionen von `Microsoft.FluentUI.AspNetCore.Components` und `MudBlazor` mit demselben Fixture geprüft; andere Suites sind ausgeschlossen.
    - Fixture: eine kleine Interactive-Server-Seite mit Shellnavigation, beschriftetem Formular samt Validierung, modalem Bestätigungsdialog, kleiner Tabelle, Inlinehinweis und Toast sowie zentral überschreibbaren Farb-, Abstands- und Fokus-Tokens. Der Dialog muss per Tastatur geöffnet und geschlossen werden, Fokus einfangen und zum Auslöser zurückgeben.
    - Musskriterien: .NET 10 und Interactive Server; vollständig self-hosted ohne Cloud/CDN; O-021 für Semantik, Tastatur, sichtbaren Fokus, Kontrast und 200-%-Zoom; deterministisch per bUnit-artigem Komponentenfixture und Headless Chrome testbar; Light Theme mit den M2-Tokens anpassbar; keine erzwungene JavaScript-Buildtoolchain.
    - Messung: direkter und transitiver Paketgraph samt Lizenzpflichten, erforderliche Services/Assets/JS-Interop, Release-Publishgröße von `wwwroot` und Gesamtpublish jeweils gegen das native Fixture sowie notwendige Initialisierungs- und Themeeingriffe. Rohwerte werden dokumentiert; es gibt keinen erfundenen Maximalwert.
    - Auswahlregel: Nach Musskriterien gewinnt die native Basis. Eine Bibliothek wird nur gewählt, wenn sie einen im Fixture belegten, in M2 mehrfach benötigten Nutzen liefert und ihre zusätzliche Abhängigkeit sowie Betriebsoberfläche kleiner sind als die dadurch vermiedene Eigenimplementierung. Bei Gleichstand gilt: weniger Produktionsabhängigkeiten, dann weniger JS/Assets, dann kleinere Publishdifferenz.
    - Nicht enthalten: Knowledge Tree und Rich-Text-Editor; diese werden separat entschieden.
    - Ergebnisort: Auswahl und Integrationsleitplanken in `konzept/02-bedienkonzept-und-ui.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-001 aus `konzept/07-entscheidungen-und-offene-fragen.md` entfernen.
    - Abnahme: Entscheidung, Messwerte, Lizenzprüfung und verworfene Alternativen sind dokumentiert; Browserunterstützung erfüllt O-013.

  - [ ] **M0.3-T2 – Knowledge-Tree-Komponente auswählen**
    - Kandidaten: (1) eine schmale native Blazor-/HTML-Lösung, (2) genau die Tree-Komponente der in M0.3-T1 gewählten allgemeinen UI-Bibliothek, falls dort eine enthalten ist, und (3) die aktuelle stabile `Radzen.Blazor`-Tree-Komponente. Kandidat 2 entfällt bei nativer UI-Basis oder fehlender Tree-Komponente; weitere Tree-Pakete werden nicht gesucht.
    - Gemeinsamer Datenadapter: opake Cursor, Root- und Children-Seiten zu je 100 Einträgen, stabile `NodeId`, `HasChildren`, kontrollierter Expand-/Selection-State und Instrumentierung jedes Datenabrufs. Die synthetische Quelle bildet 100.000 Nodes, mindestens zehn Ebenen und einen Parent mit mehr als 1.000 direkten Children ab, hält aber nie den Gesamtbaum im UI-Speicher.
    - Musskriterien: echtes Load-on-expand; serverseitiges Root-/Children-Paging ohne interne Web-API; begrenzte DOM- und Speichernutzung durch Paging plus Virtualisierung oder gleichwertig begrenztes Rendering; stabiler Zustand nach Seitenwechsel und Re-render; zugängliche Tree-Semantik, Pfeil-/Home-/End-/Enter-/Leertastenbedienung und sichtbarer Fokus; Drag-and-drop-Zielvorschau für `Parent`, `Before` und `After`; dieselben drei Moves vollständig ohne Drag-and-drop per Tastatur/Aktionsmenü; Interactive Server und Headless-Chrome-Testbarkeit.
    - Knock-outs: Vorabladen des Gesamtbaums; nur clientseitiges Paging; Verlust von Auswahl/Expand-State beim Nachladen; keine eindeutige Move-Zielposition; notwendiger Fork, DOM-Patch oder Zugriff auf nichtöffentliche Komponenteninternas. Ein kleiner öffentlicher Adapter und eigene Darstellung der Tastaturalternative sind zulässig und werden im Aufwand ausgewiesen.
    - Auswahlregel: Nach Musskriterien gewinnt der Kandidat mit dem kleinsten produktiven Paket-/JS-/CSS-Footprint und der geringsten komponentenspezifischen Adapterfläche. Eine allgemeine Suite wird nicht allein für den Tree gewählt, wenn die native Lösung gleichwertig ist.
    - Ergebnisort: Entscheidung, Datenadaptervertrag, Zustandsbesitz, bekannte Grenzen und spätere Integrationsschritte in `konzept/02-bedienkonzept-und-ui.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-003 entfernen.
    - Abnahme: Instrumentierte Tests belegen, dass Expand und Paging nur die angeforderte Seite laden; alle Musskriterien und die Grenzen der gewählten Lösung sind dokumentiert.

  - [ ] **M0.3-T3 – Markdown-fähigen Rich-Text-Editor auswählen**
    - Kandidaten: aktuelle stabile Versionen von Milkdown und Tiptap mit dessen offizieller Markdown-Erweiterung; beide werden mit demselben Fixture geprüft, Milkdown zuerst. Die weiterhin als Beta dokumentierte Tiptap-Markdown-Erweiterung ist als Produktrisiko auszuweisen. TOAST UI Editor ist ausgeschlossen, weil das Upstream-Repository seit 2026-09-02 archiviert und schreibgeschützt ist; weitere Editoren werden nicht gesucht.
    - Golden-Master-Fixture: Absätze; fett/kursiv/durchgestrichen; Links; geordnete, ungeordnete und verschachtelte Listen; Tabellen; Inline-Code; fenced Code mit Sprachkennung; Blockquotes; Unicode; absichtlich enthaltenes Raw HTML; erlaubte und verbotene Linkziele; externe Bildsyntax; Text-, Browser-HTML- und Office-HTML-Paste; Inhalt nahe der fachlichen 4-KiB-Warngrenze.
    - Roundtrip: jeden zulässigen Golden Master fünfmal `Markdown -> Editor -> Markdown` durchlaufen lassen und anschließend mit Markdig als semantische Struktur vergleichen. Unterschiedliche, aber semantisch gleichwertige Markdownschreibweisen sind zulässig; verlorene oder neu erzeugte Struktur ist ein Knock-out. Das serverseitig verbotene Raw HTML und Headings müssen als klarer Validierungsfehler erhalten beziehungsweise abgelehnt werden und dürfen nicht stillschweigend verschwinden.
    - Sicherheits- und Integrationsprüfung: Heading-Befehle sind nicht verfügbar; Links folgen exakt O-020; externe Bilder/Assets lösen im Browser keinen Request aus; Paste-Reduktionen erzeugen einen sichtbaren Hinweis; der ursprüngliche Editorinhalt bleibt bei Serverablehnung erhalten; Image-Upload ist deaktiviert, besitzt aber einen später anschließbaren internen Hook; Dirty-State, Fokus, Dispose, Reconnect und Nodewechsel sind kontrollierbar.
    - Musskriterien: Markdown bleibt einziges kanonisches Speicherformat; kein HTML-/Editor-JSON als verstecktes Zweitformat; self-hosted Assets; schmale JS-Isolation für Blazor Interactive Server; automatisierbar in Headless Chrome; keine Cloudfunktion und kein Pro-/Bezahlmodul für Mussfunktionen.
    - Auswahlregel: Ein stabiler, vollständig bestehender Markdownpfad gewinnt vor einer Beta-API. Danach gelten geringere semantische Anpassung, kleinere transitive JavaScript-Lieferkette und weniger eigener zustandsbehafteter JS-Code. Besteht kein Kandidat den Roundtrip, wird nichts ausgewählt.
    - Ergebnisort: Entscheidung, erlaubte Editorfunktionen, Interop-/Lifecycle-Vertrag, Golden Master und bekannte Grenzen in `konzept/03-content-und-editor.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-002 entfernen.
    - Abnahme: alle erlaubten Strukturen bestehen den fünffachen semantischen Roundtrip und sämtliche Sicherheits-/Lifecycle-Kriterien sind automatisiert belegt.

  - [ ] **M0.3-T4 – Web-Komponenten- und Browser-Testwerkzeuge auswählen**
    - Feste Werkzeuge: aktuelle stabile bUnit-Version mit xUnit v3 für Razor-Komponenten und aktuelle stabile Microsoft-Playwright-.NET-Version mit dem installierten Google-Chrome-Stable-Kanal für Browserabläufe. Es werden keine alternativen Testframeworks verglichen.
    - Fixture bUnit: eine zustandsbehaftete Razor-Komponente rendert, DI beziehen, Parameter ändern, Event auslösen, Fehlerzustand prüfen und dünnes JS-Interop mocken. Der Test läuft mit `dotnet test` unter `net10.0` und xUnit v3.
    - Fixture Playwright: einen realen temporären ASP.NET-Core-Host auf dynamischem Loopback-Port starten, Interactive-Server-Verbindung abwarten, über Rollen/Labels/Test-IDs bedienen, Web-first Assertions und einen stabil maskierten Screenshot ausführen sowie sauber beenden. Start ausschließlich mit `Channel = "chrome"` und `Headless = true`; kein Chromium-Fallback, keine Browsermatrix und kein sichtbarer Browserstart.
    - Chrome-Voraussetzung: Version protokollieren und einen nichtinteraktiven Installations-/Prüfbefehl für lokale und CI-Ausführung dokumentieren. Fehlt Chrome, wird nicht still auf einen anderen Browser gewechselt. Die Browserinstallation ist keine Anwendungslaufzeitabhängigkeit.
    - Vitest-Entscheidung: nur ergänzen, wenn die in M0.3-T2/T3 gewählte Integration eigenen zustandsbehafteten JavaScript-/TypeScript-Code mit Verzweigungen, Transformationen oder Retry-/Lifecyclelogik erfordert. Bei ausschließlich dünnen Interop-Aufrufen wird ausdrücklich „nicht erforderlich“ dokumentiert und keine Node-Testtoolchain angelegt. Falls erforderlich, muss ein Fixture die reine JS-Logik ohne Browser testen und der feste CI-Befehl dokumentiert werden.
    - Prüfen: Interaktion, JS-Interop, Screenshots, Parallel-/Wiederholungslauf, keine festen Wartezeiten, lokale/CI-Befehle, Lizenzgraph und Wartungsstatus. Browser-E2E bleibt auf Integrationsrisiken begrenzt; fachliche Varianten gehören in Core- und Komponententests.
    - Nicht enthalten: Testprojekte oder produktive Testfälle; diese folgen in M2.
    - Ergebnisort: Versionen, Projektzuordnung, feste Befehle, Locator-/Screenshot-Regeln und Vitest-Entscheidung in `konzept/08-projektstruktur-und-codekonventionen.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-015 entfernen.
    - Abnahme: beide Pflichtfixtures laufen zweimal hintereinander grün und hinterlassen keinen Hostprozess; O-015 ist geschlossen.

## Milestone-Abnahme

- O-001 bis O-003 und O-015 sind geschlossen; die niedrig priorisierte Assetentscheidung O-004 bleibt bis M8 offen.
- Jede gewählte direkte und transitive Abhängigkeit ist kostenlos nutzbar, mit der MIT-Distribution vereinbar und besitzt eine dokumentierte Lizenz-, Pflicht- und Wartungsbewertung.
- Kein Kernmilestone M1 bis M6 hängt von einer unbekannten Kernkomponente oder unbestätigten Hostannahme ab.

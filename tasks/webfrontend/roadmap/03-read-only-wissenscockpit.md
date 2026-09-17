# M3 – Read-only Wissenscockpit

[Roadmap-Index](../Roadmap.md)

- [ ] **M3 abschließen**

Abhängigkeit: [M2](02-designsystem-und-shell.md)

Ziel: Menschen können den gesamten vorhandenen Wissensstand, seine Struktur, Rollenauflösung und Historie ohne MCP-Client verstehen.

Referenzen: [Dashboard](../konzept/02-bedienkonzept-und-ui.md#dashboard), [Wissensbaum](../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [Historie und Releases](../konzept/02-bedienkonzept-und-ui.md#historie-und-releases), [Blazor-interne Aufrufe](../konzept/05-architektur-api-und-mcp.md#blazor-interne-aufrufe)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M3.1 – Web-Lesegrenze

- [ ] **M3.1 abschließen**

  - [ ] **M3.1-T1 – Transportneutrale Lese-Use-Cases für die UI anbinden**
    - Umfang: UI-nahe Facades/ViewModels und Mapper für Navigation, Rollen, Search, Historie und Read Context strukturieren.
    - Regel: Komponenten rufen Application Services direkt auf; keine REST-Schicht und keine Domain-Typen im Rendering.
    - Prüfen: Fehler, Warnungen, Cursor und `ChangeVersion` bleiben vollständig erhalten.
    - Abnahme: nachfolgende Seiten konsumieren eine konsistente Web-Grenze ohne Fachlogikduplikate.

## M3.2 – Dashboard

- [ ] **M3.2 abschließen**

  - [ ] **M3.2-T1 – Wissensdashboard implementieren**
    - Umfang: Current Snapshot, letzter Release, alle offenen Transactions, Current-Qualität über alle Rollen und zuletzt geänderte Nodes als kompakte Einstiegsübersicht.
    - Semantik: harte Fehler gehören zu offenen Transactions; letzte Nodeänderungen stammen aus dem Diff zum direkten committed Vorgänger; ohne Vorgänger bleibt diese Liste leer.
    - Interaktion: Karten und Listen verlinken auf vorhandene Detailkontexte; keine Mutation.
    - Zustände: Laden, leer, partieller Fehler und große Mengen.
    - Tests: ViewModel-/Komponententests und Browser-Smoke.
    - Abnahme: Systemzustand ist nach dem Öffnen ohne Agent verständlich.

## M3.3 – Hierarchienavigation

- [ ] **M3.3 abschließen**

  - [ ] **M3.3-T1 – Lazy-Loading-Datenadapter für den Wissensbaum implementieren**
    - Umfang: Root-/Children-Paging, opake Cursor, stabile Node-Identitäten, Expand-State und Abbruch veralteter Requests.
    - Prüfen: tiefe Hierarchie, breite Geschwisterlisten, Kontextwechsel und Fehler einzelner Zweige.
    - Tests: Adapter- und Pagingtests mit realistischen Grenzfällen.
    - Abnahme: Baumdaten werden nie ungepaginiert vollständig geladen.

  - [ ] **M3.3-T2 – Read-only Knowledge Tree und Breadcrumbs implementieren**
    - Umfang: ausgewählte Tree-Komponente, Expand/Collapse, virtuelle Darstellung, Auswahl, Breadcrumbs und Tastaturnavigation.
    - Zustände: selektierter, geladener, teilweise geladener, leerer und fehlerhafter Node.
    - Nicht enthalten: Drag-and-drop, Erstellen, Löschen oder Sortieren.
    - Tests: Komponenten- und Browserfälle für Tiefe, Breite, Fokus und Auswahl.
    - Abnahme: beliebige vorhandene Nodes sind performant auffindbar und auswählbar.

## M3.4 – Rolle, Lesekontext und Node

- [ ] **M3.4 abschließen**

  - [ ] **M3.4-T1 – Globalen Rollen- und Lesekontext-Selektor implementieren**
    - Voraussetzung: O-008 zum Verhalten ohne gewählte Rolle ist durch den Benutzer entschieden.
    - Umfang: Rolle sowie Current Snapshot, historischer Snapshot, Release oder vorhandene Working Transaction auswählen.
    - Prüfen: Kontext ist global sichtbar, URL-/Navigationsverhalten ist definiert und ungültige Kombinationen werden erklärt.
    - Tests: Kontextwechsel, leere Rollenliste, nicht mehr vorhandener Kontext und Reconnect.
    - Abnahme: jede Leseansicht verwendet denselben expliziten Kontext.

  - [ ] **M3.4-T2 – Read-only Node-Detailansicht implementieren**
    - Voraussetzung: O-020 zur sicheren Markdown-/HTML- und Fremdressourcen-Policy ist geschlossen.
    - Umfang: Titel, Description, Position, Rolle, aufgelöster Content, Fallback/Provenienz, Revision und Freshness.
    - Darstellung: gerendertes Markdown plus klar getrennte Metadaten; keine Bearbeitungscontrols.
    - Tests: eigener Content, Fallback, leerer Content, stale/derived und fehlender Node.
    - Abnahme: der fachlich wirksame Inhalt und seine Herkunft sind eindeutig erkennbar.

## M3.5 – Suche

- [ ] **M3.5 abschließen**

  - [ ] **M3.5-T1 – Paginierte Wissenssuche implementieren**
    - Umfang: Suchtext, Rolle, Read Context, Cursor, Treffer-Snippet, Breadcrumb und Navigation zum Node.
    - Prüfen: normale TODO-Texte verhalten sich wie jeder andere Suchinhalt.
    - Zustände: kein Treffer, weitere Seite, Kontextwechsel, ungültiger Cursor und abgebrochene Suche.
    - Tests: Application-/Web-Mapping, Komponenten und Browser-Smoke.
    - Abnahme: Benutzer findet Content ohne Kenntnis der Baumposition.

  - [ ] **M3.5-T2 – Wissensfilter implementieren**
    - Umfang: Filter für Rolle, Availability, Freshness und Findings auf der paginierten Such-/Filtertrefferliste; Trefferauswahl fokussiert den Node im unveränderten Knowledge Tree.
    - Semantik: Werte derselben Gruppe werden ODER-verknüpft, unterschiedliche Gruppen UND-verknüpft; kein Filter ist der Default.
    - Prüfen: Paging bleibt stabil, Filterwechsel verwirft alte Cursor und leere Ergebnisse werden erklärt.
    - Tests: Query-/Repositoryverhalten, Web-Mapping, Komponenten und Browser-Smoke.
    - Abnahme: Filtersemantik aus dem Bedienkonzept ist vollständig serverseitig paginiert umgesetzt; der Tree wird nicht clientseitig beschnitten.

## M3.6 – Historie und Releases

- [ ] **M3.6 abschließen**

  - [ ] **M3.6-T1 – Snapshot- und Releaseübersichten implementieren**
    - Umfang: paginierte Listen, Kernmetadaten, Auswahl und Navigation in den jeweiligen Read Context.
    - Prüfen: unveränderliche Stände sind klar von Working Transactions getrennt.
    - Tests: Paging, leere Historie, ungültige Auswahl und Kontextübernahme.
    - Abnahme: historische und benannte Stände sind ohne MCP zugänglich.

  - [ ] **M3.6-T2 – Snapshot-Diff-Ansicht implementieren**
    - Umfang: strukturierte Node-, Content-, Rollen- und Dependency-Änderungen zwischen unterstützten Ständen anzeigen.
    - Prüfen: Paging, große Diffs, stabile Sortierung und verständliche leere Ergebnisse.
    - Nicht enthalten: Merge oder Reapply.
    - Tests: repräsentative Diffarten und Browserdarstellung.
    - Abnahme: Benutzer kann Unterschiede fachlich nachvollziehen und die Historie eines Nodes über auf diesen Node gefilterte Snapshot-Diffs verfolgen.

## M3.7 – Markdown-Export

- [ ] **M3.7 abschließen**

  - [ ] **M3.7-T1 – Bestehenden Markdown-Teilbaumexport in der UI bereitstellen**
    - Umfang: aktueller Node, Rolle und Read Context an vorhandenen Export-Use-Case übergeben und Datei herunterladen.
    - Prüfen: Root/Teilbaum, Fallback, Dateiname, MIME-Type, Fehler und große Ausgabe.
    - Nicht enthalten: PDF oder Exportprofile.
    - Tests: Mapping, Download-Endpunkt und Browser-Smoke.
    - Abnahme: bestehende Exportfunktion ist ohne Agent nutzbar.

## M3.8 – Fachliche Parität

- [ ] **M3.8 abschließen**

  - [ ] **M3.8-T1 – UI- und MCP-Leseergebnisse gegen gemeinsame Use Cases prüfen**
    - Umfang: repräsentative Navigation, Rollenauflösung, Search, Historie, Diff und Export vergleichen.
    - Ziel: Mappingfehler erkennen; keine Bytegleichheit unterschiedlicher Transportmodelle erzwingen.
    - Abnahme: UI und MCP zeigen denselben fachlichen Zustand, dieselben Warnungen und dieselben Kontextgrenzen.

## Milestone-Abnahme

- Der vorhandene Wissensstand ist ohne MCP-Client navigierbar, suchbar, historisch einsehbar und als Markdown exportierbar.
- Rolle, Read Context, Fallback, Provenienz und Freshness sind sichtbar.
- UI und MCP verwenden dieselben Application-Use-Cases ohne duplizierte Fachlogik.

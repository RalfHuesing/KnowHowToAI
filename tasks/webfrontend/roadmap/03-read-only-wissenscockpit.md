# M3 – Read-only Wissenscockpit

[Roadmap-Index](../Roadmap.md)

- [x] **M3 abschließen**

Abhängigkeit: [M2](02-designsystem-und-shell.md)

Verbindliche M0-Basis: Der Tree ist nativ und lädt Children serverseitig mit opaken Cursors und exakt 100 Einträgen pro Seite; höchstens zehn Seiten liegen gleichzeitig im Circuit. Radzen und eine erneute Tree-Auswahl sind ausgeschlossen. Komponenten- und Browsernachweise verwenden bUnit mit xUnit v3 beziehungsweise Microsoft.Playwright .NET mit der installierten aktuellen Google-Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Menschen können den gesamten vorhandenen Wissensstand, seine Struktur, Rollenauflösung und Historie ohne MCP-Client verstehen.

Referenzen: [Dashboard](../konzept/02-bedienkonzept-und-ui.md#dashboard), [Wissensbaum](../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [Historie und Releases](../konzept/02-bedienkonzept-und-ui.md#historie-und-releases), [Blazor-interne Aufrufe](../konzept/05-architektur-api-und-mcp.md#blazor-interne-aufrufe)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M3.0 – Manuelle Planung und Konzeptschärfung

- [x] **M3.0 abschließen**
  - Durchgeführt am 2026-09-19 gemeinsam mit dem Benutzer.
  - Entschieden: O-008 (initiale Rolle: letzten gespeicherten `localStorage`-Wert verwenden, Pflichtauswahl bei fehlendem/ungültigem Eintrag), O-007 (Actor über `ICurrentUserService`-Seam, initiale Dummy-Implementierung), O-025 (mehrere gleichzeitige Clients erlaubt, keine Locks, `ChangeVersion`-Ablehnung), O-026 (keine automatische Transaction-Lebensdauer, Alter im Dashboard sichtbar, Warnbadge ab 7 Tagen), O-027 (Undo nur im Editor bis Speichern, kein globaler Undo-Stack).
  - Konzepte aktualisiert: [Bedienkonzept und UI](../konzept/02-bedienkonzept-und-ui.md), [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md), [Offene Fragen](../konzept/07-entscheidungen-und-offene-fragen.md).
  - Gate: M3.1 und folgende Arbeitspakete sind durch Implementierungsagenten ausführbar.

## M3.1 – Web-Lesegrenze

- [x] **M3.1 abschließen**

  - [x] **M3.1-T1 – Transportneutrale Lese-Use-Cases für die UI anbinden**
    - Umfang: UI-nahe Facades/ViewModels und Mapper für Navigation, Rollen, Search, Historie und Read Context strukturieren.
    - Regel: Komponenten rufen Application Services direkt auf; keine REST-Schicht und keine Domain-Typen im Rendering.
    - Prüfen: Fehler, Warnungen, Cursor und `ChangeVersion` bleiben vollständig erhalten.
    - Abnahme: nachfolgende Seiten konsumieren eine konsistente Web-Grenze ohne Fachlogikduplikate.

## M3.2 – Dashboard

- [x] **M3.2 abschließen**

  - [x] **M3.2-T1 – Wissensdashboard implementieren**
    - Umfang: Current Snapshot, letzter Release, alle offenen Transactions, Current-Qualität über alle Rollen und zuletzt geänderte Nodes als kompakte Einstiegsübersicht.
    - Semantik: harte Fehler gehören zu offenen Transactions; letzte Nodeänderungen stammen aus dem Diff zum direkten committed Vorgänger; ohne Vorgänger bleibt diese Liste leer.
    - Interaktion: Karten und Listen verlinken auf vorhandene Detailkontexte; keine Mutation.
    - Zustände: Laden, leer, partieller Fehler und große Mengen.
    - Tests: ViewModel-/Komponententests und Browser-Smoke.
    - Abnahme: Systemzustand ist nach dem Öffnen ohne Agent verständlich.

## M3.3 – Hierarchienavigation

- [x] **M3.3 abschließen**

  - [x] **M3.3-T1 – Lazy-Loading-Datenadapter für den Wissensbaum implementieren**
    - Umfang: den einzelnen Root über `NavigationService.GetRootAsync` laden; pro Expand `NavigationService.ListChildrenAsync` mit explizitem `ReadContext`, `RoleId`, `Limit: 100` und dem unverändert weitergereichten opaken Cursor aufrufen. `ChildCount > 0` bestimmt `HasChildren`; kein separater HTTP-Endpunkt und kein neuer Persistence-Port.
    - Zustand: stabile `NodeId`, Auswahl, Expand-Zustand, je Parent genau eine sichtbare Seite, `nextCursor`, Cursor-Historie, Request-Cancellation und LRU-Reihenfolge liegen im `KnowledgeTree`-Circuit-State. Die Cursor-Historie speichert nur zuvor verwendete opake Cursorstrings, keine Itemseiten. Kontextwechsel verwirft alle Tree-Seiten, Cursor-Historien und laufenden Requests; ein verspätetes Ergebnis darf den neuen Kontext nicht überschreiben.
    - Cachegrenze: maximal zehn geladene Seiten. Die elfte Anforderung entfernt die am längsten ungenutzte Seite eines nicht ausgewählten Teilbaums und schließt ihn. Gehören alle zehn Seiten zum Auswahlpfad, wird die rootnächste Seite entfernt und ihr Kind auf dem Auswahlpfad zum visuellen Root des Tree-Ausschnitts; der globale Pfad bleibt in den Breadcrumbs. Navigation zu einem höheren Breadcrumb lädt dessen Seite erneut und unterliegt derselben Cachegrenze. Die UI kündigt Schließen oder Neuzentrieren einmalig über `role=status` an.
    - Paging: „Zurück“ lädt den vorigen Cursor aus der Cursor-Historie erneut, „Weitere“ verwendet `nextCursor`; beide Aktionen ersetzen die sichtbare 100er-Seite des Parents. Seiten werden nicht zu einer wachsenden Childliste zusammengefügt. Cursor werden weder decodiert noch clientseitig erzeugt.
    - Prüfen: leerer Zustand, genau 100 und 101 Children, mehr als 1.000 direkte Children, mindestens zehn Ebenen, zehn/elf geladene Seiten, Kontextwechsel, `InvalidCursor`, `CursorExpired`, Fehler eines einzelnen Zweigs und Abbruch eines überholten Requests.
    - Tests: bUnit-Adapter-/Pagingtests instrumentieren jeden `GetRootAsync`-/`ListChildrenAsync`-Aufruf und prüfen Parent, Rolle, Read Context, `Limit = 100`, Cursor, Aufrufanzahl, Cache-Eviction und dass nie ein Vollbaum angefordert oder im ViewModel gehalten wird.
    - Abnahme: jede Datenanforderung betrifft ausschließlich Root oder eine 100er-Childseite; höchstens zehn Seiten bleiben im Circuit; Cursor-, Kontext- und Evictionverhalten sind automatisiert belegt.

  - [x] **M3.3-T2 – Read-only Knowledge Tree und Breadcrumbs implementieren**
    - Umfang: nativen `KnowledgeTree` gemäß [Bedienkonzept](../konzept/02-bedienkonzept-und-ui.md#wissensbaum) mit Expand/Collapse, seitenbegrenzt gerenderten Treeitems, Auswahl und Breadcrumbs implementieren. Keine Fremdkomponente und keine behauptete Viewport-Virtualisierung.
    - Zustände: selektierter, geladener, teilweise geladener, leerer und fehlerhafter Node.
    - Nicht enthalten: Drag-and-drop, Erstellen, Löschen oder Sortieren.
    - Semantik/Tastatur: `role=tree`, `role=treeitem`, roving `tabindex`, Ebene, Auswahl und Expandstatus; `ArrowUp/Down/Left/Right`, `Home`, `End`, `Enter` und Leertaste. Auswahl aktualisiert `/knowledge/{NodeId}` und bleibt nach Re-render/Refresh aus Route und Query rekonstruierbar.
    - Tests: bUnit-Komponentenfälle und headless Playwright-Browserfälle für Tiefe, 100/101/breite Childrenmengen, Seite wechseln, Fokus, Auswahl, Cache-Eviction, Neuzentrierung und Breadcrumb-Rücknavigation. Keine festen Wartezeiten.
    - Abnahme: jeder im Tree, über Suche oder stabile `NodeId` erreichbare Node ist auswählbar; DOM und Circuit enthalten keinen Vollbaum und höchstens die dokumentierten zehn Seiten.

## M3.4 – Rolle, Lesekontext und Node

- [x] **M3.4 abschließen**

  - [x] **M3.4-T1 – Globalen Rollen- und Lesekontext-Selektor implementieren**
    - Umfang: Rolle sowie Current Snapshot, historischer Snapshot, Release oder vorhandene Working Transaction auswählen.
    - Rollenwahl (O-008 entschieden): letzte Rolle aus `localStorage` (Schlüssel `knowhowtoai.lastRoleId`) vorladen und gegen Rollenliste prüfen; fehlt der Eintrag oder existiert die Rolle nicht mehr, erscheint ein modaler Pflichtauswahl-Selektor; es gibt keine stille Standardrolle.
    - Prüfen: Kontext ist global sichtbar, URL-/Navigationsverhalten ist definiert und ungültige Kombinationen werden erklärt.
    - Tests: Kontextwechsel, leere Rollenliste, nicht mehr vorhandener Kontext und Reconnect.
    - Abnahme: jede Leseansicht verwendet denselben expliziten Kontext.

  - [x] **M3.4-T2 – Read-only Node-Detailansicht implementieren**
    - Umfang: Titel, Description, Position, Rolle, aufgelöster Content, Fallback/Provenienz, Revision und Freshness.
    - Darstellung: gerendertes Markdown plus klar getrennte Metadaten; keine Bearbeitungscontrols, keine Ausführung von Raw HTML und kein Nachladen externer oder lokaler Ressourcen gemäß O-020.
    - Tests: eigener Content, Fallback, leerer Content, stale/derived und fehlender Node.
    - Abnahme: der fachlich wirksame Inhalt und seine Herkunft sind eindeutig erkennbar.

## M3.5 – Suche

- [x] **M3.5 abschließen**

  - [x] **M3.5-T1 – Paginierte Wissenssuche implementieren**
    - Umfang: Suchtext, Rolle, Read Context, Cursor, Treffer-Snippet, Breadcrumb und Navigation zum Node.
    - Prüfen: normale TODO-Texte verhalten sich wie jeder andere Suchinhalt.
    - Zustände: kein Treffer, weitere Seite, Kontextwechsel, ungültiger Cursor und abgebrochene Suche.
    - Tests: Application-/Web-Mapping, Komponenten und Browser-Smoke.
    - Abnahme: Benutzer findet Content ohne Kenntnis der Baumposition.

  - [x] **M3.5-T2 – Wissensfilter implementieren**
    - Umfang: Filter für Rolle, Availability, Freshness und Findings auf der paginierten Such-/Filtertrefferliste; Trefferauswahl fokussiert den Node im unveränderten Knowledge Tree.
    - Semantik: Werte derselben Gruppe werden ODER-verknüpft, unterschiedliche Gruppen UND-verknüpft; kein Filter ist der Default.
    - Prüfen: Paging bleibt stabil, Filterwechsel verwirft alte Cursor und leere Ergebnisse werden erklärt.
    - Tests: Query-/Repositoryverhalten, Web-Mapping, Komponenten und Browser-Smoke.
    - Abnahme: Filtersemantik aus dem Bedienkonzept ist vollständig serverseitig paginiert umgesetzt; der Tree wird nicht clientseitig beschnitten.

## M3.6 – Historie und Releases

- [x] **M3.6 abschließen**

  - [x] **M3.6-T1 – Snapshot- und Releaseübersichten implementieren**
    - Umfang: paginierte Listen, Kernmetadaten, Auswahl und Navigation in den jeweiligen Read Context.
    - Prüfen: unveränderliche Stände sind klar von Working Transactions getrennt.
    - Tests: Paging, leere Historie, ungültige Auswahl und Kontextübernahme.
    - Abnahme: historische und benannte Stände sind ohne MCP zugänglich.

  - [x] **M3.6-T2 – Snapshot-Diff-Ansicht implementieren**
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

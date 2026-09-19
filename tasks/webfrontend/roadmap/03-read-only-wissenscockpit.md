# M3 – Read-only Wissenscockpit

[Roadmap-Index](../Roadmap.md)

- [ ] **M3 abschließen**

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

  - [x] **M3.5-T3 – Suche gegen den deterministischen Browserbestand stabilisieren**
    - Befund aus dem M3.7-T2-Gate: `SearchSmoke` zeigt bei nichtleerem Bestand nach Auswahl des Fallback-Filters einen technischen Fehler statt einer Treffer- oder Leerergebnisdarstellung.
    - Abnahme: Die Suche verarbeitet den repräsentativen, über den realen MCP-Transport bereitgestellten Browserbestand einschließlich Fallback-Rolle fehlerfrei; der vollständige Browserlauf ist wieder grün.

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

- [x] **M3.7 abschließen**

  - [x] **M3.7-T1 – Bestehenden Markdown-Teilbaumexport in der UI bereitstellen**
    - Umfang: aktueller Node, Rolle und Read Context an vorhandenen Export-Use-Case übergeben und Datei herunterladen.
    - Prüfen: Root/Teilbaum, Fallback, Dateiname, MIME-Type, Fehler und große Ausgabe.
    - Nicht enthalten: PDF oder Exportprofile.
    - Tests: Mapping, Download-Endpunkt und Browser-Smoke.
    - Abnahme: bestehende Exportfunktion ist ohne Agent nutzbar.

  - [x] **M3.7-T2 – Deterministischen Browsernachweis für den Markdown-Download herstellen**
    - Befund aus dem M3.7-Review: Der aktuelle Browser-Smoke beendet sich bei einer leeren Browser-Testdatenbank erfolgreich und prüft dadurch weder Link noch Download.
    - Umfang: Die bestehende Browser-Testinfrastruktur stellt für diesen Test einen reproduzierbaren, nichtleeren Read-only-Wissensstand mit Rolle und auswählbarem Node bereit; der Smoke wartet anschließend auf einen tatsächlichen Download und prüft Status, `text/markdown; charset=utf-8`, `Content-Disposition: attachment`, `Cache-Control: no-store` und den `.md`-Dateinamen.
    - Grenze: Keine Produktdaten-Seeds, keine Produktionsmutation und kein Überspringen des Download-Nachweises bei leerem Zustand.
    - Abnahme: Der Browser-Smoke schlägt fehl, wenn der exportierbare Node, der Link oder die erfolgreiche Dateiauslieferung fehlt.

## M3.8 – Fachliche Parität

- [x] **M3.8 abschließen**

  - [x] **M3.8-T1 – UI- und MCP-Leseergebnisse gegen gemeinsame Use Cases prüfen**
    - Umfang: repräsentative Navigation, Rollenauflösung, Search, Historie, Diff und Export vergleichen.
    - Ziel: Mappingfehler erkennen; keine Bytegleichheit unterschiedlicher Transportmodelle erzwingen.
    - Abnahme: UI und MCP zeigen denselben fachlichen Zustand, dieselben Warnungen und dieselben Kontextgrenzen.

## M3.9 – Audit-Nacharbeiten

- [ ] **M3.9 abschließen**
  - Auditbasis: Code- und Nachweisprüfung am 2026-09-19 gegen `603f830`; M4-Arbeitsstand ist nicht Teil des Befunds.

  - [x] **M3.9-T1 – Tree-Circuit tatsächlich begrenzen und Request-Rennen schließen**
    - Befund: Ersetzte oder evictete Child-Seiten bleiben in `KnowledgeTreeState._knownNodes`; damit wächst der Circuit trotz `LoadedPageCount <= 10` bis zum Vollbaum. Bei zwei Requests desselben Parents kann das `finally` des alten Requests den neueren Cancellation-Eintrag entfernen; ein spät erfolgreiches altes Ergebnis derselben Kontextgeneration kann die neuere Seite überschreiben.
    - Umsetzung: Beim Seitenersatz/Evict alle nicht mehr erreichbaren Off-Path-Knoten samt Nachfahren aus Index, Auswahl-/Fokus- und Cursorzustand entfernen; nur Root, Knoten der höchstens zehn geladenen Seiten und die minimale ausgewählte Breadcrumb-Kette dürfen verbleiben. Pro Parent eine monotone Requestgeneration oder äquivalente Identität verwenden; nur der aktuell registrierte Request darf Ergebnis, Fehler, Loadingzustand oder Registry verändern. Eviction erst für eine erfolgreich übernommene neue Seite wirksam machen.
    - Tests: 1.050 direkte Children vorwärts/rückwärts, elf breite Zweige und wiederholte Seitenwechsel prüfen die reale Anzahl gehaltener Knoten/Seiten, nicht nur DOM und Cachezähler. Ein kontrolliert verzögerter Repository-Call ignoriert Cancellation und liefert nach dem Nachfolger; ausschließlich das Nachfolgerergebnis bleibt sichtbar und registriert. Kontextwechsel deckt denselben Fall ab.
    - Abnahme: Circuit und DOM können keinen Vollbaum akkumulieren; maximal zehn Childseiten plus minimale Breadcrumb-Kette bleiben referenziert, und überholte Antworten ändern niemals den aktuellen Zustand.

  - [x] **M3.9-T2 – Direkte Node-Navigation über beliebige Cursorseiten zuverlässig rekonstruieren**
    - Befund: `KnowledgeTreePathLoader` paginiert nur den bereits bekannten direkten Parent des Ziels. Liegt ein Zwischenknoten auf Seite 2+, wird dessen Parent nicht bis zu diesem Pfadsegment geblättert; Route, Suchtreffer oder Breadcrumb können dann Details laden, aber den Node im Tree weder offenlegen noch fokussieren.
    - Umsetzung: Den serverseitig ermittelten Ancestorpfad Root-abwärts abarbeiten; für jedes Segment dessen Parent expandieren und dessen opake Seiten nacheinander laden, bis das nächste Segment sichtbar ist oder ein strukturierter Fehler entsteht. Jede Seite unterliegt T1 und der Zehn-Seiten-Grenze; kein Cursor wird erzeugt/decodiert und kein Vollbaum geladen. Nicht gefunden, `InvalidCursor` und `CursorExpired` als erklärten Node-/Zweigzustand anzeigen statt eine unsichtbare Auswahl zu setzen.
    - Tests: Ziel auf einer späteren Seite eines flachen Parents sowie auf späteren Seiten mehrerer tiefer Parents; Einstieg per URL, Suche, Reload und Breadcrumb oberhalb eines rezentrierten Ausschnitts. Aufrufprotokoll prüft je Request Parent, Kontext, Rolle, `Limit = 100` und unveränderten Cursor.
    - Browsernachweis: deterministischer nichtleerer Bestand; 100/101, >1.000 direkte Children, zehn/elf geladene Seiten, Rezentrierung und Breadcrumb-Rückweg ohne bedingtes Überspringen. Für `ArrowUp/Down/Left/Right`, `Home`, `End`, `Enter` und Leertaste jeweils echten Fokus/Expand/Auswahlzustand und ausbleibendes Seitenscrollen prüfen.
    - Abnahme: Jeder gültige stabile `NodeId`-Einstieg endet mit sichtbarem, ausgewähltem und fokussierbarem Treeitem sowie vollständigen Breadcrumbs.

  - [x] **M3.9-T3 – Web-Lesegrenze für Rollen, Diagnostik und Working-Version vervollständigen**
    - Befund: Knowledge/Search und Pflichtselektor betrachten nur die erste `list_roles`-Seite (`Limit = 100`); eine ungültige explizite `roleId` fällt auf einen gültigen `localStorage`-Wert zurück. Seiten konsumieren gemappte Werte, verwerfen aber `Result.Warnings`; `WorkspaceState.CurrentChangeVersion` wird im M3-Lesepfad nie gesetzt. Breadcrumb-Reads kürzen Fehler still zu einem Teilpfad.
    - Umsetzung: Rollen über opake Cursor vollständig bzw. UI-paginiert anbieten und jede explizite Rolle gegen den gesamten Kontextbestand prüfen. Nur bei fehlendem `roleId` darf der validierte gespeicherte Wert greifen; ein explizit ungültiger Wert bleibt als fachlicher Fehler/Pflichtauswahl sichtbar. Erwartete Fehler inklusive Code/Details und alle Warnungen bis in einen zugänglichen Seitenzustand erhalten; technische Exceptions neutral mit Correlation-ID darstellen und serverseitig loggen. Bei Working Context dessen aktuelle `ChangeVersion` über einen gemeinsamen Application-Read laden, im Workspace/Context anzeigen und bei jedem zusammengehörigen Read konsistent halten; kein Repository-Aufruf aus Razor.
    - Tests: >100 Rollen mit Auswahl auf späterer Seite, ungültige Query-Rolle trotz gültigem Storage, leere Rollenliste, Working Transaction offen/fehlend/geschlossen, Warning an Node/Search/History sowie Breadcrumb-Fehler. Assertions prüfen strukturierte Codes/Details und `ChangeVersion`, nicht nur Meldungstext.
    - Abnahme: Jeder definierte Rolle ist auswählbar; URL-Kontext wird nie still umgedeutet und Diagnostik/Working-Version gehen an keiner Web-Grenze verloren.

  - [x] **M3.9-T4 – Provenienz und Tree-Status aus echten Reads anzeigen**
    - Befund: `KnowledgePage` übergibt dem Mapper stets `allDependencies: null`; die getestete Derived-Provenienz ist daher im Produktpfad immer leer. Das Bedienkonzept fordert Source-Revisions samt Freshness sowie Tree-Badges für Explicit/Fallback/None, Freshness und Findings; der Tree rendert aktuell nur Titel/Zustand.
    - Umsetzung: Einen transportneutralen metadata-first Node-Detail-Read verwenden/ergänzen, der für den tatsächlich aufgelösten Derived Content seine Source-Revisions und deren aktuellen Freshness-Vergleich liefert; UI und MCP mappen denselben fachlichen Stand. Tree-Summaries um die minimal nötigen Status-/Findingmetadaten ergänzen und als Text+Icon-Badges rendern, ohne Content oder Dependencies pro Treeitem nachzuladen.
    - Tests: geroutete `KnowledgePage` (kein direkt konstruiertes ViewModel) für Current, Snapshot und Working Transaction mit Independent, aktuellem Derived, stale Derived und Fallback auf Derived; Source-ID, Rolle, gespeicherte Revision und Source-Freshness prüfen. Tree-Komponentenfälle prüfen alle Badges und dass Expand weiterhin genau einen 100er-Read auslöst.
    - Abnahme: Wirksamer Content, Fallback und vollständige Derived-Provenienz sind sichtbar; Tree-Status entsteht ohne N+1-Reads oder Fachlogik im Webadapter.

  - [ ] **M3.9-T5 – Dashboard partiell fehlertolerant und mengenfest machen**
    - Befund: Ein Repository-/Validierungsfehler verwirft das gesamte Dashboard; der geforderte partielle Fehlerzustand fehlt. Alle Transactions werden nacheinander vollständig validiert, beide Snapshots vollständig geladen und der Diff mit `int.MaxValue` materialisiert. Qualitätswarnungen verlieren Code/Details, stale Links verlieren `roleId`, und reine Dependency-Änderungen fehlen bei „Zuletzt geändert“.
    - Umsetzung: Snapshot/Release, Transactions, Qualität und Änderungen als unabhängig ladbare Ergebnisbereiche modellieren; ein fehlerhafter Bereich lässt die anderen nutzbar und zeigt Retry plus neutrale technische Diagnose. Counts/Übersichten serverseitig aggregieren und große Listen stabil cursor-paginieren oder explizit policy-begrenzt nachladen; keine unbeschränkten Graph-/Diff-Loads und kein N+1 pro sichtbarer Transaction. Nodebezogene Content- und Dependency-Diffs dedupliziert aufnehmen; Qualitätsdiagnostik strukturiert erhalten und stale Einstieg mit `roleId` plus Current-Kontext verlinken.
    - Tests: isolierter Fehler je Bereich, große Transaction-/Quality-/Diff-Mengen, dependency-only Nodeänderung, stale Deep-Link und fehlender Vorgänger. Repository-Instrumentierung belegt begrenzte Calls/Items; UI zeigt niemals Exception-, SQL- oder Pfadtext.
    - Abnahme: Dashboard bleibt bei Teilausfall verständlich und bei großen Beständen begrenzt; jede Karte führt in den fachlich passenden Detailkontext.

  - [ ] **M3.9-T6 – Historienmetadaten und echten Browser-Diff nachweisen**
    - Befund: Die Snapshotliste zeigt ID/Zeit/Basis, aber nicht die im Bedienkonzept verlangte erzeugende Transaction und Commit-Metadaten. Der Browser-Smoke prüft nur Überschriften und den Zustand „Vergleich auswählen“; Listeninhalt, Paging, Kontextübernahme und tatsächliche Diffdarstellung sind damit nicht belegt.
    - Umsetzung: History-Application-Result metadata-first um erzeugende `TransactionId`, Actor/Client/Purpose und Commit Message ergänzen, soweit für den committed Snapshot vorhanden; UI zeigt kompakte Kernmetadaten ohne Storagekopplung. Browserbestand reproduzierbar mit mindestens zwei fachlich unterschiedlichen committed Snapshots und einem Release herstellen.
    - Tests: Application-/SQL-/Mappingfälle für initialen Snapshot und Snapshot mit Transaction-Metadaten. Headless Chrome wählt Snapshot und Release, übernimmt deren Read Context, vergleicht zwei Stände, zeigt repräsentative Node-/Content-/Rollen-/Resolution-/Dependencywerte, blättert eine Diffseite und prüft Nodefilter/Leerergebnis; kein bedingter Erfolgsweg bei leerem Bestand.
    - Abnahme: Historie erklärt Herkunft und Änderung eines Standes; der reale Browsernachweis scheitert bei fehlenden Daten, falschem Kontext oder nicht gerendertem Diff.

  - [ ] **M3.9-T7 – Markdown-Downloadfehler vertragstreu abschließen**
    - Befund: Erwartete Domainfehler werden gemappt, unerwartete Service-/IO-Ausnahmen besitzen jedoch keinen Endpoint-spezifischen RFC-9457-Vertrag; außerdem fällt jeder unbekannte Fehlercode pauschal auf `400`, obwohl exogene Fehler `500` sein müssen.
    - Umsetzung: Zentrale explizite Fehlercode→Status-Zuordnung mit sicherem Default `500`; unerwartete Exceptions loggen und ausschließlich neutrales `ProblemDetails` mit stabilem technischen Code und Correlation-ID liefern. Nie Attachment/Teilinhalt oder interne Meldung im Fehlerfall; Request-Abbruch nicht als Serverfehler loggen.
    - Tests: Export-/Navigation-Exception, unbekannter Domaincode, Abbruch und bestehende 400/404/409-Fälle gegen echten Host; Status, `application/problem+json`, Code, Correlation-ID, fehlendes `Content-Disposition` und fehlende interne Details prüfen.
    - Abnahme: Jeder Downloadfehler erfüllt den dokumentierten HTTP-Vertrag; erfolgreiche Header/Inhalt und der deterministische Browserdownload bleiben unverändert grün.

  - [ ] **M3.9-T8 – M3 erneut gesamthaft abnehmen**
    - Umfang: Ist-Dokumentation erst mit dem jeweiligen implementierten Verhalten synchronisieren; insbesondere die derzeit zu weit gehenden Aussagen zu Tree-Rekonstruktion und Source-Revisions in `docs/Architektur.md` gegen echte Produktpfade prüfen. Paritätstests müssen gemeinsame Use-Case-Ergebnisse durch die tatsächlich konsumierenden Webpfade führen, nicht nur isolierte Mapper mit künstlich befüllten ViewModels vergleichen.
    - Gate: `dotnet build`, Solution-Linter mit `verdict=pass`, `score=10.0`, `violationCount=0`, vollständige FastTests und Integrationstests einschließlich Browser-Suite. Keine M3.9-Abnahme wird wegen leerem Bestand, bedingtem Testpfad oder bloßem Cachezähler grün.
    - Abnahme: Milestone-Abnahme unten ist vollständig automatisiert belegt; erst danach M3.9 und „M3 abschließen“ markieren.

## Milestone-Abnahme

- Der vorhandene Wissensstand ist ohne MCP-Client navigierbar, suchbar, historisch einsehbar und als Markdown exportierbar.
- Rolle, Read Context, Fallback, Provenienz und Freshness sind sichtbar.
- UI und MCP verwenden dieselben Application-Use-Cases ohne duplizierte Fachlogik.

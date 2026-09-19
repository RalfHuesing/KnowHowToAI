# Bedienkonzept und UI

## Visueller Stil

Die Oberfläche ist sachlich, seriös und modern. Sie entspricht der Erwartung an eine aktuelle professionelle Business-Webanwendung.

- Ruhige, klare Informationshierarchie; keine verspielte oder chatzentrierte KI-Optik.
- Konsistente Typografie, Abstände, Raster, Icons und Interaktionsmuster.
- Hohe Informationsdichte ohne visuelle Unruhe.
- Statusfarben ausschließlich semantisch: Erfolg, Warnung, Fehler, stale, Working, committed.
- Klare Hover-, Fokus-, Auswahl-, Lade-, Leer- und Fehlerzustände.
- Tastaturbedienung, ausreichende Kontraste und zugängliche Komponenten.
- Die Implementierung verwendet Webstandards und enthält keine absichtlichen Ausschlüsse oder browserspezifischen Produktpfade. Sie soll in aktuellen Browsern funktionieren.
- Verbindlicher automatisierter Browser-Abnahmekanal ist Google Chrome Stable (installierte aktuelle Version). Andere Browserfamilien und Microsoft Edge werden nicht als eigene Testmatrix behandelt; ihre Funktion ist erwartete Kompatibilität, aber keine separat nachgewiesene Abnahme. Fehlendes Chrome ist ein klarer Preflight-Fehler, kein Anlass für einen Browserwechsel.
- Browser-E2E läuft ausschließlich nichtinteraktiv im Headless-Modus. Implementierungsagenten starten für reguläre Abnahmen kein sichtbares Browserfenster.
- Zielgerät ist ein PC mit Desktopbrowser; Smartphones und eine eigenständige mobile Oberfläche sind ausdrücklich kein Ziel.
- Die vollständige Desktopdarstellung ist ab 1280 × 720 CSS-Pixeln bei 100 % Zoom ausgelegt. Bei 1024 × 720 bleibt die gesamte Funktion mit verdichtetem Layout und einklappbaren Seitenbereichen erreichbar. Unterhalb dieser Referenzbreite besteht außer den nachfolgend festgelegten Zoom-/Accessibility-Regeln keine Produktanforderung.
- Produktbranding und Theme werden zentral definiert, nicht pro Seite erfunden.

Zeitwerte bleiben serverseitig UTC und werden in der UI in Browserlokalzeit mit UTC-Wert im Tooltip dargestellt.

## Branding und Sprache

- Der angezeigte Produktname lautet `KnowHowToAI`.
- M0 bis M2 verwenden ausschließlich eine Textwortmarke. Es wird weder ein Dummy-Logo noch ein vorläufiges Bildlogo oder eine Logo-Abhängigkeit angelegt.
- Die primäre Startfarbe ist ein neutrales Blau (`#2563EB`). Hover-, Active-, Fokus- und Kontrastvarianten werden in M2 als zentrale Tokens abgeleitet und müssen die festgelegten Accessibility-Regeln erfüllen.
- Die Standardschrift ist die lokale Systemkette `Segoe UI, Arial, sans-serif`; es werden keine Webfonts, Font-CDNs oder Font-Buildschritte eingeführt.
- Die Oberfläche ist im aktuellen Planungshorizont ausschließlich deutsch. Es gibt keine Sprachauswahl, keine `.resx`-Ressourcen, kein `IStringLocalizer` und keine andere Lokalisierungsinfrastruktur.
- Einmalige featurelokale Texte bleiben bei ihrer Razor-Komponente. Nur tatsächlich featureübergreifend wiederverwendete Bezeichnungen und Fehlermappings werden zentral gehalten; es entsteht keine globale Sammlung aller UI-Literale.
- Technische, im Produkt bereits etablierte Domänenbegriffe wie Snapshot, Transaction, Commit, Discard und Release dürfen in der deutschen UI unverändert verwendet werden.

## Barrierefreiheit

WCAG 2.2 AA ist der Entwicklungsmaßstab für die menschlichen Kernworkflows, jedoch keine formale Konformitäts- oder Zertifizierungsbehauptung. Es gibt im ersten Stand keine Browser- oder Screenreader-Matrix.

- Alle Kernfunktionen sind per Tastatur erreichbar. Es gibt keine Tastaturfalle; Fokusreihenfolge, sichtbarer und nicht vollständig verdeckter Fokus sowie Fokusübergabe bei Dialogen und Fehlern sind definiert.
- Semantisches HTML, zugängliche Namen, Labels, Statusmeldungen und Fehlerzuordnungen werden bevorzugt; ARIA ergänzt nur fehlende native Semantik.
- Textkontrast beträgt mindestens 4,5:1, großer Text mindestens 3:1. Relevante nichttextuelle UI-Zustände und Fokusdarstellungen erreichen mindestens 3:1 und werden nie nur durch Farbe vermittelt.
- Bei 200 % Desktop-Zoom gehen keine Informationen oder Funktionen verloren. Bei 400 % Zoom fließen normale Inhalte einspaltig um; fachlich wirklich zweidimensionale Bereiche dürfen innerhalb ihres eigenen Bereichs scrollen. Das ist Desktop-Zoom und begründet keine Smartphone-Unterstützung.
- Drag-and-drop erhält immer eine funktional gleichwertige Tastaturalternative. Der native Tree bietet dieselben Zielpositionen `Parent`, `Before` und `After` über Drag-and-drop und fokussierbare Aktionsbuttons. Milkdown wird auf die freigegebenen Befehle begrenzt und darf keine Heading- oder externe Bildfunktion anbieten.
- Agenten prüfen repräsentative Zustände mit bUnit und xUnit v3 sowie wenigen echten Tastatursequenzen über Microsoft.Playwright .NET. Browserläufe verwenden ausschließlich die installierte aktuelle Google-Chrome-Stable-Version, `Channel = "chrome"` und `Headless = true`. Eine kurze, feste manuelle Tastaturcheckliste wird für die Abnahme durch einen Menschen gepflegt; Agenten starten dafür keinen interaktiven Browser.

## Komponentenstrategie

Die folgenden Ergebnisse sind nach M0 verbindlich und keine erneuten Auswahlaufträge:

- Kostenpflichtige Komponenten und Abonnements sind ausgeschlossen. Alle direkten und transitiven externen Abhängigkeiten müssen kostenlos nutzbar und mit der Distribution des MIT-lizenzierten Projekts vereinbar sein.
- Standardmäßig zulässig sind permissive Lizenzen wie MIT, 0BSD, BSD-2-Clause, BSD-3-Clause, ISC und Apache-2.0 unter Einhaltung ihrer Copyright-, Lizenz- und NOTICE-Pflichten. Andere Lizenzen erfordern vor Aufnahme eine dokumentierte Einzelfallprüfung und ausdrückliche Benutzerentscheidung; Copyleft-, Source-available-, nutzungsbeschränkte oder kommerziell doppelt lizenzierte Komponenten sind nicht der Default.
- Es gibt keine allgemeine UI-Komponentenbibliothek. Layout, Formulare, Tabellen, Hinweise, Toasts und Dialoge werden mit nativem Blazor, semantischem HTML und überschaubarem eigenem CSS umgesetzt. Fluent UI, MudBlazor und weitere Suites werden in M1–M8 weder installiert noch erneut bewertet.
- Ein nativer HTML-`dialog` wird nur über eine schmale lokale JS-Isolation für `showModal()`, Fokusfalle, `Escape` und Fokusrückgabe ergänzt. Daraus entsteht kein allgemeines Control-Framework.
- Der Knowledge Tree ist eine native Blazor-/HTML-/CSS-Komponente. `Radzen.Blazor` ist ausgeschlossen, weil `RadzenTree` keine öffentliche Tree-Virtualisierungs- oder serverseitige Cursor-Paging-API besitzt. Es wird kein Fork, DOM-Patch oder Zugriff auf nichtöffentliche Komponenteninternas verwendet.
- Der einzige spezialisierte Editor ist Milkdown `@milkdown/crepe`; Details und Grenzen stehen in [Content und Assets](03-content-und-assets.md#rich-text-editor).
- Diese Produktauswahl ist abgeschlossen. Eine neue Komponente benötigt einen konkreten, belegten Inkompatibilitäts- oder Funktionsgrund, erneute Lizenzprüfung und eine Aktualisierung von `THIRD-PARTY-NOTICES.md`; eine allgemeine Komponentensuche ist kein Folge-Task. Versionsaktualisierungen folgen dagegen der Abhängigkeitsregel im [Strukturkonzept](08-projektstruktur-und-codekonventionen.md).

## Grundlayout

```text
┌ Navigation/Baum ─────┬ Node-Editor/Ansicht ─────┬ Kontext/Status ────┐
│ Suche und Filter     │ Titel, Beschreibung          │ Rolle/Fallback       │
│ Lazy Tree           │ Rollen-Content              │ Freshness/Quellen    │
│ Drag-and-drop       │ Rich Text + Vorschau        │ Findings/Quellen     │
│ Badges              │ Diff bei Änderung          │ Historie             │
└──────────────────────┴────────────────────────────┴─────────────────────┘
Kontextleiste: Wissensbasis | Snapshot/Transaction | Rolle | Änderungszustand
```

Snapshot/Transaction und Rolle bleiben global sichtbar. Historischer Zustand oder aufgelöster Fallback-Content darf nie unbemerkt bearbeitet werden.

## Dashboard

- Current Snapshot und letzter Release.
- Alle offenen Transactions mit Zweck, Akteur, Client, Alter, Base Snapshot und `ChangeVersion`; ohne Auth gibt es keine belastbare Einschränkung auf „eigene“ Transactions.
- Qualitätsübersicht des Current Snapshot: stale Derived Contents und Qualitätswarnungen über alle Rollen.
- Harte Validierungsfehler werden pro offener Transaction gezeigt; ein committed Snapshot besitzt definitionsgemäß keine harten Validierungsfehler.
- Zuletzt geänderte Nodes werden aus dem Diff zwischen Current Snapshot und seinem direkten committed Vorgänger abgeleitet; Releases werden nach Erzeugungszeit sortiert.
- Direkte Einstiege in Wissensbaum, Transaction, Vergleich und Publikation.

## Wissensbaum

- Native Blazor-/HTML-/CSS-Implementierung ohne Tree-Paket.
- Der einzelne fachliche Root wird separat geladen; Children werden ausschließlich bei Expand mit `limit = 100` über den vorhandenen Navigation-Use-Case angefordert. `nextCursor` bleibt opak und wird unverändert für die nächste Seite desselben Parents weitergereicht.
- Eine geladene Parent-Seite enthält höchstens 100 Einträge. Seitennavigation ersetzt die sichtbare Seite desselben Parents, statt frühere Seiten im DOM anzuhängen. Für „Zurück“ speichert der Circuit je Parent nur die zuvor verwendeten opaken Cursorstrings, keine früheren Itemseiten; der vorherige Cursor wird erneut serverseitig geladen.
- Der Circuit hält höchstens zehn geladene 100er-Seiten. Beim Laden einer elften Seite wird die am längsten ungenutzte Seite eines nicht ausgewählten Teilbaums entfernt und dieser Zweig sichtbar geschlossen. Gehören alle zehn Seiten zum aktuellen Auswahlpfad, wird die rootnächste Seite entfernt und ihr Kind auf dem Auswahlpfad zum visuellen Root des Tree-Ausschnitts; die vollständige globale Herkunft bleibt über Breadcrumbs navigierbar. Breadcrumb-Navigation oberhalb des Ausschnitts lädt die benötigte Seite erneut und unterliegt derselben Zehn-Seiten-Regel. Eine höfliche Statusmeldung erklärt das Schließen beziehungsweise Neuzentrieren.
- Auswahl, Expand-Zustand, aktuell sichtbare Seiten und LRU-Reihenfolge sind ausschließlich flüchtiger Circuit-State. Route und Query bleiben Quelle für Node, Rolle und Read Context; nach Reload werden nur die für den ausgewählten Pfad benötigten Seiten erneut geladen.
- Der DOM enthält niemals den Gesamtbaum. Die Begrenzung erfolgt durch serverseitiges Paging und den Zehn-Seiten-Cache; echte Viewport-Virtualisierung ist weder Voraussetzung noch behauptete Eigenschaft.
- Suche nach Titel, Beschreibung und rollenaufgelöstem Content.
- Filter nach Rolle, Availability, Freshness und Findings wirken auf die paginierte Trefferliste neben der Suche. Innerhalb einer Filtergruppe gilt ODER, zwischen Filtergruppen UND; ungefilterte Ansicht ist der Default. Der Tree wird nicht clientseitig beschnitten; die Auswahl eines Treffers fokussiert dessen Node im Tree.
- Badges für eigenen Content, Fallback, fehlenden Content, stale und Warnung.
- Drag-and-drop für Verschieben und Sortierung mit Zielvorschau.
- Der Tree verwendet `role="tree"`/`role="treeitem"`, roving `tabindex`, `aria-level`, `aria-selected` und bei Parents `aria-expanded`. Pflichtbedienung: `ArrowUp`, `ArrowDown`, `ArrowLeft`, `ArrowRight`, `Home`, `End`, `Enter` und Leertaste.
- `Parent`, `Before` und `After` sind sowohl Drag-and-drop-Ziele als auch fokussierbare Aktionsbuttons mit demselben Move-Vertrag.
- Strukturänderungen nur in einer offenen Working Transaction.
- Tiefe Strukturen werden nicht vollständig vorab geladen.
- Breadcrumbs und direkte Navigation per stabiler `NodeId`.

## Node-Ansicht und Editor

- Struktur: Titel, Description, Parent, SortOrder, stabile NodeId.
- Der globale Rollen-Selektor steuert die Node-Ansicht; die Ansicht zeigt `Explicit`, `Fallback` oder `None` und erzeugt keinen zweiten lokalen Rollenmechanismus.
- `requestedRole`, `resolvedRole`, Content Mode, Revision und Freshness.
- Rich-Text-Editor und optionale Markdown-Quellansicht.
- Vorschau erzeugt Node-Überschrift aus der Struktur, nicht aus `ContentMd`.
- Derived Content zeigt Source-Revisions und deren Freshness.
- Vergleich mit Base Snapshot und historischen Snapshots.
- Rollen-Content-Löschung und globale Node-Löschung sind klar getrennt.

### Speichern und unpersistierter Zustand

- Formulare und der Contenteditor speichern ausschließlich über eine explizite Benutzeraktion; es gibt kein Autosave und kein zeitgesteuertes Debounce-Schreiben.
- Nach erfolgreicher Mutation werden Working Snapshot, `ChangeVersion`, Diff-Indikator und dargestellte Daten aus der Serverantwort aktualisiert.
- Navigation, Rollen-/Nodewechsel, Reconnect-Reload und Schließen eines dirty Editors zeigen `Bleiben` oder `Ungespeicherte Eingabe verwerfen`; es gibt kein implizites Speichern.
- Drag-and-drop persistiert beim Drop unmittelbar in die aktive Transaction. Bei Serverablehnung kehrt der Tree zum bestätigten Serverzustand zurück und zeigt den strukturierten Fehler.
- Resolution Orders werden lokal sortiert und erst mit `Speichern` als vollständige Reihenfolge ersetzt.
- Commit und Discard besitzen immer einen expliziten Bestätigungsdialog.
- **Undo-Umfang (O-027):** Lokales Browser-Undo (`Strg+Z`) wirkt ausschließlich innerhalb eines Textfeldes oder Editors bis zum letzten Speichern. Es gibt keinen globalen Undo-Stack für persistierte Mutationen innerhalb einer Transaction; Korrekturen erfolgen durch Gegenänderung oder vollständiges Discard der Transaction.

Editor-, TODO- und Assetdetails: [Content und Assets](03-content-und-assets.md).

## Transaction-Arbeitsbereich

- Transaction beginnen oder fortsetzen.
- Zweck, Akteur und Client werden beim Beginnen gesetzt, bleiben danach immutable und sind sichtbar. Die Commit Message wird erst im Commitdialog erfasst.
- **Actor (O-007):** Der Actor wird beim Starten einer neuen Transaction durch `ICurrentUserService.GetCurrentUserName()` gesetzt. Die initiale Implementierung ist ein Dummy-Service, der einen konfigurierten oder festen Platzhalternamen zurückgibt; der Service wird später durch echte Authentifizierung ersetzt, ohne dass Transaction-Komponenten angepasst werden müssen. Der Actor-Wert ist nach dem Start immutable.
- **Gleichzeitige Clients (O-025):** Mehrere UI- oder MCP-Clients dürfen gleichzeitig in derselben offenen Transaction schreiben. Es gibt keine Locks. Jede Mutation überträgt `ChangeVersion`; stale Writes werden vom Server deterministisch abgelehnt. Der Client zeigt einen fachlichen Hinweis und fordert zum Neuladen des betroffenen Bereichs auf.
- **Transaction-Lebensdauer (O-026):** Offene Transactions verfallen nicht automatisch. Das Dashboard zeigt das Alter einer Transaction deutlich an; Transactions älter als sieben Tage erhalten ein Warnbadge. Schließen erfolgt ausschließlich durch explizites Commit oder Discard.
- Strukturierter Netto-Diff nach Rollen, Resolution Orders, Nodes, Contents und Dependencies.
- Validierung mit Errors, Warnings, Stale Contents und Refactoring-Kandidaten.
- Bewusste Diff- und Validation-Sicht vor Commit.
- Discard mit klarer Auswirkung auf den Working Snapshot.
- Bei `SnapshotConflict` Base/Current anzeigen; kein vorgetäuschtes automatisches Merge.

## Historie und Releases

- Snapshots mit Zustand, Zeit, Transaction und Metadaten.
- Beliebige committed Snapshots vergleichen.
- Releases anzeigen und auf ihren Snapshot navigieren.
- Release aus committed Snapshot anlegen.
- Node-Historie aus Snapshot-Diffs ableiten; kein erfundenes Operation Log.

Routen, Query-Parameter und die Rekonstruktion des Arbeitskontexts: [Projektstruktur und Codekonventionen](08-projektstruktur-und-codekonventionen.md#url--und-arbeitskontext).

## Rollenverwaltung

- Rollen erstellen, umbenennen und löschen.
- Resolution Order als sortierbare Liste bearbeiten.
- Fallback-Auswirkung transparent vorschauen.
- Änderungen erfolgen in einer Transaction.
- Keine Vermischung mit Authentifizierung oder ACL.

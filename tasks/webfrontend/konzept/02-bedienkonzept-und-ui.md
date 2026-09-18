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
- Verbindlicher automatisierter Browser-Abnahmekanal ist ausschließlich die jeweils aktuelle stabile Desktopversion von Google Chrome. Andere Browserfamilien und Microsoft Edge werden nicht als eigene Testmatrix behandelt; ihre Funktion ist erwartete Kompatibilität, aber keine separat nachgewiesene Abnahme.
- Browser-E2E läuft ausschließlich nichtinteraktiv im Headless-Modus. Implementierungsagenten starten für reguläre Abnahmen kein sichtbares Browserfenster.
- Zielgerät ist ein PC mit Desktopbrowser; Smartphones und eine eigenständige mobile Oberfläche sind ausdrücklich kein Ziel.
- Die vollständige Desktopdarstellung ist ab 1280 × 720 CSS-Pixeln bei 100 % Zoom ausgelegt. Bei 1024 × 720 bleibt die gesamte Funktion mit verdichtetem Layout und einklappbaren Seitenbereichen erreichbar. Unterhalb dieser Referenzbreite besteht außer den nachfolgend festgelegten Zoom-/Accessibility-Regeln keine Produktanforderung.
- Produktbranding und Theme werden zentral konfiguriert, nicht pro Seite erfunden.

Offene Vorgaben zu Branding und UI-Sprache stehen in [Offene Fragen](07-entscheidungen-und-offene-fragen.md). Zeitwerte bleiben serverseitig UTC und werden in der UI in Browserlokalzeit mit UTC-Wert im Tooltip dargestellt.

## Barrierefreiheit

WCAG 2.2 AA ist der Entwicklungsmaßstab für die menschlichen Kernworkflows, jedoch keine formale Konformitäts- oder Zertifizierungsbehauptung. Es gibt im ersten Stand keine Browser- oder Screenreader-Matrix.

- Alle Kernfunktionen sind per Tastatur erreichbar. Es gibt keine Tastaturfalle; Fokusreihenfolge, sichtbarer und nicht vollständig verdeckter Fokus sowie Fokusübergabe bei Dialogen und Fehlern sind definiert.
- Semantisches HTML, zugängliche Namen, Labels, Statusmeldungen und Fehlerzuordnungen werden bevorzugt; ARIA ergänzt nur fehlende native Semantik.
- Textkontrast beträgt mindestens 4,5:1, großer Text mindestens 3:1. Relevante nichttextuelle UI-Zustände und Fokusdarstellungen erreichen mindestens 3:1 und werden nie nur durch Farbe vermittelt.
- Bei 200 % Desktop-Zoom gehen keine Informationen oder Funktionen verloren. Bei 400 % Zoom fließen normale Inhalte einspaltig um; fachlich wirklich zweidimensionale Bereiche dürfen innerhalb ihres eigenen Bereichs scrollen. Das ist Desktop-Zoom und begründet keine Smartphone-Unterstützung.
- Drag-and-drop erhält immer eine funktional gleichwertige Tastaturalternative. Tree und Rich-Text-Editor werden in M0 nur ausgewählt, wenn ihre Kernfunktionen diese Regeln erfüllen oder mit begrenztem, dokumentiertem Aufwand erfüllen können.
- Agenten prüfen repräsentative Zustände automatisiert mit Komponentenassertionen, wenigen Accessibility-Smokes und echten Tastatursequenzen im Headless-Chrome-Lauf. Eine kurze, feste manuelle Tastaturcheckliste wird für die Abnahme durch einen Menschen gepflegt; Agenten starten dafür keinen interaktiven Browser.

## Komponentenstrategie

- Kostenpflichtige Komponenten und Abonnements sind ausgeschlossen. Alle direkten und transitiven externen Abhängigkeiten müssen kostenlos nutzbar und mit der Distribution des MIT-lizenzierten Projekts vereinbar sein.
- Standardmäßig zulässig sind permissive Lizenzen wie MIT, 0BSD, BSD-2-Clause, BSD-3-Clause, ISC und Apache-2.0 unter Einhaltung ihrer Copyright-, Lizenz- und NOTICE-Pflichten. Andere Lizenzen erfordern vor Aufnahme eine dokumentierte Einzelfallprüfung und ausdrückliche Benutzerentscheidung; Copyleft-, Source-available-, nutzungsbeschränkte oder kommerziell doppelt lizenzierte Komponenten sind nicht der Default.
- Für einfache Layouts, Formulare und Gestaltung werden Blazor, semantisches HTML und überschaubares eigenes CSS bevorzugt. Eine kleine projektspezifische Lösung ist einer umfangreichen Suite oder zusätzlichen Toolchain vorzuziehen, wenn sie gleich verständlich, testbar und wartbar ist.
- Für nachweislich komplexe Controls wie Knowledge Tree oder Markdown-Rich-Text-Editor werden fokussierte, etablierte Open-Source-Komponenten bevorzugt, wenn sie Risiko und Eigenaufwand materiell senken. Eine allgemeine Suite wird nicht allein für ein einzelnes Control eingeführt.
- Auswahlkriterien: fachliche Passung, aktive Pflege, Lizenz aller transitiven Abhängigkeiten, Paket- und Bundle-Gewicht, zusätzliche Build-Toolchain, JS-Interop, .NET-10-/Blazor-Kompatibilität, Barrierefreiheit, Internationalisierung, Testbarkeit, Theme-Fähigkeit, keine erzwungene Cloud/CDN-Nutzung und kein proprietäres Contentformat.
- Komponenten werden vor Festlegung mit realistischen Daten und Randfällen gespikt. „Keine allgemeine Komponentenbibliothek“ ist ein zulässiges Ergebnis.

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

- Lazy Loading über Root und paginierte Children.
- Suche nach Titel, Beschreibung und rollenaufgelöstem Content.
- Filter nach Rolle, Availability, Freshness und Findings wirken auf die paginierte Trefferliste neben der Suche. Innerhalb einer Filtergruppe gilt ODER, zwischen Filtergruppen UND; ungefilterte Ansicht ist der Default. Der Tree wird nicht clientseitig beschnitten; die Auswahl eines Treffers fokussiert dessen Node im Tree.
- Badges für eigenen Content, Fallback, fehlenden Content, stale und Warnung.
- Drag-and-drop für Verschieben und Sortierung mit Zielvorschau.
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

Editor-, TODO- und Assetdetails: [Content und Assets](03-content-und-assets.md).

## Transaction-Arbeitsbereich

- Transaction beginnen oder fortsetzen.
- Zweck, Akteur und Client werden beim Beginnen gesetzt, bleiben danach immutable und sind sichtbar. Die Commit Message wird erst im Commitdialog erfasst.
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

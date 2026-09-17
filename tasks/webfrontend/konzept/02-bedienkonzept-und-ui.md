# Bedienkonzept und UI

## Visueller Stil

Die Oberfläche ist sachlich, seriös und modern. Sie entspricht der Erwartung an eine aktuelle professionelle Business-Webanwendung.

- Ruhige, klare Informationshierarchie; keine verspielte oder chatzentrierte KI-Optik.
- Konsistente Typografie, Abstände, Raster, Icons und Interaktionsmuster.
- Hohe Informationsdichte ohne visuelle Unruhe.
- Statusfarben ausschließlich semantisch: Erfolg, Warnung, Fehler, stale, Working, committed.
- Klare Hover-, Fokus-, Auswahl-, Lade-, Leer- und Fehlerzustände.
- Tastaturbedienung, ausreichende Kontraste und zugängliche Komponenten.
- Desktop-first für komplexe Baum- und Editorarbeit; kleinere Viewports bleiben lesbar.
- Produktbranding und Theme werden zentral konfiguriert, nicht pro Seite erfunden.

## Komponentenstrategie

- Etablierte Blazor- oder browserbasierte Komponenten für Layout, Formulare, Dialoge, Tabellen, Baum, Editor und Upload verwenden.
- Eigenentwicklung nur für KnowHowTo-spezifische Interaktion oder bei nachgewiesener Lücke.
- Auswahlkriterien: aktive Pflege, kompatible Lizenz, .NET-10-/Blazor-Kompatibilität, Barrierefreiheit, Internationalisierung, Testbarkeit, Theme-Fähigkeit, keine erzwungene Cloud und kein proprietäres Contentformat.
- Komponenten werden vor Festlegung mit realistischen Daten und Randfällen gespikt.

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
- Offene Transactions mit Zweck, Akteur, Alter und Base Snapshot.
- Stale Derived Contents.
- Harte Validierungsfehler und Qualitätswarnungen.
- Zuletzt geänderte Nodes und Releases.
- Direkte Einstiege in Wissensbaum, Transaction, Vergleich und Publikation.

## Wissensbaum

- Lazy Loading über Root und paginierte Children.
- Suche nach Titel, Beschreibung und rollenaufgelöstem Content.
- Filter nach Rolle, Availability, Freshness und Findings.
- Badges für eigenen Content, Fallback, fehlenden Content, stale und Warnung.
- Drag-and-drop für Verschieben und Sortierung mit Zielvorschau.
- Strukturänderungen nur in einer offenen Working Transaction.
- Tiefe Strukturen werden nicht vollständig vorab geladen.
- Breadcrumbs und direkte Navigation per stabiler `NodeId`.

## Node-Ansicht und Editor

- Struktur: Titel, Description, Parent, SortOrder, stabile NodeId.
- Rollen-Tabs oder Rollen-Selektor mit `Explicit`, `Fallback`, `None`.
- `requestedRole`, `resolvedRole`, Content Mode, Revision und Freshness.
- Rich-Text-Editor und optionale Markdown-Quellansicht.
- Vorschau erzeugt Node-Überschrift aus der Struktur, nicht aus `ContentMd`.
- Derived Content zeigt Source-Revisions und deren Freshness.
- Vergleich mit Base Snapshot und historischen Snapshots.
- Rollen-Content-Löschung und globale Node-Löschung sind klar getrennt.

Editor-, TODO- und Assetdetails: [Content und Assets](03-content-und-assets.md).

## Transaction-Arbeitsbereich

- Transaction beginnen oder fortsetzen.
- Zweck, Akteur, Client und Commit Message sichtbar pflegen.
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

## Rollenverwaltung

- Rollen erstellen, umbenennen und löschen.
- Resolution Order als sortierbare Liste bearbeiten.
- Fallback-Auswirkung transparent vorschauen.
- Änderungen erfolgen in einer Transaction.
- Keine Vermischung mit Authentifizierung oder ACL.

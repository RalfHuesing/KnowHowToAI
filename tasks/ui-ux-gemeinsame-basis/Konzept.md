---
status: ready
---

# Gemeinsame UI-Seitenbasis

## Intention

Alle vorhandenen und künftigen routbaren Web-Seiten sollen dieselbe erkennbare
und technisch erzwungene Seitenbasis verwenden. Die Shell, der Seitenrahmen und
der Seitenkopf erhalten eindeutige Verantwortlichkeiten, damit neue Features
nicht erneut lokale Breiten, Außenabstände oder Überschriftenvarianten erfinden.

Das Ergebnis ist kein Redesign der Fachseiten. Es vereinheitlicht deren äußere
Struktur und Semantik, während fachliche Raster, Formulare, Tabellen, Bäume und
Arbeitsabläufe in den jeweiligen Features bleiben.

## Zielbild und Entscheidungen

### Layout-Ownership

- Die Anwendungsshell besitzt die volle Viewportfläche, die ein- und
  ausblendbare Navigation sowie den einheitlichen Innenabstand der
  Arbeitsfläche.
- Jeder routbare Page-Root füllt innerhalb dieses Shell-Innenabstands die
  gesamte verfügbare Breite aus. Er erhält kein eigenes Shell-Padding, keine
  Zentrierung und kein seitenweites `max-width`.
- „Fullscreen“ bezeichnet diese volle verfügbare Arbeitsfläche und keine fest
  erzwungene Viewport-Höhe. Normaler Seiteninhalt bleibt im Dokument scrollbar;
  es entsteht keine zweite allgemeine Scrollfläche.
- Breitenbegrenzungen sind nur innerhalb fachlich begrenzter Inhalte zulässig,
  beispielsweise für Fließtext. Navigation, Baum, Tabellen, Diffs, Formulare
  und Aktionsflächen bleiben grundsätzlich breitennutzend.

### Gemeinsamer Seitenrahmen und Seitenkopf

- Eine wiederverwendbare Blazor-Seitenbasis bildet den ausführbaren Vertrag für
  Page-Root, Seitenkopf und Inhaltsbereich. Sie unterstützt Seitentitel,
  optionale Kurzbeschreibung sowie optionale Badges oder Aktionen, ohne das
  Featurelayout vorzugeben.
- Jede routbare Seite rendert in jedem relevanten Lade-, Leer-, Fehler- und
  Erfolgszustand genau ein semantisches `h1` über diesen gemeinsamen Vertrag.
  Typografie, Abstände und Umbruch des Seitenkopfs werden nicht lokal
  überschrieben.
- Für die Wissensseite ist `Wissensbasis` der stabile Seitentitel (`h1`); ein
  ausgewählter Node ist untergeordneter Seiteninhalt und verwendet für seinen
  Titel `h2`. Für eine Transaction-Detailseite darf der konkrete Zweck weiterhin
  der dynamische Seitentitel (`h1`) sein.
- Innerhalb von `main#shell-main` gibt es kein weiteres `main`-Landmark.

### CSS- und Agentenvertrag

- Reset, Design-Tokens und wirklich frameworkweite Layoutprimitive bleiben in
  der vorhandenen zentralen CSS-Basis. Die gemeinsame Blazor-Seitenbasis besitzt
  ihre einmalige, komponentennahe Gestaltung; fachliches Layout bleibt im
  scoped CSS des jeweiligen Features.
- Lokale Kopien der gemeinsamen Page-Root-, `h1`- oder Header-Regeln werden aus
  Feature-CSS entfernt. Eine Feature-Ausnahme benötigt eine fachliche
  Verantwortung und einen expliziten routeübergreifenden Nachweis.
- Es entsteht kein separates Scaffolding- oder Texttemplate. Die gemeinsame
  Komponente ist das ausführbare Template; verbindliche Ist-Dokumentation und
  automatisierte Strukturtests machen den Vertrag für spätere Agenten
  auffindbar und schützen ihn gegen erneuten Drift.
- Die Startseite `/` ist die visuelle Referenz für die Nutzung der
  Shell-Arbeitsbreite und den grundsätzlichen Seitenrhythmus. Ihre
  dashboard-spezifischen Badges, Aktionen und Karten sind keine Vorgabe für
  andere Seiten.

## Scope

### Muss

- Die gemeinsame Seitenbasis gilt ohne Layoutausnahme für alle derzeit
  implementierten Routen:

  | Route | Relevante Strukturzustände |
  |---|---|
  | `/` | Laden, geladene Bereiche, bereichsweiser Fehler |
  | `/knowledge` | Laden/Fehler, keine Zielgruppe, keine Auswahl, leerer Working Tree |
  | `/knowledge/{NodeId:guid}` | wie `/knowledge`, zusätzlich ausgewählter oder nicht gefundener Node |
  | `/search` | Kontextfehler, keine Zielgruppe, bereit, Suche/Ergebnisse |
  | `/transactions` | Laden, leer, Startfehler, offene Transactions |
  | `/transactions/{TransactionId:guid}` | Laden, Fehler, offene oder abgeschlossene Transaction, Snapshot-Konflikt |
  | `/audiences` | Laden, Kontextfehler, lesender und schreibender Kontext |
  | `/history` | Auswahl, Vergleich/Fehler und Release-Bereich |

- Page-Root, nutzbare Breite, Shell-Inset, vertikaler Grundrhythmus und
  Page-Header-/`h1`-Semantik werden über die gemeinsame Basis vereinheitlicht.
- Bestehende fachliche Funktionen, Kontextübergaben, Interaktionen,
  Fokusverhalten und Responsive-/Reflow-Verträge bleiben erhalten.
- Die dauerhafte UI-Ist-Dokumentation beschreibt die Ownership und die Nutzung
  der gemeinsamen Seitenbasis so, dass neue Agenten keine lokale
  Parallelstruktur ableiten müssen.
- Der bestehende routeübergreifende Browsernachweis wird auf sämtliche oben
  genannten Routen und die strukturell abweichenden Zustände erweitert.

### Nicht

- Kein visuelles oder fachliches Redesign einzelner Featurebereiche, Karten,
  Formulare, Tabellen, Bäume, Diffs oder Dialoge.
- Keine Vereinheitlichung jeder untergeordneten `h2`-/`h3`-Darstellung und kein
  allgemeines Komponenten- oder Designsystemprojekt.
- Keine neue UI-Bibliothek, kein Generator und kein zusätzliches Theme.
- Keine Änderung von Navigationseinträgen, Routen, Fachlogik, Datenzugriff oder
  MCP-Verträgen.
- Keine Smartphonefreigabe und keine Erweiterung der bestehenden
  Accessibility-Ziele; vorhandener Desktop-Zoom und Reflow dürfen jedoch nicht
  verschlechtert werden.
- Keine feste Content-Höhe, kein globales Abschneiden von Überlauf und keine
  Baselineaktualisierung ohne manuelle visuelle Prüfung.

## Verifikation

- Komponententests belegen den gemeinsamen Seitenbasis-/Header-Vertrag und für
  jede routbare Page genau ein `h1` in den strukturell relevanten Zuständen.
- Browserprüfungen vergleichen bei 1280 × 720, 1024 × 720 sowie breiten
  Desktop-Viewports die sichtbaren linken und rechten Innenkanten jedes
  Page-Roots mit `main#shell-main`; Navigation offen und geschlossen nutzt die
  jeweils verfügbare Breite ohne lokales `max-width`, zusätzliches Außenpadding
  oder horizontalen Seitenüberlauf.
- Die bestehenden Reflow-Nachweise bei effektiven 640 und 320 CSS-Pixeln bleiben
  grün; Inhalte und Aktionen bleiben erreichbar.
- Ein gemeinsamer UI-Audit-Lauf erzeugt Screenshots aller betroffenen Routen und
  repräsentativen Zustände. Assertions zu Computed Styles, Bounding Boxes,
  Überschriften und Overflow sind primär; Screenshots werden gemeinsam manuell
  auf Drift geprüft und ersetzen diese Assertions nicht.
- Die geänderte Ist-Dokumentation, der vollständige Diff und
  `git diff --check` sind Bestandteil der Abnahme. Build-, FastTest- und die
  betroffenen Browser-Testgates richten sich in der Umsetzung nach den
  verbindlichen Repository-Regeln.

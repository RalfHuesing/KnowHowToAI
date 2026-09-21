# Layout- und Aktionssystem

**Version:** 1.0 · **Status:** verbindlich für M2-Basis und featureweise Adoption

## Seitenrahmen und Fläche

**Verbindlich:** Der Seitenrahmen nutzt die volle verfügbare Monitorbreite konsistent für Shell, Page-Frame und Arbeitsflächen. Eine Lesemessung (`readable measure`) gilt nur für Prosa, Beschreibungen und vergleichbaren Fließtext; sie darf Navigation, Baum, Tabellen, Diff, Formulare oder Aktionsflächen nicht künstlich auf eine Textbreite begrenzen.

Die Basisprimitive sind `page-frame`, `readable` und `action-group`. Sie werden als DRY-Tokens/Base-Patterns zentral definiert und featureweise adoptiert. Bestehende Feature-CSS bleibt zunächst unangetastet, solange eine Migration nicht nötig ist. Die primitive Basis darf keine neue UI-Komponentenbibliothek und keinen globalen Action-Datenkatalog erzeugen.

## Grundraster

1. globale Shell/Kontextleiste;
2. Seitenkopf mit Titel, Aufgabe und gegebenenfalls Breadcrumb;
3. voller Page-Frame;
4. fachliche Arbeitsabschnitte in sinnvoller Reihenfolge;
5. lokale Action-Group im selben Abschnitt wie ihre Auswirkung;
6. technische Details, IDs und seltene Folgewege progressiv.

Bei 1280 CSS-Pixeln ist die Desktop-Funktion vollständig sichtbar; 1920 und 2560 nutzen zusätzliche Breite ohne überlange Prosazeilen. Responsive/kompakt erhält alle Funktionen mit verdichteten oder einklappbaren Bereichen gemäß bestehendem Webfrontend-Vertrag.

## Aktionshierarchie

- **Primär:** genau eine nächste Aktion pro sichtbarem Abschnitt, mit Text und fachlichem Ergebnis (`Speichern`, `Validieren`, `Commit ausführen`).
- **Sekundär:** Navigation, alternative Modi und risikoarme Folgewege (`Im Wissensbaum öffnen`, `Details anzeigen`).
- **Destruktiv:** klar getrennt und semantisch markiert (`Verwerfen`, `Löschen`), immer mit bestehender Bestätigung.
- **Selten:** History/Download kompakt als Text oder Icon-plus-Text; Icon-only erst nach einer expliziten Entscheidung und zugänglichem Namen.

Globale Aktionen ändern den Kontext oder die Sicht (`Zielgruppe/ReadContext wählen`, Shell-Navigation). Lokale Aktionen ändern Node, Content oder Transaction. Eine globale Aktion darf nicht die lokale Hauptaktion verdrängen.

## PageActions-Slot

`PageActions` ist eine vorhandene, derzeit ungenutzte Layoutregion. **Verbindlich:** Sie bleibt ein Slot für echte seitenweite Aktionen mit Bezug zur ganzen Seite. Es gibt keine Action Registry. Ein Feature adoptiert den Slot nur, wenn die Aktion nicht zu einem einzelnen Node, Formular oder Editor gehört; der Aufruf und die Entscheidung bleiben featurelokal.

## Zustandsdarstellung

Loading, Empty, Error, Not found, Conflict und Dirty werden als echte Zustände der fachlichen Oberfläche dargestellt. Keine Capture- oder Layoutarbeit darf einen Zustand erfinden, verstecken oder durch Scroll-/DOM-Tricks ersetzen. Text führt die Bedeutung; Farbe und Icon verstärken nur.

## Responsive und Nachweis

Jede Basis- oder Featuremigration weist mindestens 1280, 1920, 2560 und die bestehende Responsive-Referenz nach. Zu prüfen sind horizontaler Überlauf, Zeilenumbruch von Aktionsgruppen, erreichbare primäre Aktion, lesbare Prosa-Messung und unveränderte Route/Funktion. Tastatur ist kein eigenes M2-Abnahmekriterium; native Semantik darf nicht absichtlich verschlechtert werden.

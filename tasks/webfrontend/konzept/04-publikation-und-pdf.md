# Publikation und PDF

## Anwendungsfall

```text
Teilbaum: Kundenanpassungen / Max Schulze GmbH
Rolle: Endkunde
Stand: Release 2026.09
Profil: Max-Schulze-Endkundendokumentation
Ausgabe: PDF
```

Ein Klick erzeugt ein reproduzierbares Dokument aus dem gewählten Wissensstand.

## Publikationsprofil

Ein Profil definiert mindestens:

- Name und Dokumenttitel.
- Root Node oder Auswahl mehrerer Teilbäume.
- Angefragte Rolle und Verhalten bei Fallback oder fehlendem Content.
- Snapshot oder Release als Datenquelle; Working Transaction nur für Vorschauen.
- Logo, Farben, Schriften, Seitenformat, Ränder, Kopf- und Fußzeilen.
- Deckblatt, Inhaltsverzeichnis und Seitennummerierung.
- Maximale oder dargestellte Hierarchietiefe.
- Ein-/Ausschlussregeln für leere Struktur-Nodes, stale Content und Findings.
- Ausgabeformat und Dateinamensschema.

Profile und Assets sind versioniert oder werden unveränderlich referenziert. Nur damit ist ein Release-Dokument später reproduzierbar.

## Pipeline

```text
Snapshot/Release
  → rollenbezogene Baumauflösung
  → neutrales Dokumentmodell
  → Template/Layout
  → HTML-Vorschau
  → PDF-Renderer
  → Exportprotokoll
```

- Das neutrale Dokumentmodell trennt Wissensselektion von PDF-Technik.
- HTML und später DOCX können denselben aufgelösten Dokumentstand verwenden.
- Bilder werden über die zentrale Asset-Auflösung eingebunden.
- Der Renderer ist eine etablierte Komponente; Auswahl nach CSS-, TOC-, Header/Footer-, Font-, Lizenz- und Deployment-Unterstützung.

## Freigabe

- Arbeitsvorschau darf eine Working Transaction verwenden und ist deutlich gekennzeichnet.
- Offizielle Publikation verwendet committed Snapshot oder Release.
- Optionale Policy: Export blockieren oder bestätigen lassen bei stale Content, harten Findings oder offenen Editorial Notes.
- Exportprotokoll enthält SnapshotId, optional Release, Profilversion, Assetrevisionen, Findings und Erzeugungszeit.

## Kundenwissen und Zielgruppen

Eine Hierarchie wie `Kundenanpassungen / Max Schulze GmbH` passt in den globalen Baum. Drei Konzepte bleiben getrennt:

| Konzept | Bedeutung |
|---|---|
| Hierarchie | Fachliche Ablage und Navigation des Kundenwissens |
| Content-Rolle `Endkunde` | Zielgruppengerechte Darstellung |
| Zugriffsschutz | Wer Wissen lesen oder ändern darf; nicht durch Content-Rollen gelöst |

Unterschiedliche Publikationsnavigationen werden später als Presentation Views auf denselben stabilen `NodeId`s modelliert; der kanonische Baum bleibt erhalten.

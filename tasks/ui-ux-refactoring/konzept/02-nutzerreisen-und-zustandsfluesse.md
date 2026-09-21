# Nutzerreisen und Zustandsflüsse

**Version:** 1.0 · **Status:** verbindlich für M2 und folgende Konzeptgates

Die Reisen beschreiben das erwartete Bedienmodell mit bestehenden Funktionen. Sie sind keine Produktfreigabe für neue Aktionen. Jede Reise muss im sichtbaren Arbeitsabschnitt eine eindeutige nächste Aktion und den fachlich korrekten Kontext zeigen.

## Finden und lesen

`Start (/) → Wissensbaum (/knowledge) → Pflicht-Zielgruppenauswahl → Node (/knowledge/{NodeId})`.

1. Start führt zum vorhandenen Wissenszugang; Diagnose bleibt sekundär.
2. Fehlt eine gültige Zielgruppe aus Query oder geprüftem `localStorage`, öffnet sich der bestehende Pflichtselektor. Zielgruppe erklärt Perspektive, nicht Zugriff.
3. Nach Bestätigung werden Zielgruppe, `ReadContext`, Baum und Auswahl gemeinsam aktiv; `NodeId` in URL, Tree-Auswahl, Breadcrumb und Detail müssen übereinstimmen.
4. Read-only zeigt Inhalt beziehungsweise ehrlichen None-/Fallback-Zustand. History und Markdown-Download bleiben kompakte Folgewege.

## Konkreten Eintrag bearbeiten

Aus einer konkreten Read-only-Node ist der Übergang zur Bearbeitung der kritischste bestehende Nutzungspfad. **M2 führt ihn node-lokal und sichtbar:** `Wissenseintrag lesen → Bearbeiten → kompatible Arbeitskopie wählen oder Neue Arbeitskopie beginnen → derselbe Wissenseintrag und dieselbe Zielgruppe im Working-Editor`. Die Auswahl ist nie still; ein historischer Snapshot oder Release bleibt read-only.

Der Working-Arbeitsplatz bleibt innerhalb derselben Knowledge-Seite (`NodeDetailsPane`). Route, Query und bestehende Transaction-/Editorverträge bleiben maßgeblich. Für Explicit+Independent führt die Aktion direkt in den vorhandenen Editor. Fallback, `None` und `Derived` werden verständlich als nicht direkt bearbeitbare Zustände geführt; die neuen Contentaktionen dafür bleiben [M5.4-T1](../../webfrontend/roadmap/05-zielgruppen-content-und-rich-text/tasks/M5.4-T1.md) und [M5.4-T2](../../webfrontend/roadmap/05-zielgruppen-content-und-rich-text/tasks/M5.4-T2.md).

Die vollständige Zustandsmatrix sowie die verbindlichen URL- und Terminologieverträge stehen im ausführbaren [M2.0-T1-Leaf](../roadmap/02-systemrahmen-und-navigationsfluss/tasks/M2.0-T1.md).

## Node/Root anlegen

- **Root:** `/knowledge?transactionId=...` zeigt bei leerem Baum den bestehenden `RootNodeEditor`; `CreateNodeRequest` verwendet `ParentNodeId = null`.
- **Child:** Im Working-Node-Abschnitt wird über `NodeMetadataEditor` ein bestehendes Child angelegt; der Baum liefert den sichtbaren Parent und die Child-Aktion.
- **Nach Erfolg:** `NodeMutationResult` liefert die neue/stabile `NodeId` und `ChangeVersion`. Die sichtbare Auswahl muss auf den fachlich maßgeblichen Node wechseln. URL und Breadcrumb müssen diese Auswahl zuverlässig widerspiegeln; die jetzige Aktualisierung von Tree-/Workspace-Selection synchronisiert die URL nicht zuverlässig.
- **Availability:** Eine neue Node startet mit `Availability=None`. Erster eigener Content oder Fallback-Übernahme ist M5.4-T1, nicht reine Layoutarbeit.

## Content bearbeiten

Im Working-Kontext führt „Inhalt bearbeiten“ denselben Content in WYSIWYG und Markdown-Quelle. `Dirty` bedeutet ungespeicherte Editor-Eingabe; `Speichern` schreibt in die aktive Transaction und aktualisiert `ChangeVersion`. M2 öffnet diesen vorhandenen Editor aus dem node-lokalen Einstieg für Explicit+Independent. Fallback bleibt read-only. Eigenen Content anlegen, Fallback als Ausgangstext übernehmen und explizit leer speichern sind M5.4-T1; Derived/Freshness folgt M5.4-T2.

## Validieren, Commit, Discard

`/transactions/{TransactionId}` führt verbindlich: (1) Validieren, (2) Commit oder vollständig Verwerfen. Validierung liest die Working-Version; bei zwischenzeitlicher Änderung ist das Ergebnis stale. Commit ist irreversibel und wird bestätigt; Discard lässt den Current Snapshot unverändert und wird bestätigt. Bei `SnapshotConflict` zeigt die Seite Base/Current und bietet manuelles Reapply; kein automatisches Merge.

Nach erfolgreichem Commit/Discard führt der vorhandene Weg zurück zu Current Knowledge. Eine spätere Entscheidung kann die Reihenfolge oder Führung bewerten; M2 implementiert keine Änderung des Commit-Vertrags.

### Offene Darstellungsoption: geführter Abschluss

Der seltene, lineare und folgenreiche Abschluss kann außerhalb M2 als Assistent beziehungsweise Stepper innerhalb derselben Transaction-Route bewertet werden: `Validieren → Änderungen prüfen → Commit-Nachricht → Bestätigen`. Zurück, Weiter und Abbrechen führen dabei ausschließlich durch die Darstellung; Validierungs-, Commit-, Discard-, Conflict- und Dirty-Verträge bleiben unverändert. Mehrere neue Einzelseiten oder ein Assistent für alltägliche Aufgaben wie Suche und Content-Bearbeitung sind nicht vorweggenommen. Die Entscheidung steht als UX-011 im [Entscheidungsregister](06-entscheidungsregister.md).

## History

`/history` zeigt Snapshot-/Release-Listen und den bestehenden Vergleich von Ausgang und Ziel. `nodeId` filtert die Node-Historie; `snapshotId` oder `releaseId` führen als ReadContext in `/knowledge`. IDs bleiben für Experten erreichbar, die fachliche Vergleichsbedeutung steht zuerst.

## Zielgruppen und Fallback

`/audiences` verwaltet Zielgruppen in einer Working Transaction. Aufgelöster Fallback ist read-only und wird mit angefragter und aufgelöster Zielgruppe erklärt. AudienceId, Revision und technische Herkunft gehören in die progressive Ebene. Zielgruppe ist Perspektive, keine ACL. Zielgruppenverwaltung bleibt in M2 außerhalb des Migrationsumfangs.

## Leere, Fehler-, Konflikt- und Dirty-Zustände

| Zustand | Sichtbare Führung | Verbindlicher Ausweg |
|---|---|---|
| Empty | Was fehlt und was als Nächstes möglich ist; keine erfundene Datenlage | bestehende Start-/Anlageaktion oder Navigation |
| Error/Not found | fachlich verständlicher Fehler, technischer Code sekundär | bestehender sicherer Rückweg/Retry |
| Conflict | Base/Current, Verlustfreiheit und kein Merge-Versprechen | Vergleich und manuelles Reapply oder Discard |
| Dirty | betroffener Editor, ungespeicherte Eingabe, Speichern im selben Abschnitt | Speichern oder bestehender Navigationsschutz „Bleiben/Verwerfen“ |
| Read-only/Fallback | Lesen und Herkunft klar, keine irreführende Editieraktion | bestehender Transaction-Einstieg beziehungsweise Folgeweg |

## Zustandsinvariante nach Mutation

Nach Create child, Create root, Update und Delete werden `NodeId`, Tree-Selection, Detaildaten, Breadcrumb, WorkspaceState und URL als ein Zustand behandelt. Bei Delete wird der bestehende fachliche Parent- beziehungsweise Root-Fallback gewählt; eine gelöschte URL darf nicht als scheinbar gültige Auswahl verbleiben. Dieses Verhalten wird in M2.2-T1 zuerst als roter Test festgeschrieben.

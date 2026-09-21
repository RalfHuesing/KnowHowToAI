# Findings- und Evidenzinventar

**Version:** 1.0 · **Status:** versionierter Ist-/Nachweisindex

## Historische und aktuelle Evidenz

| Evidenz | Bedeutung |
|---|---|
| `temp/ui-audit/2026-09-20_18-36-16` | historischer M1.1-Lauf, 20 Zustände bei 1280×800; Befunde bleiben als historische Beobachtung erhalten |
| `temp/ui-audit/2026-09-20_20-05-26` | grüner M1.3-Re-Audit; bestätigt Root, Diff und Löschdialog sowie M1.2-Führung |
| `temp/ui-audit/2026-09-20_21-35-15` | M1.4-Abschluss und M1.5-T2–T6-Basis, 20 Zustände bei 1280×800 |
| `temp/ui-audit/m1-5-t1-rerun/2026-09-20_22-00-50` | deterministischer Capture-Nachweis Zustand 04 |
| `temp/ui-audit/m1-5-t3/2026-09-20_22-45-00` | Read-only-/Fallback-Führung 04/05 |
| `temp/ui-audit/m1-5-t5/2026-09-20_23-20-36` | History 08/09 |
| `temp/ui-audit/m1-5-t6/2026-09-20_23-25-32` | Suchbegriffe 06/07 |

Screenshots und Manifeste sind temporär und nicht zu committen; versioniert werden Befunde, Nachweise und Entscheidungen.

## M1-Statusinventar

- **Erledigt:** M1.0 Planung, M1.1 Audit, M1.2-T1/T2, M1.3 Re-Audit, M1.4-T1–T7 sowie M1.5-T0–T7.
- **Ein-Karten-Grid:** M1.5-T7 ist abgeschlossen. M2 übernimmt den Nachweis nur als Regression und führt keinen zweiten Grid-Leaf ein.
- **Out of scope:** `AudiencesPage.razor.cs` und Zielgruppenverwaltung; sie werden nicht als UI-Refactoring-Fix in M2 vermischt. Ein parallel bearbeiteter Linterbefund bleibt fremder Scope und ist hier nur historischer Kontext.
- **Zurückgestellt:** „Commit vor Validierung“; bis zu einer fachlichen Entscheidung gilt Validieren → Commit/Verwerfen.

## Aktuelles Code-/Routeninventar

| Bereich | Route(n) | Bestehende Funktionen/Kontexte |
|---|---|---|
| Dashboard | `/` | Wissenszugang, offene Transactions, Qualitäts-/Snapshot-/Release- und Recent-Change-Information |
| Wissen | `/knowledge`, `/knowledge/{NodeId:guid}` | Pflicht-Zielgruppenauswahl, Tree/Expand/Paging/Drag-and-drop, Breadcrumb, Read-only-Detail, Working-Struktur, ContentEditor, History-/Markdown-Folgewege, Root-/Child-/Update-/Delete-Mutationen |
| Suche | `/search` | Suchfeld, Trefferliste, Zielgruppe/Availability/Freshness/Findings-Filter, Treffer in Tree/Node-Kontext |
| Historie | `/history` | Snapshot-/Release-Listen, Ausgang/Ziel-Diff, `nodeId`-Filter, Navigation in `snapshotId`-/`releaseId`-ReadContext |
| Transactions | `/transactions`, `/transactions/{TransactionId:guid}` | Beginnen/Auflisten/Fortsetzen, Zweck/Akteur/Client, Validieren, Diff, Commit/Discard, Konflikt und manuelles Reapply, „Im Wissensbaum öffnen“, Zielgruppen pflegen |
| Zielgruppen | `/audiences` | Read-only-/Working-Zielgruppenpflege, Anlegen/Umbenennen/Löschen, später Resolution Order/Fallback-Vorschau; M2 out of scope |
| Content | keine eigene Route | WYSIWYG/Markdown-Quelle, Dirty/Save, Explicit/Fallback/None, später M5.4-Aktionen |
| Shell | global | Start, Suche, Transactions, Zielgruppen; Kontextleiste/ContextSelector; Navigation und Context-Panel; PageActions-Slot ungenutzt |

### Kontextverträge

- `NodeId` bleibt stabil; Route `/knowledge/{NodeId}` identifiziert die Auswahl.
- `audienceId` ist die Zielgruppenperspektive und bei zielgruppenaufgelösten Reads/Exporten explizit.
- Genau einer von `transactionId`, `snapshotId`, `releaseId` wird als ReadContext-Selektor akzeptiert; ohne Selektor Current.
- Query/Route sind rekonstruierbare Quelle; `WorkspaceState` und Tree-Caches sind flüchtiger Circuit-State.
- `ChangeVersion` schützt Working-Mutationen; nach Erfolg muss die neue Version in Kontext und UI sichtbar sein.

## Priorisierte Befunde für M2

1. **P0/P1 – Read-only Node → Bearbeitung nicht auffindbar:** Befund 04/05 und aktueller Code zeigen Inhalt, History und Download, aber keine prominente Bearbeitungsaffordance. Die technische Transaktionseröffnung und der Einstieg „Im Wissensbaum öffnen“ sind nicht als zusammenhängender Weg geführt. M2 adressiert dies zuerst mit dem freigegebenen node-lokalen `Bearbeiten`-Einstieg; der Befund gilt bis zur Umsetzung als offen.
2. **P1 – Transaction-Detail verliert Arbeitsfluss:** `TransactionPage` besitzt `Im Wissensbaum öffnen`, `Zielgruppen pflegen` und `Zur Übersicht`; der erste Weg ist nach Commit-/Diff-Inhalt nicht prominent genug in der Bearbeitungsreise. M2.3-T1 führt ausschließlich diesen bestehenden Weg sichtbarer.
3. **P1 – Mutation-URL/Selection-Drift:** `KnowledgePage.HandleNodeMutationSucceededAsync` aktualisiert `TreeWorkspace` und `WorkspaceState`, navigiert aber nicht auf die neue/Parent-URL. Create child/root/update/delete können damit sichtbare Auswahl und URL entkoppeln. M2.2-T1 ist red-test-first.
4. **P1 – Systemrahmen:** Vollbreiten-Seitenrahmen, Prosa-Measure und Action-Group sind nicht als gemeinsame Basisfeatureweise standardisiert. M2.1-T1 schafft nur diese Primitive und migriert kontrolliert.
5. **Erledigter Grid-Harmonisierungspunkt:** M1.5-T7 belegt den Ein-Karten-Fall; M2 prüft ihn nur als Regression.

## Befundgrenzen

Der Screenshot beweist einen sichtbaren Zustand, nicht seine Produktabsicht. Ein fehlender Capture-Anker ist eine Audit-Lücke. Layoutarbeit darf keine fehlende Fallback-Quelle, keinen ersten Content und keine neue Bearbeitungslogik fingieren. Availability `None` und erster eigener Content/Fallback-Übernahme gehören zu `tasks/webfrontend/roadmap/05-zielgruppen-content-und-rich-text/tasks/M5.4-T1.md`.

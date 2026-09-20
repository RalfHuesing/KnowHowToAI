# Abschlussaudit M4 – Transactions und Strukturpflege

## Auditstand

- Datum: 2026-09-20
- Geprüfter Git-Stand: `c12f79229532929fc2505e0abcdd10f660811f0d`
- Ausgangszustand: sauberer Working Tree; Branch `v2` zwei Commits vor
  `origin/v2`.
- Urteil: **Nacharbeit erforderlich**. Der Audit ist vollständig durchgeführt,
  M4 bleibt wegen der offenen Tasks M4.6-T1 bis M4.6-T5 offen.

## Umfang und Nachweise

Geprüft wurden die M4-Roadmap und sämtliche vorhandenen M4-Leaf-Tasks, die
relevanten Konzepte, die verbindlichen Ist-Dokumente zu Transaktionen,
Hierarchie, Retrieval, MCP, Datenmodell, Architektur und Invarianten sowie die
Produktions- und Testimplementierung und die M4-Git-Historie. Besondere
Prüfpunkte waren Transaction-Lebenszyklus und Arbeitskontext, Navigation und
Reconnect, Validierung und Diff, Commit/Discard/Release, Node-Pflege,
Drag-and-drop, Parallelität, `ChangeVersion`, `SnapshotConflict`, gemeinsame
Application-Use-Cases und Schichtengrenzen.

Vorhandene Implementierungsnachweise liegen insbesondere in den Commits
`1ffeed1`, `7e8b309`, `e374504`, `e8ff3f9`, `c050d1b`, `c80f4de`, `a44d814`,
`7f0456e`, `b9f5be5`, `4ed6faf`, `da39177`, `4b12cb2`, `44e1fd1` und `46fa063`.
Zusätzlich wurden gezielt ausgeführt:

- `dotnet test tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj
  --filter "FullyQualifiedName~NodeMutationServiceTests" --no-restore` – 30/30
  bestanden.
- `dotnet test tests/KnowHowToAI.Web.Tests/KnowHowToAI.Web.Tests.csproj
  --filter "FullyQualifiedName~KnowledgeTreeTests|FullyQualifiedName~NodeMetadataEditorTests|FullyQualifiedName~RootNodeEditorTests|FullyQualifiedName~NodeDeletionEditorTests"
  --no-restore` – 19/19 bestanden.
- `dotnet test tests/KnowHowToAI.Web.Tests/KnowHowToAI.Web.Tests.csproj
  --no-restore --filter
  "FullyQualifiedName~Features.Transactions|FullyQualifiedName~Components.Layout.NavigationProtection|FullyQualifiedName~Features.History"`
  – 47/47 bestanden.

## Bestätigte Bereiche

- Transaction-Beginn, Auflistung, Fortsetzung und per Route rekonstruierbarer
  Working Context sind implementiert; Alter, Base Snapshot und ChangeVersion
  bleiben sichtbar.
- Commit und Discard arbeiten über die gemeinsamen Application-Use-Cases;
  Commit validiert und prüft Base gegen Current atomar, Discard verändert Current
  nicht.
- Validierung und strukturierter, cursorpaginierter Netto-Diff decken die fünf
  dokumentierten Kategorien ab; Findings sind gruppiert und Node-bezogen
  navigierbar.
- Release-Erstellung verwendet den bestehenden Application-Use-Case und lässt
  ausschließlich committed Snapshots zu; Warnungen blockieren nicht.
- Node-Erstellung, Stammdatenänderung, initiale Root-Anlage und kontrollierte
  Löschung einschließlich Tombstones und Dependencies sind implementiert.
- Die drei Drag-and-drop-Zonen, Servervalidierung, Reload nach Ablehnung und
  persistierte Working-Snapshot-Mutation sind vorhanden.
- `SnapshotConflict` wird atomar erkannt und mit Base/Current-Diff sowie neuer,
  leerer Transaction für manuelles Reapply behandelt; es gibt kein implizites
  Merge, Rebase oder automatisches Discard. M4.4-T1 war implementiert und nur im
  Roadmapstatus offen.
- Web und MCP delegieren an gemeinsame Application-Use-Cases; die geprüften
  M4-Pfade verletzen keine Schicht- oder Projektstrukturgrenze.

## Befunde

### A1 – Before/After ist innerhalb derselben Geschwistergruppe nicht positionsgenau (hoch)

`TreeMoveCoordinator` übersetzt `Before` in den bestehenden `SortOrder` des
Ziels und `After` in `SortOrder + 1`. `NodeMutationService` ersetzt nur den
SortOrder der Quelle; `SiblingOrderNormalizer` löst die dadurch entstehenden
Gleichstände nach `NodeId`. Das Ergebnis kann deshalb abhängig von GUIDs vor
oder hinter der verlangten Position landen oder unverändert bleiben. Die
Browsernachweise prüfen Mutationserfolg, ChangeVersion und Sichtbarkeit, aber
nicht die resultierende Reihenfolge. Betroffen sind der `Parent`/`Before`/`After`-
Vertrag aus M4.3-T3/T5 und die Milestone-Abnahme der vollständigen
Node-Strukturpflege. Nacharbeit: [M4.6-T1](tasks/M4.6-T1.md).

Belege: `TreeMoveCoordinator.cs:32-41`, `NodeMutationService.cs:78-82`,
`SiblingOrderNormalizer.cs:65-80` und `KnowledgeTreeMoveSmokeTests.cs:56-75`.

### A2 – M4-Strukturformulare aktivieren den Navigationsschutz nicht (hoch)

`NavigationProtection` und `WorkspaceState.SetDirty` existieren, aber weder
`NodeMetadataEditor` noch `RootNodeEditor` setzen oder räumen den Dirty-State bei
unpersistierter Eingabe. Damit kann ein Benutzer nach einer Titel- oder
Beschreibungsänderung ohne Warnung navigieren, den Kontext wechseln oder einen
Reload auslösen. Die vorhandenen Schutztests setzen Dirty ausschließlich
synthetisch. Betroffen sind M4.1-T2 und der verbindliche Bedienvertrag für
ungespeicherte Formulare. Nacharbeit: [M4.6-T2](tasks/M4.6-T2.md).

Belege: `NodeMetadataEditor.razor.cs:42-93`, `RootNodeEditor.razor.cs:22-47`,
`WorkspaceState.cs:29-40` und `NavigationProtection.razor.cs:27-58`.

### A3 – MCP-Strukturmutationen sind nicht vollständig ChangeVersion-sicher (hoch)

O-025 verlangt, dass jede Mutation die gelesene `ChangeVersion` überträgt und
stale Writes deterministisch abgelehnt werden. Core und UI unterstützen dies
für Create/Move/Reorder, die MCP-Tools `create_node`, `move_node` und
`reorder_node` exponieren beziehungsweise übergeben `expectedChangeVersion`
jedoch nicht. Ein MCP-Client kann deshalb mit veraltetem Working-Kontext eine
Strukturmutation ausführen. `update_node` und `delete_node` sind bereits
abgesichert. Betroffen sind O-025, M4.3 und die Milestone-Abnahme zu parallelen
MCP-Änderungen. Nacharbeit: [M4.6-T3](tasks/M4.6-T3.md).

Belege: `NodeMutationTools.cs:42-67,219-261`,
`NodeMutationApplicationService.cs:26-85` und
`McpTransactionToolRegistrationTests.cs:187-209`.

### A4 – Validierungsbefunde verlieren ihre atomar gelesene ChangeVersion (mittel)

Der SQL-Read liefert die ChangeVersion gemeinsam mit den Validierungsdaten,
`WorkingSnapshotValidationData` und das Validierungsergebnis transportieren sie
aber nicht weiter. Die UI etikettiert Befunde stattdessen mit ihrem lokalen
Workspace-Wert. Nach einer parallelen Mutation kann sie deshalb Befunde einer
anderen Version als aktuell darstellen. Der Commit selbst revalidiert weiterhin
korrekt. Betroffen sind M4.2-T1, O-025 und der Vertrag für veraltete
Validierungsergebnisse. Nacharbeit: [M4.6-T4](tasks/M4.6-T4.md).

Belege: `SqlWorkingSnapshotReadRepository.cs:25-42,88-137`,
`WorkingSnapshotValidationData.cs:8-15`,
`SqlWorkingSnapshotValidationDataRepository.cs:22-34` und
`TransactionValidation.razor.cs:45-64`.

### A5 – Der Konflikt-Smoke belegt Datenhalt und Reapply nur mit leeren Transactions (mittel)

Der Browser-Smoke erzeugt den parallelen Commit, zeigt den Konflikt und startet
eine neue Transaction, verändert aber weder die konfliktbehaftete noch die neue
Transaction fachlich. Damit belegt er den geschäftskritischen Vertrag „keine
Änderung verloren, kein implizites Merge, bewusstes Reapply“ nur trivial. Dies
ist eine konkrete Nachweislücke des ausdrücklich geforderten parallelen
UI-/MCP-Ablaufs, keine zusätzliche Testvariante. Nacharbeit:
[M4.6-T5](tasks/M4.6-T5.md).

Belege: `SnapshotConflictSmokeTests.cs:25-61` und
`TransactionLifecycleTests.cs:102-137`.

## Verworfene oder nicht taskwürdige Hinweise

- Die alte M4-Kopfaussage zu fokussierbaren Verschiebebuttons widersprach dem
  später beauftragten M4.3-T5 und dem aktualisierten Bedienkonzept. Sie wurde als
  Roadmap-Synchronisierung korrigiert; die bewusst mausbasierte Verschiebung ist
  kein Implementierungsbefund.
- Fehlende ChangeVersion-Guards für Rollen- und Contentmutationen werden nicht in
  M4 gezogen; diese Benutzeroberflächen und ihre Vertragsabnahme gehören zu M5.
- Der optionale, vom MCP-Aufrufer gelieferte Actor entspricht dem dokumentierten
  MCP-Vertrag. Die UI-Ermittlung über `ICurrentUserService` begründet keinen
  M4-Befund.
- Weitere theoretische Race-Varianten ohne abweichenden Vertrag oder realistischen
  Fehlerpfad sowie kosmetische Refactorings wurden nicht aufgenommen.

## Abschlussurteil

Der Abschlussaudit wurde durchgeführt und M4.5 kann als erledigt gelten. Die
bereits geplanten Leaf-Tasks einschließlich M4.4-T1 sind implementiert; bei
M4.4-T1 fehlte nur die Statuspflege. Wegen fünf belegter Nacharbeiten ist M4
nicht bestanden und bleibt bis zur Abnahme von M4.6 offen.

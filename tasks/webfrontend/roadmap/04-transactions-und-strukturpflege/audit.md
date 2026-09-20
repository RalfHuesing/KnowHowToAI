# Abschlussaudit M4 – Transactions und Strukturpflege

## Auditstand

- Datum: 2026-09-20
- Initialer Auditstand: `c12f79229532929fc2505e0abcdd10f660811f0d`; Urteil der
  ersten Runde: Nacharbeit erforderlich wegen der fünf Befunde A1–A5.
- Final geprüfter Git-Stand: `b11c9e82978b549f12f7d1100c4fc6a9f61ed240`
  (`b11c9e8`); Branch `v2`, sauberer Ausgangs-Working-Tree.
- Urteil: **bestanden**. Die gezielte Korrekturrunde M4.6 ist abgeschlossen;
  M4.2, M4.3, M4.4 und M4.6 sowie die drei Milestone-Abnahmekriterien sind
  geschlossen.

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

## Korrekturrunde M4.6

Die fünf Befunde A1–A5 wurden in genau einer gezielten Korrekturrunde
bearbeitet. Die Leaf-Abschlussnachweise und die zugehörigen Commits sind:

| Leaf | Ergebnis | Commit |
| --- | --- | --- |
| M4.6-T1 | Exakte `Before`-/`After`-Einfügepositionen, Parentwechsel und Randpositionen sind serverseitig persistent und per Browser geprüft (Browser 3/3). | `7fd135b` |
| M4.6-T2 | Strukturformulare liefern ihren Dirty-State an den bestehenden Navigationsschutz; Save, Cancel, Fehler und Verwerfen sind abgedeckt (Browser 1/1). | `b09755e` |
| M4.6-T3 | `create_node`, `move_node` und `reorder_node` übertragen `expectedChangeVersion`; stale Writes werden atomar abgelehnt. | `a960cb0` |
| M4.6-T4 | Validierungsbefunde führen die atomar gelesene ChangeVersion bis zur UI und werden bei einer neueren Workspace-Version stale. | `d2886ff` |
| M4.6-T5 | SnapshotConflict mit echter UI- und MCP-Änderung, bewusstem Reapply und explizitem Discard ist persistent nachgewiesen (Browser 1/1). | `b11c9e8` |

Damit sind M4.6 und die abhängigen Parent-Status abgeschlossen: M4.2 nach
M4.2-T4 und M4.6-T4, M4.3 nach M4.3-T1/T2/T3 (ergänzt um T4/T5) und
M4.6-T1/T2/T3 sowie M4.4 auf Basis der Bestandsimplementierung M4.4-T1
(`da39177`) und M4.6-T5.

## Finale Gates

- `pwsh -NoProfile -File scripts/build.ps1`: grün.
- AiNetLinter Solution-Pass: `verdict=pass`, `score=10.0`,
  `violationCount=0`.
- FastTests: 1.037/1.037 bestanden.
- Integration: 79/79 bestanden.
- M4-relevante Browser-E2E: M4.6-T1 3/3, M4.6-T2 1/1 und M4.6-T5 1/1
  bestanden; die übrigen M4-Nachweise sind in den jeweiligen Leaf-Dateien
  dokumentiert.

### Abgrenzung der VisualShell-Baseline

Im Gesamt-Browserlauf bleiben zwei VisualShell-Baselineabweichungen bestehen
(23/25 Browser-Tests bestanden). Beide liegen außerhalb des M4-Scopes und
sind vorbestehend. Der Historiencheck der beiden Baseline-Dateien ergibt an
`d2886ff`, `7fd135b` und dem aktuellen Stand `b11c9e8` identische Git-Blob-
Hashes:

| Baseline | identischer Blob-Hash |
| --- | --- |
| `Shell-1024x720-light.png` | `c37d8c9938de266052b5dec169ad4674d269125` |
| `Shell-1280x720-light.png` | `ee1359c9ef3eb3e19fcbd1621d4ae4fccbb11fa6` |

Nach der Projektregel für vorbestehende, außerhalb des Scopes liegende Fehler
sind sie kein M4-Blocker; die M4-relevanten Browser-Nachweise bleiben
vollständig grün.

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

Die erste Audit-Runde wurde durch die Korrekturrunde M4.6 vollständig
abgeschlossen. Die Bestandsimplementierung einschließlich M4.4-T1, die fünf
Korrekturen, alle drei Milestone-Abnahmekriterien und die finalen Gates sind
belegt. Die zwei außerhalb M4 liegenden, historisch unveränderten
VisualShell-Baselineabweichungen ändern daran nichts. **M4 ist bestanden und
geschlossen.**

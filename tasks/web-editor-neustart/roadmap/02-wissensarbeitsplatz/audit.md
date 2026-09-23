# M2-Audit – Wissensarbeitsplatz

Datum: 2026-09-23
Ergebnis: Bestanden; das einzige Nachweis-Finding ist geschlossen.

## Geprüfter Umfang

Nur-lesender Abgleich von Konzept, M2-Roadmap, den Leafs M2.1–M2.3,
Projektregeln, M1-Audit, Implementierungs-Diffs und den zugehörigen Fast- und
Browsertests. Produktionscode wurde nicht geändert.

Geprüft wurden insbesondere Tree-Paging und tiefe Pfadladung, Zielgruppen- und
Entwurfskontext, direkte Text-/Metadatenbearbeitung mit Dirty-, Stale- und
Fehlerfällen, Root-/Child-Anlage sowie Drag-, Tastatur- und Reload-Verhalten.
Die Moves laufen über den bestehenden Mutation-Use-Case; abgelehnte Moves
laden den bestätigten Baumzustand erneut. Die getesteten Bereiche halten die
M2-Nicht-Ziele ein. Leaf-Nachweise melden erfolgreiche Builds, Solution-
Linter-Gates und die jeweils vorgesehenen Testläufe.

## Finding und Auflösung

- **[P2] Deep-Link mit gleichzeitigem Zielgruppen- und Entwurfskontext ist
  nicht gemeinsam nachgewiesen — behoben.**
  `KnowledgeTreeSmokeTests.KnowledgeNode_DirectDeepUrlAndReloadRestoreTheSelectedPathReadOnly`
  belegt einen tiefen Node-Direktaufruf und Reload mit `audienceId`, aber ohne
  `transactionId` (`tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeTreeSmokeTests.cs`).
  `KnowledgePageContextSelectorTests.Reload_WithExplicitDraftInNodeUrl_RestoresThatDraft`
  belegt `audienceId` und `transactionId` bei `/knowledge` ohne `NodeId`
  (`tests/KnowHowToAI.Web.Tests/Features/Knowledge/KnowledgePageContextSelectorTests.cs`).
  Der weitere Working-Node-Test bildet beide Query-Parameter auf den Download
  ab, ohne die geroutete Node-Ansicht oder Reload zu prüfen. Damit war das
  erste M2-Abnahmekriterium für eine geteilte Node-URL mit Zielgruppe und
  Entwurf samt Reload nicht als gemeinsamer Ablauf belegt.

  `KnowledgeDirectEditingSmokeTests.DeepNodeUrlRestoresAudienceAndWorkingDraftAfterReload`
  in `tests/KnowHowToAI.BrowserTests/Transactions/KnowledgeDirectEditingSmokeTests.cs`
  schließt die Lücke gegen den echten gestarteten Host: Der bestehende tiefe
  Seed wird im Browser über den Editor in einer Working Transaction geändert,
  anschließend wird `/knowledge/{NodeId}?audienceId=Default&transactionId=...`
  direkt geöffnet und vollständig neu geladen. Beide Ansichten prüfen
  Knotenauswahl, vollständigen Breadcrumb-Pfad, Zielgruppe, Arbeitskopie,
  aktiven Entwurf und den gespeicherten Working-Inhalt.

  Gezielter Browserlauf: 1/1 bestanden. Die erste Testausführung fand einen
  ungenauen Testlocator für den Root-Titel (Treeitem-Text enthielt auch
  Bedienelemente); die Prüfung wurde auf den Node-Titel begrenzt. Dabei zeigte
  sich kein Produktfehler. Die M2-Roadmap führte M2.2 bereits als geschlossen;
  die widersprechende Aussage zu einem offenen M2.2-Parentstatus war falsch.

## Abschlussnachweis

Keine Produktionscode-Korrektur aus dem Audit abgeleitet. M2.1, M2.2 und M2.3
sind durch ihre Leaf-Nachweise abgeschlossen. Alle drei M2-Abnahmekriterien
sind belegt; der Audit ist bestanden. M2.2 ist geschlossen.

- `pwsh -NoProfile -File scripts/build.ps1`: Exitcode 0.
- Gezielter Browserlauf: 1/1 bestanden.
- `pwsh -NoProfile -File scripts/test-fast.ps1`: 1.135 bestanden (Frontend 5,
  Analyzer 6, Core 419, Integration 286, Web 419).
- AiNetLinter Solution-Gate: Score 10.0, 0 Verstöße.
- `git diff --check` und `git show --format= --check HEAD`: sauber.

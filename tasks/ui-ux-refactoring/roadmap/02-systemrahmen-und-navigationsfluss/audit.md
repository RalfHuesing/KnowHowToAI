# M2-Abschlussaudit – Systemrahmen und Navigationsfluss

## Ergebnis

**Korrekturrunde erforderlich.** Der Audit wurde am 2026-09-21 gegen den
Arbeitsstand `4064683` durchgeführt. Die vier Leaf-Checkboxen sind in der
Milestone-Roadmap geschlossen; zwei M2-Abnahmepunkte bleiben wegen fehlender
Belege offen. Es wurde kein Produktions- oder Testcode geändert.

## Prüfgrundlage und Umfang

- Orchestrierungsrolle und Regeln: `.agents/agent-workflow/03-orchestrierte-umsetzung.md`,
  `.agents/rules/Richtlinien.mdc`, `.agents/rules/DokuRichtlinien.mdc`,
  `.agents/rules/TestRichtlinien.mdc`, `AGENTS.md`.
- M2-Planung, Roadmap und alle vier Leaves einschließlich Abschlussnachweisen.
- Verbindliches UI/UX-Konzept einschließlich Bedienmodell, Nutzerreisen,
  Informationsarchitektur, Layout-/Aktionssystem, Evidenzinventar und
  Entscheidungsregister.
- Betroffene Ist-Dokumentation: `docs/README.md`, `docs/Invarianten.md`,
  `docs/Retrieval.md`, `docs/Transaktionen-und-Historie.md`,
  `docs/Zielgruppen-und-Content.md`, `docs/Architektur.md` sowie das
  Webfrontend-Zielkonzept.
- Commits seit M2-Beginn: `802d102`, `f071d0b`, `4b68276`, `a5a1aba`,
  `823f3b5`, `5f26237`, `4064683`.

## Befunde

### F-01 – M2.0-Kernreise und Sonderzustände nicht als Browser-/Workflow-Nachweis belegt (P1)

- **Fundstellen:** `tasks/ui-ux-refactoring/roadmap/02-systemrahmen-und-navigationsfluss/tasks/M2.0-T1.md:132-142,158`; `tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeTreeSmokeTests.cs:19-97`; `tests/KnowHowToAI.Web.Tests/Features/Knowledge/NodeDetailsPaneTests.cs:107-180`.
- Der Abschlussnachweis nennt `KnowledgeTreeSmokeTests` als repräsentativen
  Browser-Smoke. Dieser Test belegt jedoch nur Dashboard → Wissensbaum,
  Zielgruppenauswahl, Baum-/Breadcrumb-Navigation und Reload. Er betätigt weder
  `Bearbeiten` noch den Dialog, startet oder wählt keine Arbeitskopie und prüft
  nicht den anschließenden Editor derselben Node und Zielgruppe.
- Der vorhandene bUnit-Test belegt nur die Anzeige und Auswahl einer bereits
  kompatiblen Arbeitskopie. Begin-Fehler, erfolgreicher Start, Current-Race,
  direkte Editor-Navigation sowie die Abwesenheit des Einstiegs für Fallback,
  None und historische Kontexte werden dort nicht als M2.0-Arbeitsweg geprüft.
- Damit sind die M2.0-Testbudget-Checkbox und der Milestone-Punkt
  `Wissenseintrag → Bearbeiten → Arbeitskopie wählen/beginnen → gleicher Editor`
  nicht vollständig belegt. Die Implementierung wird an dieser Stelle nicht
  als falsch behauptet; es fehlt der geforderte Nachweis.

### F-02 – Verbindliche Ist-Dokumentation beschreibt den neuen M2.0-Einstieg nicht (P1)

- **Fundstellen:** `docs/Architektur.md:340-381`; `802d102`; Regel
  `.agents/rules/DokuRichtlinien.mdc` (Synchronisation von Verhaltensänderung
  und Ist-Dokumentation im selben Commit).
- `802d102` führt in `NodeDetails`/`NodeDetailsPane` die sichtbare Aktion
  `Bearbeiten`, den Arbeitskopien-Dialog und die Navigation mit derselben
  `NodeId`/Zielgruppe in den Working-Editor ein. Der Commit aktualisiert jedoch
  keine Datei unter `docs/`; auch die späteren M2-Dokumentationscommits
  ergänzen nur Mutation- und Transaction-Führung.
- Der zuständige Architekturabschnitt dokumentiert weiterhin nur die
  Read-only-Darstellung und den Einstieg über bereits aktive Transactions. Die
  neue Current-Read-only → Arbeitskopie → Editor-Führung, ihre Kompatibilitäts-
  und Read-only-Grenzen sowie der bestehende Route-/Query-Vertrag fehlen als
  belegbarer Ist-Zustand. Die M2.0-Checkbox „Dokumentation aktualisiert“ ist
  daher durch den aktuellen Repository-Stand nicht gedeckt.

## Positiv geprüft

- `AudiencesPage` sowie deren CSS-/Testdateien sind in `bedefd6..4064683`
  unverändert; es wurde keine neue Route, kein zweiter fachlicher Einstieg,
  keine Action Registry und kein Stepper eingeführt.
- Die M2.2-Diffs synchronisieren bei Create child/root, Update und Delete die
  bestehende Route mit `NodeId`, `transactionId`, `audienceId` und
  Workspace-Auswahl; die vorhandenen Nachweise decken die vier Mutationspfade
  ab.
- M2.3 führt den vorhandenen `Im Wissensbaum öffnen`-Link im ersten
  Weiterarbeiten-Abschnitt; Zielroute und bestehende Commit-/Discard-/Konflikt-
  Wege bleiben laut Tests und Nachweisen erhalten.
- M2.1 weist Page-Frame, Prosa-Measure, Action-Group, 1280/1920/2560 und
  Responsive über den vorhandenen Browser-Smoke nach; der M1.5-T7-Nachweis
  bleibt Regression. Die `AudiencesPage`-Grenze ist eingehalten.
- Die relative Linkprüfung der auditierten Roadmap-, Leaf- und Konzeptdateien
  ist ohne defekte Ziele durchgelaufen. Der Working Tree war vor der
  Audit-Dokumentation sauber.
- Bereits dokumentierte grüne Build-, Verify- und Testnachweise wurden gemäß
  den Repository-Regeln nicht blind wiederholt; seit diesen Nachweisen wurde
  kein Produktions- oder Testcode geändert.

## Abnahmeentscheidung

Die beiden offenen Befunde erfordern genau eine gezielte Korrekturrunde gemäß
`03-orchestrierte-umsetzung.md`. Bis deren Nachweise und die fehlende
Ist-Dokumentation ergänzt und erneut geprüft sind, wird M2 nicht als
abgeschlossen markiert.

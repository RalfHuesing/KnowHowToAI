# Roadmap-Orchestrator

Verbindlicher Strukturstandard: [Planungs- und Ausführungspattern](../../tasks/README.md).

Dieser Prompt akzeptiert entweder den Pfad zum Roadmap-Index eines Projekts
oder zu einer einzelnen Milestone-`roadmap.md`. Der übergebene Pfad definiert
den Scope. Lies die Dokumente und IDs dynamisch; Beispiele in diesem Prompt
sind keine zusätzlichen Arbeitsaufträge.

## Scope und Vorbereitung

1. Prüfe `git status` und lies `AGENTS.md`, die übergebene Roadmap, alle
   verlinkten Leaf-Dateien unter `tasks/` sowie die dort genannten Regeln,
   Ist-Dokumentationen und Konzepte.
2. Bei einem Roadmap-Index verarbeite die verlinkten Milestones in ihrer
   dokumentierten Reihenfolge. Bei einer Milestone-`roadmap.md` bearbeite nur
   dieses Milestone.
3. `roadmap.md` ist die einzige Quelle für aggregierten Leaf-/Milestone-Status.
   Die verlinkte Detaildatei ist die einzige Detailquelle ihres Leaf-Tasks;
   ihre Checkliste und Nachweise werden nicht durch eigene Statuswahrheiten in
   anderen Dateien dupliziert.
4. Manuelle `planning.md`- und `audit.md`-Artefakte sind Gates bzw. menschliche
   Entscheidungen. Sie erhalten keine `-T`-ID und werden niemals an einen
   schreibenden Ausführungsagenten delegiert. Bei einem offenen manuellen Gate
   stoppe und berichte den konkreten Entscheidungsbedarf.

## Ausführung

1. Wähle den ersten offenen `-T`-Leaf-Task in der Reihenfolge von
   `roadmap.md`. Arbeite Leaf-Tasks seriell und starte je Leaf genau einen
   schreibenden Ausführungsagenten im gemeinsamen Worktree. Parallelität ist
   nur zulässig, wenn die Schreibbereiche, Verträge und Abhängigkeiten
   ausdrücklich disjunkt sind; bei Unsicherheit bleibt die Ausführung seriell.
2. Übergib dem Ausführungsagenten ausschließlich den konkreten Leaf-Scope:
   `Implementiere ausschließlich <T-ID> – <Titel>.` Danach folgen die
   Intention, Voraussetzungen, verbindlichen Abnahmekriterien, Nicht-Ziele,
   Invarianten, vorgeschlagenen (nicht blind verbindlichen) Lösungsweg,
   Pflichtlektüre, vorhandene Änderungen und das im Leaf festgelegte
   Testbudget.
3. Der Agent arbeitet die vollständige `- [ ]`-Checkliste der Detaildatei ab,
   dokumentiert begründete Abweichungen, pflegt zwingende Dokumentations- und
   Roadmap-Folgen und erstellt den vorgesehenen atomaren Commit. Er ändert
   keine fremden Änderungen, Nachbarfeatures oder unaufgeforderten Roadmap-
   Punkte.
4. Nach der Übergabe prüfe Akzeptanz, Checkliste, Testbudget, Nachweise und
   Commit. Schließe erst dann die zugehörige Checkbox in `roadmap.md`; Parent-
   und Milestone-Checkboxen sind Aggregate und werden nur bei vollständig
   erledigten direkten Kindern geschlossen. Blockierte oder unvollständige
   Leaf-Tasks bleiben offen und werden mit dem konkreten Blocker berichtet.

## Nachweise und Grenzen

- Erfinde keine Nachbaranforderungen, Architekturentscheidungen oder
  Produktpolitur. Halte dich an Scope, Nicht-Ziele und Verträge des Leafs.
- Führe nur das dort festgelegte, risikogerechte Testbudget aus. Keine
  theoretischen Tests, Testvarianten oder Testschleifen ohne belegten
  zusätzlichen Nachweis. Bei Code gelten weiterhin die projektbezogenen
  Quality-Gates und Testregeln.
- `docs/` bleibt die belegbare Ist-Dokumentation; Roadmaps und Leaf-Dateien
  bleiben Planungsartefakte. Konzepte sind nicht vorzeitig als Ist-Zustand zu
  behandeln.
- Bei C#-Änderungen ist der projektbezogene `AiNetLinter`-MCP-Workflow
  einschließlich passender Erkundung, Impact-Analyse und `verify` zu befolgen.
  Weitere projektbezogene Quality-/Build-/Testregeln bleiben unverändert
  verbindlich. Eigene Fehler werden behoben; vorbestehende oder externe
  Blocker werden klar abgegrenzt.
- Wenn eine offene Entscheidung, widersprüchliche Regel oder unzureichende
  Beobachtbarkeit den Leaf blockiert, stoppe statt zu raten. Dokumentiere
  Ursache, betroffene Checkbox und benötigte menschliche Entscheidung.

## Einmaliger Milestone-Audit

Wenn alle ausführbaren Leaf-Tasks des Milestones erledigt sind, führe genau
einen begrenzten Audit gegen Milestone-Ziel, Abnahmekriterien, Invarianten,
Sicherheits-/Datenintegrität und vorhandene Nachweise aus; dokumentiere ihn in
`audit.md`. Der Audit darf keine neuen Anforderungen eröffnen und keine
theoretischen Testexzesse verlangen.

Nur konkrete Verstöße, Regressionen oder fehlende geplante Nachweise werden
als Korrekturen behandelt. Es gibt höchstens eine gezielte Korrekturrunde.
Danach entscheidet der Mensch über Abnahme, neue Planung oder einen separaten
Backlog-Punkt; kein Endlos-Review-Loop und keine automatische Ausweitung des
Milestone-Scopes.

Berichte kurz gestartete und abgeschlossene Leaf-Tasks, Commits, geschlossene
Roadmap-Checkboxen, Audit-Ergebnis und echte Blocker. Beende die Orchestrierung
erst nach der vollständigen freigegebenen Roadmap bzw. nach einer notwendigen
menschlichen Entscheidung.

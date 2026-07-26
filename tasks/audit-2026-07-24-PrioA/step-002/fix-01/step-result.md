---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 002/fix-01
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T20:30:00+02:00
code_commit_hash: 84cf2e15e0d2ad4264ccadd2e488f30801d36e0f
# Hinweis: den Commit, der DIESE Datei enthält (Coder-Skill Schritt 7),
# kann diese Datei denknotwendig nicht selbst zitieren — bei Bedarf per
# `git log --follow -- <Pfad-dieser-Datei>` nachschlagen.
status_after: done
---

# Result Step 002/fix-01: AiNetLinter `AvoidExcessiveMiddleMen` in `BuildLikePatternTests` beheben + `step-result.md` Z.84 korrigieren

## Zusammenfassung

`BuildLikePatternTests` wurde von 7 `[Fact]`-Methoden auf 2 `[Theory]`-Methoden mit insgesamt 7 `[InlineData]`-Datensätzen refaktoriert. Damit liegt die Methoden-Anzahl der Klasse (2) unter `MiddleManMinMemberCount: 5` und die AiNetLinter-Regel `AvoidExcessiveMiddleMen` greift nicht mehr — der `lint-report.md` zeigt nach dem Fix `OK` statt der vorherigen 1 Violation in `BuildLikePatternTests.cs:5`. Die Test-Coverage ist inhaltlich identisch (alle 7 Inputs werden weiterhin geprüft, nur als Inline-Data statt als separate Methoden), `dotnet test` bleibt 72/72 grün. Zusätzlich wurde die faktisch falsche Lint-Aussage in `step-002/step-result.md:84` präzisiert, sodass der Befund konsistent zum AiNetLinter-Report dokumentiert ist.

## Geänderte Dateien

- `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs` — 7 `[Fact]`-Methoden ersatzlos gestrichen und durch 2 `[Theory]`-Methoden ersetzt: `BuildLikePattern_EscapesSqlWildcardsAndWraps` (5 Inline-Datensätze für den einfachen Escape-Pfad inkl. Empty-Input) und `BuildLikePattern_PreservesEscapingOrder` (2 Inline-Datensätze für die Escape-Reihenfolge bzw. Mehrfach-Wildcards). Methoden-Anzahl 2 < `MiddleManMinMemberCount: 5` → AiNetLinter-Verstoß behoben.
- `tasks/audit-2026-07-24-PrioA/step-002/step-result.md` — Z.84 korrigiert: aus „grün, 0 neue Verstöße" wurde „grün (Exit 0), aber Report zeigt 1 Violation in `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs:5` — wird in step-002/fix-01 behoben". Die ursprüngliche Behauptung widersprach dem `lint-report.md` desselben Laufs (siehe `step-review.md` Z. 140-144).

## Commit

- **Code-Commit-Hash:** `84cf2e15e0d2ad4264ccadd2e488f30801d36e0f`
- **Message:**
  ```
  fix(test): like-pattern-tests von 7 facts auf 2 theories konsolidieren

  Die 7 [Fact]-Methoden in BuildLikePatternTests waren reine Forwards auf
  SqlDocumentsStore.BuildLikePattern und loesten damit die AiNetLinter-Regel
  AvoidExcessiveMiddleMen aus (100% Forwarding-Ratio ueberschritt die
  60%-Schwelle bei MiddleManMinMemberCount=5). Konsolidierung zu 2
  [Theory]-Methoden mit insgesamt 7 [InlineData]-Datensaetzen:
  Coverage numerisch identisch, Methoden-Anzahl unter dem Threshold,
  AiNetLinter-Report jetzt 0 Violations.

  Zusaetzlich step-002/step-result.md Z.84 korrigiert: die Behauptung
  '0 neue Verstoesse' widersprach dem lint-report und wird praezisiert auf
  'gruen (Exit 0), aber Report zeigt 1 Violation ... wird in fix-01 behoben'.

  Refs: tasks/audit-2026-07-24-PrioA/step-002/fix-01
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```
- **Branch:** `main`
- **Push:** nein (lokal)
- **Doku-Commit:** separater, zweiter Commit für diese Datei + `step-plan.md` (Status) — dessen Hash steht nicht hier drin (Selbstbezug), sondern in `git log`.

## Build-Output

```
dotnet build -c Release
→ 0 Warnung(en), 0 Fehler
→ 3 Projekte (Core, Cli, Tests) erfolgreich gebaut in ~11.5s
```

## Test-Output

```
dotnet test -c Release --no-build
→ Total: 72, Errors: 0, Failed: 0, Skipped: 0
→ 72/72 grün (Baseline 65 + 7 BuildLikePatternTests-Cases als 2 Theories = 72 gesamt)
→ Anzahl der Inline-Datensätze: 7 (5 in BuildLikePattern_EscapesSqlWildcardsAndWraps + 2 in BuildLikePattern_PreservesEscapingOrder) — identisch zu vorher 7 [Fact]-Methoden
→ AiNetLinterTest (filter-method "*AiNetLinterTests*"): grün (1/1), lint-report.md nach Lauf: "OK" (0 Violations, vorher 1 Violation in BuildLikePatternTests.cs:5)
```

## Abweichungen vom Plan

Keine — Plan 1:1 umgesetzt. Methoden-Anzahl, Reihenfolge der Inline-Daten (entsprechend der ursprünglichen `[Fact]`-Reihenfolge), Methoden-Body als Expression-Body (`=>`), unveränderte `using`-Imports und Namespace — alles wie im `step-plan.md` Code-Skizze und Notes vorgegeben.

## Beobachtungen

- **`LintRun_ReportsNoViolations` prüft weiterhin nur den Exit-Code, nicht den Violation-Count** — die strukturelle Ursache für Finding 1 (Test grün obwohl Report 1 Violation zeigt) bleibt im Test-Code bestehen. Im Plan als „Bekannte Ausnahme" markiert und explizit nicht in diesem Fix-Scope (würde Test-Logik-Änderung erfordern). Vorschlag für Step 003+ oder einen separaten Hygiene-Step: Assertion `report.Contains("OK")` bzw. `!report.Contains("AvoidExcessiveMiddleMen")` ergänzen, sodass der Test die Report-Semantik mit absichert.
- **`docs/04:50` Typo `]-`Klammer`** — bei der Doku-Korrektur in `step-result.md` mit-gesehen, aber bewusst nicht mit-angefasst (out of scope dieses Fixes per Plan-Vorgabe). Bleibt für einen Folge-Hygiene-Step vorgemerkt.
- **Commit-Subject-Länge** — der Code-Commit-Subject hat 65 Zeichen (≤ 72), Audit-Beobachtung 1 damit für *diesen* Fix erledigt. Die Beobachtung 1 des Step-002-Reviews bezog sich auf den Step-002-Commit `a9e4140` (77 Zeichen) und ist als historisch nicht mehr änderbar markiert.
- **`BuildLikePattern`-Klasse nicht angefasst** — kein Refactor an `SqlDocumentsStore.BuildLikePattern` oder `SearchDocsAsync` (war nicht im Scope, der Code-Commit diff bestätigt: nur die zwei genannten Dateien geändert).

## Bekannte Unschärfen

- **Test-Discovery-Verhalten bei `[Theory]`-Refactor:** xUnit v3 zeigt `[Theory]`-Tests in der Regel als einen Eintrag pro Methode mit mehren Test-Cases an. Sollte der Auditer beim Test-Output nachzählen, sind 2 `[Theory]`-Methoden mit insgesamt 7 Cases (nicht 9 Einzeleinträge) — das ist erwartetes Verhalten und entspricht der Coverage.
- **Lint-Report-Zeitstempel:** Der `lint-report.md` trägt jetzt den Run-Zeitstempel `2026-07-26 18:54:41` (vom letzten `AiNetLinterTests`-Lauf). Der Report selbst ist nicht im Git-Repo (gehört zu `AiNetLinter/output/` und wird vom Test zur Laufzeit erzeugt/überschrieben — siehe `AiNetLinterTests.cs:31`), d.h. der `git diff` zeigt diese Datei nicht. Verifikation der 0-Violation-Eigenschaft erfolgt per `dotnet test -- --filter-method "*AiNetLinterTests*"` (Exit 0) und per Lesen der generierten Report-Datei.

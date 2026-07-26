---
status: done
type: step-plan
task: audit-2026-07-24-PrioA
step: 002/fix-01
title: "Fix-01 — AiNetLinter AvoidExcessiveMiddleMen in BuildLikePatternTests beheben + step-result.md korrigieren"
estimated_risk: low
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T20:00:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/step-002/step-review.md#findings-bei-issues"
---

# Step 002/fix-01: AiNetLinter-Verstoß `AvoidExcessiveMiddleMen` beheben + `step-result.md` korrigieren

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `tasks/audit-2026-07-24-PrioA/step-002/step-review.md` — Abschnitt „Findings (bei `issues`)", Findings 1 und 2
- **Trigger:** Auditer-Verdict `issues` für Step 002 am 2026-07-26 (`reviewed_at: 2026-07-26T19:55:00+02:00`)
- **Phase / Priorität:** Sofort (Fix-Loop-Blocker) — verhindert die Freigabe von Step 002 und blockiert damit den 360°-Audit-Abschluss

## Intention

Step 002 triggert die AiNetLinter-Regel `AvoidExcessiveMiddleMen`, weil alle 7 `[Fact]`-Methoden in `BuildLikePatternTests` reine One-Line-Forwards auf `SqlDocumentsStore.BuildLikePattern` sind — Forwarding-Ratio 100% > 60% Threshold (`MaxMiddleManForwardingRatio: 0.6`, `MiddleManMinMemberCount: 5` aus `tests/.../AiNetLinter/rules/KnowHowToAI.rules.json:62-65`). Nach diesem Fix ist die Klasse mit 2 `[Theory]`-Methoden unter `MiddleManMinMemberCount: 5` und die Regel nicht mehr anwendbar. Gleichzeitig wird die faktisch falsche Lint-Aussage in `step-002/step-result.md:84` korrigiert, sodass der Befund konsistent zum AiNetLinter-Report dokumentiert ist. Die Test-Coverage bleibt inhaltlich identisch (alle 7 Inputs weiterhin geprüft, nur als Inline-Data statt als separate Methoden).

## Konkrete Änderungen

### Datei 1: `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs`

- **Was:** Komplette Refaktorierung der Test-Klasse von 7 `[Fact]`-Methoden auf 2 `[Theory]`-Methoden mit `[InlineData]`. Konkret:
  - Methode `BuildLikePattern_AllowsNormalSubstring` (Z. 7-9) wird ersatzlos gestrichen.
  - Methode `BuildLikePattern_EscapesPercent` (Z. 11-13) wird ersatzlos gestrichen.
  - Methode `BuildLikePattern_EscapesUnderscore` (Z. 15-17) wird ersatzlos gestrichen.
  - Methode `BuildLikePattern_EscapesOpeningBracket` (Z. 19-21) wird ersatzlos gestrichen.
  - Methode `BuildLikePattern_EmptyInput_ReturnsPercentPercent` (Z. 23-25) wird ersatzlos gestrichen.
  - Methode `BuildLikePattern_OrderOfEscapesDoesNotDoubleEscape` (Z. 27-29) wird ersatzlos gestrichen.
  - Methode `BuildLikePattern_AllThreeWildcardsInOneInput_AllEscaped` (Z. 31-33) wird ersatzlos gestrichen.
  - Stattdessen genau zwei `[Theory]`-Methoden einfügen — siehe Code-Skizze unten.
  - Reihenfolge der `[InlineData]`-Zeilen folgt dem Original-Test-Set, damit die Diff-Historie nachvollziehbar bleibt.
- **Warum:** Mit 2 Methoden ist `MiddleManMinMemberCount: 5` (`KnowHowToAI.rules.json:64`) nicht erreicht → Linter-Regel `AvoidExcessiveMiddleMen` greift nicht. Zusätzlich entspricht das der bestehenden Test-Konvention im Projekt (`EnumParseHelpersTests.cs:9-26`, `SqlIdentifierValidatorTests.cs:7-23`).
- **Edge-Case „leerer Input":** bleibt erhalten als `[InlineData("", "%%")]` — der `string.Empty`-Test aus dem Original darf nicht verloren gehen.
- **Edge-Case „kombinierte Wildcards":** die bisherige separate Methode `AllThreeWildcardsInOneInput_AllEscaped` wird in den zweiten `[Theory]` integriert (gruppiert mit `OrderOfEscapesDoesNotDoubleEscape`, weil beide die Escape-Reihenfolge bzw. das Verhalten bei Mehrfach-Sonderzeichen verifizieren).

### Datei 2: `tasks/audit-2026-07-24-PrioA/step-002/step-result.md`

- **Was:** Zeile 84 korrigieren.
  - **Aktuell** (Z. 84):
    ```
    → AiNetLinterTest (Lauf gegen 7 neue/erweiterte Dateien): grün, 0 neue Verstöße
    ```
  - **Neu** (exakter Wortlaut):
    ```
    → AiNetLinterTest (Lauf gegen 7 neue/erweiterte Dateien): grün (Exit 0), aber Report zeigt 1 Violation in tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs:5 — wird in step-002/fix-01 behoben
    ```
  - Hinweis: Der Linter-Test `AiNetLinterTests.LintRun_ReportsNoViolations` prüft nur den Exit-Code, nicht den Violation-Count. Diese Eigenschaft ist die Ursache, warum der Test grün bleibt obwohl der Report 1 Violation listet — das gehört zur Klarstellung mit in den Wortlaut.
- **Warum:** Die Behauptung „0 neue Verstöße" widerspricht dem `lint-report.md` desselben Laufs (`step-review.md` Zeile 140-144). Der Befund ist die Grundlage für Finding 1; ohne Korrektur bleibt der Selbstwiderspruch im Repo stehen.

## Tests

- [ ] `BuildLikePattern_EscapesSqlWildcardsAndWraps` (neuer `[Theory]`) — alle 5 Inline-Datensätze grün:
  - `("routing", "%routing%")`
  - `("50%", "%50[%]%")`
  - `("a_b", "%a[_]b%")`
  - `("[abc", "%[[]abc%")`
  - `("", "%%")`
- [ ] `BuildLikePattern_PreservesEscapingOrder` (neuer `[Theory]`) — beide Inline-Datensätze grün:
  - `("[%]", "%[[][%]]%")`
  - `("%a_b[c]", "%[%]a[_]b[[]c]%")`
- [ ] Keine Verhaltensänderung an `SqlDocumentsStore.BuildLikePattern` — Coverage bleibt numerisch identisch (7 Inputs werden weiterhin geprüft, nur als Inline-Data statt als separate `[Fact]`-Methoden).
- [ ] `dotnet test` insgesamt weiterhin **72/72 grün** (Baseline vor diesem Fix = 72).
- [ ] `dotnet test -- --filter-method "*AiNetLinterTests*"` weiterhin grün (Regression-Check für den Linter-Wrapper).

## Definition of Done

- [ ] Beide „Konkrete Änderungen" umgesetzt (Refactor + Zeile 84 in `step-result.md`).
- [ ] `dotnet build -c Release` — 0 Warnings, 0 Errors.
- [ ] `dotnet test --no-build -c Release` — 72/72 grün (keine Test-Anzahl-Änderung, nur Methoden-Refactor).
- [ ] AiNetLinter-Report zeigt **0 Violations** (`tests/.../AiNetLinter/output/lint-report.md` Stand: nach dem Fix). Verifikation: `git diff <vorher-commit> <nachher-commit> -- tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md` zeigt den Wegfall der `BuildLikePatternTests.cs:5`-Eintragung.
- [ ] Commit mit Subject `test(core): BuildLikePatternTests auf [Theory] refaktoriert (AiNetLinter-Konform)`, Body erklärt: 7 `[Fact]`-Methoden waren reine Forwards → 100% Forwarding-Ratio überschritt 60% `MaxMiddleManForwardingRatio`-Schwelle; jetzt 2 `[Theory]`-Methoden mit insgesamt 7 Inline-Datensätzen → Coverage identisch, Methoden-Anzahl unter `MiddleManMinMemberCount: 5`. Zudem Korrektur von `step-result.md` Z. 84 (AiNetLinter-Aussage präzisiert). Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`.
- [ ] `tasks/audit-2026-07-24-PrioA/step-002/fix-01/step-result.md` geschrieben mit Commit-Hash.
- [ ] `status` in dieser `step-plan.md` von `open` auf `done (pending audit)` gesetzt (nicht der Coder — der Planer bleibt bei `open`, bis der Auditer das Re-Audit abgeschlossen hat; siehe Coder-Skill-Konvention für Status-Pflege durch den Coder).

## Rules-Refs

- `.agents/rules/AiNetLinter.mdc` — Verstoß gegen `AvoidExcessiveMiddleMen` mit `MaxMiddleManForwardingRatio: 0.6` und `MiddleManMinMemberCount: 5` (konkret konfiguriert in `tests/KnowHowToAI.Core.Tests/AiNetLinter/rules/KnowHowToAI.rules.json:62-65`).
- `.agents/rules/01-code-style.mdc` — `public class` bleibt unverändert (Tests-Klassen sind nicht `sealed`, das ist die etablierte Konvention im Projekt, siehe `EnumParseHelpersTests.cs:7`, `SqlIdentifierValidatorTests.cs:5`).
- `.agents/rules/02-testing.mdc` — Tests bleiben xUnit v3, alle Inline-Daten äquivalent zu vorher, keine Test-Anzahl-Änderung in der Gesamtbilanz.
- `.agents/rules/03-git-workflow.mdc` — Conventional Commit `test(core):`, deutsch-imperativ, Trailer wie in `step-002`-Commit.

## Bekannte Ausnahmen

- **AiNetLinter-Test `LintRun_ReportsNoViolations` prüft nur Exit-Code, nicht Violation-Count:** Das ist die Wurzel des Finding-1-Verstoßes — der Test meldet grün, obwohl der Report 1 Violation zeigt. Der Fix räumt die Report-Seite auf (durch Refactor); die Test-Lücke selbst (Exit-Code-only-Check) bleibt für Step 003+ als Beobachtung stehen und ist nicht Scope dieses Fixes (würde eine Test-Logik-Änderung erfordern, die über Findings 1+2 hinausgeht).

## Code-Skizze

```csharp
using KnowHowToAI.Core.Sync;

namespace KnowHowToAI.Core.Tests;

public class BuildLikePatternTests
{
    [Theory]
    [InlineData("routing", "%routing%")]
    [InlineData("50%", "%50[%]%")]
    [InlineData("a_b", "%a[_]b%")]
    [InlineData("[abc", "%[[]abc%")]
    [InlineData("", "%%")]
    public void BuildLikePattern_EscapesSqlWildcardsAndWraps(string input, string expected) =>
        Assert.Equal(expected, SqlDocumentsStore.BuildLikePattern(input));

    [Theory]
    [InlineData("[%]", "%[[][%]]%")]
    [InlineData("%a_b[c]", "%[%]a[_]b[[]c]%")]
    public void BuildLikePattern_PreservesEscapingOrder(string input, string expected) =>
        Assert.Equal(expected, SqlDocumentsStore.BuildLikePattern(input));
}
```

## Tech-Stack-Notiz (aus `step-002/step-plan.md` übernommen, gilt weiterhin)

- **Sprache / Framework:** C# / .NET 10, xUnit v3
- **Build:** `dotnet build -c Release` — Erwartung 0 Warnings, 0 Errors
- **Test:** `dotnet test --no-build -c Release` — Baseline 72 Tests (vor diesem Fix), 72/72 grün
- **Lint:** AiNetLinter (`tests/KnowHowToAI.Core.Tests/AiNetLinter/`), ausgeführt via `dotnet test -- --filter-method "*AiNetLinterTests*"`. Report unter `tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md`. Aktuell relevante Regel: `AvoidExcessiveMiddleMen` (Fix-Ziel).
- **Commit-Sprache:** Conventional Commits, deutsch-imperativ, Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`

## Notes

- **Scope-Disziplin:** Die 3 „Sonstigen Beobachtungen" aus `step-review.md` (Commit-Subject 77 Zeichen, Doku-Typo `]-`Klammer` in `docs/04:50`, `DocsMcpTools` Primitive Obsession) bleiben **bewusst out of scope** dieses Fixes. Begründungen:
  - **Commit-Subject 77 Zeichen:** Auditer hat Soft-Verstoß mit Repo-Präzedenz (Commit `02fef83` ist 99 Zeichen) selbst als „nicht im Scope eines Fix-Steps" markiert. Der zurückliegende Commit ist historisch und nicht mehr änderbar.
  - **Doku-Typo `]-`Klammer` in `docs/04:50`:** Sicht-Typo, kein Verhaltens-Issue. Die Orchestrator-Vorgabe erlaubt zwar die Mit-Aufnahme in diesen Fix-Commit, aber strikte Scope-Disziplin (skill: „Plane **ausschließlich** die in „Findings" gelisteten Punkte") und der Wunsch, die Diff-Surface fokussiert zu halten (Test-Refactor + Result-Doc-Korrektur in zwei thematisch eng verbundenen Stellen), sprechen gegen eine Mit-Aufnahme. Beobachtung bleibt für künftigen 360°-Audit oder Hygiene-Step dokumentiert.
  - **`DocsMcpTools` Primitive Obsession:** Auditer hat explizit „out of scope für Step 002" markiert mit Empfehlung an Step 003. Nicht in diesem Fix.
- **Warum zwei `[Theory]`-Methoden und nicht eine:** Die Original-Tests gruppieren sich logisch in „einfache Einzel-Escapes" (5 Tests) und „Escape-Reihenfolge/Mehrfach-Wildcards" (2 Tests). Diese Trennung erhält die semantische Aussage der Test-Namen — `EscapesSqlWildcardsAndWraps` für den einfachen Pfad, `PreservesEscapingOrder` für den Edge-Case-Pfad. Eine einzelne `[Theory]`-Methode würde die Trennung der beiden Verhaltensaspekte verwischen.
- **Warum der Methoden-Body ein Expression-Body (`=>`) bleibt:** Konsistent mit dem Original und mit der bestehenden Test-Konvention (`EnumParseHelpersTests.cs:13`, `SqlIdentifierValidatorTests.cs:12`). Kein Umbau auf Block-Body nötig.
- **Reihenfolge der `[InlineData]`:** Folgt der Reihenfolge der Original-`[Fact]`-Methoden, damit `git blame` und `git log -p` den Übergang von `[Fact]` zu `[Theory]` nachvollziehbar machen.
- **Keine Änderung an `using`-Imports:** `using KnowHowToAI.Core.Sync;` bleibt, weil `SqlDocumentsStore` weiterhin der einzige extern referenzierte Typ ist. `[Theory]` und `[InlineData]` werden durch das bereits vorhandene globale `using Xunit;` (aus dem Test-Projekt) aufgelöst.

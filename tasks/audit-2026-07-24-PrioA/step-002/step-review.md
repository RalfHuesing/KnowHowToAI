---
status: done (pending audit)
type: step-review
task: audit-2026-07-24-PrioA
step: 002
reviewed_by: auditer
reviewed_by_model: MiniMax-M3
reviewed_by_model_knowledge_cutoff: 2026-01
reviewed_at: 2026-07-26T19:55:00+02:00
verdict: issues
---

# Review Step 002: F-SE-001 — LIKE-Wildcard-Injection + Längen-Cap

## Verdict

- [ ] **approved** — alle drei Prüfebenen ok
- [x] **issues** — Fix-Step `step-002/fix-01/` mit Fix-Plan nötig (siehe Findings)
- [ ] **blocked** — Nutzer-Entscheidung nötig (siehe Frage unten)

## Geprüft

- [x] Plan-Erfüllung: alle im `step-plan.md` genannten Änderungen erfolgt (mit dokumentierten Abweichungen)
- [x] Rules-Konformität: `.agents/rules/**` eingehalten — **mit einem Verstoß** (siehe Findings)
- [x] Logische Korrektheit: Code macht was er soll, Tests sind echt (nicht trivial grün)
- [x] Build: selbst nachgeprüft, grün
- [x] Tests: selbst nachgeprüft, 72/72 grün
- [x] Lint: `AiNetLinterTests` grün, aber **Report zeigt 1 neue Verletzung** (siehe Findings)

## Befund

### Plan-Erfüllung

| # | Plan-Punkt | Status | Evidenz / Hinweis |
|---|---|---|---|
| 1 | `KnowHowToAiOptions.cs` — `Search`-Property | ✅ erfüllt | `src/KnowHowToAI.Core/Configuration/KnowHowToAiOptions.cs:13` |
| 2 | `KnowHowToAiSearchOptions.cs` (neu) — `sealed record`, `MaxQueryLength=200`, `MaxResults=50` | ✅ erfüllt | `src/KnowHowToAI.Core/Configuration/KnowHowToAiSearchOptions.cs:4-7` |
| 3 | `SqlDocumentsStore.cs` — `BuildLikePattern` `internal static` mit Bracket-Escape, `SearchDocsAsync` mit Längen-Cap | ✅ erfüllt | `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:79-109` |
| 4 | `KnowHowToAI.Core.csproj` — kein neuer `InternalsVisibleTo` (war schon da) | ✅ erfüllt | `git diff a9e4140^ a9e4140 -- src/KnowHowToAI.Core/KnowHowToAI.Core.csproj` → leer; `KnowHowToAI.Core.csproj:16` schon vorhanden |
| 5 | `appsettings.json` — `Search`-Block | ✅ erfüllt | `src/KnowHowToAI.Cli/appsettings.json:15-18` |
| 6 | `docs/04` Abschnitt 1 — LIKE-Semantik + Längen-Cap | ✅ erfüllt | `docs/04-Datenmodell-Validierung-Edgecases.md:50-52` (kleiner Typo, siehe Beobachtungen) |
| 7 | `docs/03` Abschnitt 2 — JSON-Beispiel + `Search`-Beschreibung | ✅ erfüllt | `docs/03-Projektstruktur-und-Konfiguration.md:71-74, 83` |
| 8 | `docs/02` Abschnitt 4.D — `search_docs`-Tool-Block | ✅ erfüllt | `docs/02-Architektur-und-Techstack.md:114` |
| 9 | `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs` (neu) | ✅ erfüllt | 7 Tests, alle grün |

**Coder-Abweichungen (alle akzeptabel oder bereits in `result.md` dokumentiert):**

- **(a) `DocsMcpTools.cs` + `Program.cs` mit-aktualisiert:** Notwendig, weil die
  `SearchDocsAsync`-Signatur-Erweiterung sonst den einzigen Aufrufer bricht.
  Coder hat explizit eine `AddSingleton<DocsMcpTools>(sp => ...)` Factory
  registriert (Zeile `Program.cs:131-134`), weil `ActivatorUtilities` keine
  `int`-Primitives auflöst. Plausibel, Build ist grün. Smell-Charakter
  (Primitive Obsession — `int maxQueryLength` statt `KnowHowToAiSearchOptions`
  als Parameter) siehe Beobachtungen.

- **(b) Test-Erwartung korrigiert:** Plan-Erwartung `BuildLikePattern("[%]")` → `"%[[]%[]]%"` ist **falsch** (Plan-Tippfehler). Coder-Erwartung `"%[[][%]]%"` ist **korrekt**. Per PowerShell-Reproduktion verifiziert:
  ```
  Input: [%]
  After [->[[]]: [[]%]
  After %->[%]: [[][%]]
  Final: %[[][%]]%
  ```
  Der Plan-Hinweis im selben Bullet („wenn `[` *zuletzt* ersetzt würde, käme `%[[%]]%` raus") passt zur korrekten Erwartung — die Erwartung selbst war der Tippfehler. Coder hat richtig gehandelt.

- **(c) Commit-Subject 77 Zeichen:** Verletzt `03-git-workflow.mdc` `< 70 Zeichen` um 7 Zeichen. **Präzedenz:** Commit `02fef83` (`docs(rules): regeln fuer agenten-temp-verzeichnis in 07-environment-powershell.mdc und .gitignore ergaenzt`) ist 99 Zeichen lang. Damit existiert im Repo ein dokumentierter 99-Zeichen-Subject — die 70-Zeichen-Regel wird im Projekt pragmatisch gelebt, nicht strikt. **Einschätzung:** Soft-Verstoß, nicht im Scope eines Fix-Steps. (Siehe Beobachtungen.)

- **(d) Test-Datei flach:** Konsistent mit Step-001-Konvention (alle Tests flach). ✅

- **(e) 7 Tests statt 6:** Orchestrator-Prompt listete 6 konkrete Test-Namen, der Plan listete 7 (inkl. `AllThreeWildcardsInOneInput_AllEscaped`). Coder lieferte 7 — entspricht dem Plan und ergänzt einen leeren-Input-Test (sinnvoller Edge-Case). ✅

### Rules-Konformität

| Regel | Status | Evidenz |
|---|---|---|
| `01-code-style.mdc`: `sealed`, Early Returns, keine Kommentare | ✅ | `KnowHowToAiSearchOptions` ist `sealed record`; `BuildLikePattern` `internal static`; `SearchDocsAsync` Early Returns (Zeilen 81, 82-87); Kommentar in Zeile 3 von `KnowHowToAiSearchOptions.cs` ist konsistent mit bestehender Konvention in `KnowHowToAiValidationOptions.cs:3` und `KnowHowToAiLoggingOptions.cs:3-4` |
| `02-testing.mdc`: Tests im selben Commit, xUnit v3 | ✅ | `a9e4140` enthält `BuildLikePatternTests.cs` (verifiziert via `git show a9e4140 --stat`) |
| `03-git-workflow.mdc`: Conventional Commit, deutsch, Imperativ, Trailer | ⚠️ | Trailer vorhanden, Typ korrekt (`fix(security)`), deutsch. **Subject 77 Zeichen** statt `< 70` — Soft-Verstoß, Präzedenz im Repo (siehe Plan-Erfüllung (c)) |
| `05-documentation.mdc`: Doku im selben Commit wie Code | ✅ | `docs/02`, `docs/03`, `docs/04` alle in `a9e4140` |
| `06-configuration.mdc`: keine Magic-Werte im Code | ✅ | `MaxQueryLength=200`/`MaxResults=50` sind **Defaults in der `sealed record` Property-Init** (konfigurierbar via JSON) — passt zur Konvention. `BuildLikePattern`/`SearchDocsAsync` enthalten **keine** Magic-Literale |
| `AiNetLinter.mdc`: Methoden ≤ 60 LOC, sealed, etc. | ❌ | `BuildLikePatternTests.cs:5` triggert **`AvoidExcessiveMiddleMen`** — 7/7 Methoden sind reine Forwards, 100% > 60% Threshold (Details siehe Findings 1) |

### Logische Korrektheit

**Eigene Verifikation der Bracket-Escape-Logik** (PowerShell, Input `%a_b[c]`):
```
Input:    %a_b[c]
[→[[]:   %a_b[[]c]
%→[%]:   [%]a_b[[]c]
_→[_]:   [%]a[_]b[[]c]
Wrap:    %[%]a[_]b[[]c]%
```
→ Matched die Test-Erwartung in `BuildLikePattern_AllThreeWildcardsInOneInput_AllEscaped`. ✅

**Verhalten bei Whitespace-Query:** `string.IsNullOrWhiteSpace(query)` greift bei `""`, `" "`, `"\t"`, `"\n"`. Plan-Konformität gewahrt. ✅

**Off-by-one bei `query.Length == maxQueryLength`:** Plan benutzt `>`, also `==` ist erlaubt. Konsistent mit „max 200 Zeichen". ✅

**`ArgumentException` mit `nameof(query)`:** Korrekt — `paramName` ist `"query"`. ✅

**Backward Compatibility / DI-Setup:** `DocsMcpTools` Primary-Constructor um `int maxQueryLength` erweitert (`DocsMcpTools.cs:12`); `Program.cs:131-134` registriert explizit per Factory, die `KnowHowToAiOptions.Search.MaxQueryLength` aus DI auflöst. `KnowHowToAiOptions` ist in `Program.cs:129` als Singleton registriert. Konsistent über alle 4 Run-Modi (nur `RunServer` nutzt `DocsMcpTools`; `RunValidate`/`RunImport`/`RunExport` brauchen `search_docs` nicht). ✅

**Tests vs. Plan-Erwartungen (alle 7 per Hand nachgerechnet):**
| Test | Input | Erwartet | Tatsächlich (PowerShell) | OK? |
|---|---|---|---|---|
| `AllowsNormalSubstring` | `routing` | `%routing%` | `%routing%` | ✅ |
| `EscapesPercent` | `50%` | `%50[%]%` | `%50[%]%` | ✅ |
| `EscapesUnderscore` | `a_b` | `%a[_]b%` | `%a[_]b%` | ✅ |
| `EscapesOpeningBracket` | `[abc` | `%[[]abc%` | `%[[]abc%` | ✅ |
| `EmptyInput_ReturnsPercentPercent` | `""` | `%%` | `%%` | ✅ |
| `OrderOfEscapesDoesNotDoubleEscape` | `[%]` | `%[[][%]]%` | `%[[][%]]%` | ✅ |
| `AllThreeWildcardsInOneInput_AllEscaped` | `%a_b[c]` | `%[%]a[_]b[[]c]%` | `%[%]a[_]b[[]c]%` | ✅ |

**SearchDocsAsync ohne direkten Test:** Plan merkt explizit an, dass diese Validierung DB-abhängig ist (Backlog F-TS-001). Visuell prüfbar (5 Zeilen), Coder hat die `internal static`-Extraktion nicht gemacht — wäre eine zusätzliche Indirektion für 5 Zeilen trivialer Logik. Vertretbar.

### Build-Status

```
dotnet build -c Release
→ 0 Warnung(en), 0 Fehler
→ 3 Projekte (Core, Cli, Tests) erfolgreich gebaut in ~2.5s
```

### Test-Status

```
dotnet test --no-build -c Release
→ gesamt: 72, fehlgeschlagen: 0, erfolgreich: 72, übersprungen: 0
→ Dauer: 8.8s

dotnet test --no-build -c Release -- --filter-method "*BuildLikePattern*"
→ 7/7 grün
```

### Lint-Status

```
dotnet test --no-build -c Release -- --filter-method "*AiNetLinterTests*"
→ 1/1 grün (exit code 0)

AiNetLinter-Report (tests/.../AiNetLinter/output/lint-report.md, Stand 2026-07-26 18:38:25):
→ 1 violation, 0 Prod, 1 Tests
→ Datei: tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs, Z.5
→ Regel: AvoidExcessiveMiddleMen (100% > 60%)
```

**Achtung:** Der Linter-Test `AiNetLinterTests.LintRun_ReportsNoViolations` prüft nur den Exit-Code (0), nicht den Violation-Count. Mit 1 Violation meldet der Linter trotzdem Exit 0 → der Test bleibt grün, **aber der Report zeigt die Verletzung**. Die Coder-Aussage in `step-result.md` Zeile 84 (`AiNetLinterTest... 0 neue Verstöße`) ist **faktisch falsch**.

## Findings (bei `issues`)

### 1. `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs:5` — AiNetLinter-Verstoß `AvoidExcessiveMiddleMen` + falsche Coder-Aussage

**Befund:** Die Testklasse hat 7 `[Fact]`-Methoden, jede ist ein reiner One-Liner `Assert.Equal` nach `SqlDocumentsStore.BuildLikePattern(...)`. Der Linter wertet das als 7/7 = 100% forwarding > 60% Threshold → Verstoß.

**Verifiziert:**
- `git show a9e4140 -- tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs` → Datei hinzugefügt in `a9e4140` (Step-002-Commit).
- Andere Testklassen mit ähnlicher Methodenanzahl (`DocsValidatorTests` mit 10, `FrontMatterParserTests` mit 7) triggern die Regel **nicht**, weil sie Helper/Felder/IDisposable haben, die das Verhältnis drücken. `BuildLikePatternTests` hat nichts dergleichen.
- Plan hatte explizit `[Theory]` + `[InlineData]` vorgeschlagen (`step-plan.md` Zeile 230). Mit 3-4 `[Theory]`-Methoden (statt 7 `[Fact]`-Methoden) wäre die Methoden-Anzahl unter `MiddleManMinMemberCount: 5` → keine Verletzung.
- `step-result.md` Zeile 84 behauptet „0 neue Verstöße" — die `lint-report.md` (gleicher Lauf) zeigt **1 Violation**. Faktisch falsch.

**Fix:** Refactor von 7 `[Fact]` zu 3-4 `[Theory]` + `[InlineData]`, z.B.:
```csharp
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
```
→ 2 `[Theory]`-Methoden, beide unter `MiddleManMinMemberCount: 5` → keine Linter-Verletzung. Außerdem entspricht das der bestehenden Test-Konvention (`EnumParseHelpersTests.cs`, `SqlIdentifierValidatorTests.cs` benutzen beide `[Theory]`).

**Zusätzlich:** `step-result.md` Zeile 84 korrigieren — die Behauptung „0 neue Verstöße" ist nicht haltbar.

## Frage an Nutzer (bei `blocked`)

(nicht zutreffend — Verdict ist `issues`)

## Sonstige Beobachtungen (nicht als Issues zu werten)

1. **Commit-Subject 77 Zeichen** (`fix(security): like-wildcard-injection und query-laengen-cap fuer search_docs`) über der `03-git-workflow.mdc`-Regel `< 70 Zeichen`. **Präzedenz im Repo:** Commit `02fef83` ist 99 Zeichen lang. Die Regel wird im Projekt pragmatisch gelebt, nicht strikt durchgesetzt. Soft-Verstoß, nicht im Scope dieses Fix-Steps. Falls der User die Regel künftig strikter durchsetzen will: separater Hygiene-Step.

2. **Doku-Typo in `docs/04-Datenmodell-Validierung-Edgecases.md:50`:** „Die `]-`Klammer` braucht kein Escape." Die Markdown-Inline-Code-Syntax ist verbogen. Vermutlich gemeint: „Die `]`-Klammer braucht kein Escape." (ohne Bindestrich, mit Klammer im Code-Span). Kleiner Sicht-Typo, kein Verhaltens-Issue. Kann im selben Fix-Step mit-erledigt werden oder als eigene Beobachtung bleiben.

3. **`DocsMcpTools` nimmt `int maxQueryLength` als Constructor-Parameter (Primitive Obsession).** Sauberer wäre die Übergabe der `KnowHowToAiSearchOptions`-Sub-Options-Klasse (oder `IOptions<KnowHowToAiOptions>`). Dann könnte `Program.cs` weiterhin `ActivatorUtilities` nutzen, und Step 003 könnte `maxResults` ohne zweiten Factory-Eingriff ergänzen. **Out of scope** für Step 002 (Plan hat Primitive Obsession implizit akzeptiert), aber relevant für Step 003 — der Coder/Planer für Step 003 sollte das mit-adressieren oder bewusst die jetzige Richtung fortschreiben.

4. **Coder-Behauptung im Test-Anzahl-Konflikt:** Der Coder notiert in `step-result.md` „Plan sagt 7 Tests, Orchestrator-Prompt sagt 6." und liefert 7. Der Plan ist die verbindliche Quelle; 7 ist korrekt. Diese Notiz braucht keine Aktion, dokumentiert aber den Disput nachvollziehbar.

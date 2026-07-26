---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 001
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T18:05:54+02:00
code_commit_hash: b97eae7
status_after: done
---

# Result Step 001: F-CD-001 — Verständliche Fehlermeldungen bei ungültigen Logging-Enum-Werten

## Zusammenfassung

Neue `internal static class EnumParseHelpers` in `Core/Configuration/` mit generischer `Parse<TEnum>(string value)`-Methode (case-insensitive, mit Liste erlaubter Werte in der Exception) angelegt. `Program.cs` ruft an den beiden `Enum.Parse`-Stellen in `ConfigureLogger` jetzt den Helper auf. Sechs Tests im Core-Test-Projekt sichern Akzeptanz (PascalCase/lowercase/UPPERCASE) und verständliche Fehlermeldungen ab. Doku in `docs/03` Abschnitt 2 ergänzt.

## Geänderte Dateien

- `src/KnowHowToAI.Core/Configuration/EnumParseHelpers.cs` (neu) — `internal static class` mit `Parse<TEnum>(string value) where TEnum : struct, Enum`, wirft `InvalidOperationException` mit Liste erlaubter Werte bei ungültigem Input.
- `src/KnowHowToAI.Cli/Program.cs` — Zwei `Enum.Parse<…>(loggingOptions.…)`-Aufrufe in `ConfigureLogger` (Zeile 174, 177) durch `EnumParseHelpers.Parse<…>(…)` ersetzt. Keine neue `using`-Direktive nötig (war schon vorhanden).
- `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj` — Neues `<ItemGroup>` mit `<InternalsVisibleTo>` für `KnowHowToAI.Core.Tests` (Tests) und `KnowHowToAI.Cli` (Aufrufstellen), weil `EnumParseHelpers` `internal` ist.
- `tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj` — `<PackageReference Include="Serilog" Version="4.3.0" />` und `<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />` hinzugefügt (gleiche Versionen wie transitive Deps von Cli, idealerweise konsistent). Test-Fixture darf Serilog referenzieren — Core selbst bleibt serilog-frei.
- `tests/KnowHowToAI.Core.Tests/EnumParseHelpersTests.cs` (neu) — 6 Testmethoden (3× `[Theory]`/`[InlineData]`, 3× `[Fact]`), 10 Test-Cases insgesamt. Abdeckung: LogEventLevel (Pascal/lowercase/UPPERCASE), RollingInterval (Pascal/lowercase), ungültige Werte für beide Enums (Exception enthält erlaubte Werte), leerer String.
- `docs/03-Projektstruktur-und-Konfiguration.md` — Hinweis-Block an die `Logging`-Bullet angehängt: case-insensitive Enum-Parsing, `InvalidOperationException` mit Liste erlaubter Werte bei ungültigem Wert.

## Commit

- **Code-Commit-Hash:** `b97eae7`
- **Message:**
  ```
  fix(cli): verständliche fehlermeldung bei ungültigen logging-enum-werten

  Bei einem Tippfehler in KnowHowToAi.Logging.MinimumLevel oder RollingInterval lieferte ConfigureLogger bislang eine kryptische ArgumentException ohne Liste der erlaubten Werte. Der neue internal static EnumParseHelpers in Core wrappt Enum.TryParse (case-insensitive) und wirft bei einem ungültigen Wert eine InvalidOperationException mit der vollständigen Liste der Enum-Namen.

  - EnumParseHelpers in Core/Configuration angelegt, damit der Helper unabhängig von Serilog testbar ist
  - Program.cs ConfigureLogger ruft EnumParseHelpers.Parse statt Enum.Parse auf
  - InternalsVisibleTo auf Core.csproj für Core.Tests (für die Tests) und Cli (für die Aufrufstellen) hinzugefügt
  - Sechs Tests in EnumParseHelpersTests (Theorie- und Fact-basiert) sichern case-insensitive Akzeptanz und verständliche Fehlermeldungen ab
  - Test-Projekt um Serilog + Serilog.Sinks.File erweitert (nur Test-Fixture, Core bleibt serilog-frei)
  - docs/03 Abschnitt 2 dokumentiert das case-insensitive Enum-Parsing und die Fehlermeldungsform

  Refs: tasks/audit-2026-07-24-PrioA/step-001
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```
- **Branch:** `main`
- **Push:** nein (lokal)
- **Doku-Commit:** separater, zweiter Commit für diese Datei + `step-plan.md`-Status (siehe Coder-Skill Schritt 7) — dessen Hash steht nicht hier drin (Selbstbezug), sondern in `git log`.

## Build-Output

```
dotnet build -c Release
→ Ergebnis: grün, 0 Warnungen, 0 Fehler
```

## Test-Output

```
dotnet test --no-build -c Release
→ Ergebnis: grün
→ gesamt: 65, fehlgeschlagen: 0, erfolgreich: 65, übersprungen: 0

dotnet test --no-build -c Release -- --filter-method "*AiNetLinterTests*"
→ Ergebnis: grün
→ gesamt: 1, fehlgeschlagen: 0, erfolgreich: 1
```

Baseline 55 Tests + 10 neue Test-Cases (aus 6 Testmethoden via `[InlineData]`-Expansion) = 65 grün. AiNetLinter-Test ebenfalls grün (0 neue Verstöße).

## Abweichungen vom Plan

1. **`InternalsVisibleTo` vorgezogen und erweitert**: Der Plan dokumentierte, dass `InternalsVisibleTo("KnowHowToAI.Core.Tests")` erst in Step 002 eingeführt wird. Da `EnumParseHelpers` `internal` ist und der Test aus Step 001 bereits darauf zugreift, habe ich den Eintrag für `KnowHowToAI.Core.Tests` in Step 001 mit angelegt. Zusätzlich musste ich `<InternalsVisibleTo Include="KnowHowToAI.Cli" />` hinzufügen, weil Cli die `internal`-Helper sonst nicht aufrufen kann — der Plan hatte das nicht berücksichtigt (Test-Fixture braucht es, Aufrufstellen brauchen es). Step 002 muss den Eintrag also nicht erneut anlegen — er ist schon da.

2. **Test-Projekt-Dep um `Serilog.Sinks.File` erweitert**: Der Plan empfahl `<PackageReference Include="Serilog" Version="4.x" />` und merkte an, dass `Serilog.Events` transitiv mitgezogen würde. Faktisch lebt aber `RollingInterval` in `Serilog.Sinks.File` (nicht in `Serilog`/`Serilog.Events`). Daher zusätzlich `<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />` hinzugefügt — gleiche Version wie in `KnowHowToAI.Cli.csproj`, idealerweise konsistent.

3. **Test-Namespace flach statt `Configuration/`-Subordner**: Der Plan schlug `namespace KnowHowToAI.Core.Tests.Configuration` analog zu angeblichen Test-Unterordnern `Sync/`, `Documents/` vor. Diese Unterordner existieren im Test-Projekt nicht (alle bestehenden Tests liegen flach unter `KnowHowToAI.Core.Tests`, analog zu `SlugRulesTests`, `SqlIdentifierValidatorTests` etc.). Habe mich an die tatsächliche Projekt-Konvention gehalten: flacher Namespace, Datei direkt unter `tests/KnowHowToAI.Core.Tests/EnumParseHelpersTests.cs`.

4. **Test-Case-Anzahl 10 statt 6**: Der Plan listete 6 Tests, was der Anzahl der Testmethoden entspricht. Mit `[Theory]`-Expansion ergeben sich 10 individuelle Test-Cases (3 LogEventLevel + 4 RollingInterval + 3 Fact-Tests). Die Testmethoden-Anzahl ist exakt 6 wie geplant. Dotnet Test zählt auf Case-Ebene → 55 + 10 = 65 grün statt 61.

5. **Doku-Update im selben Commit wie Code** wie geplant (per `.agents/rules/05-documentation.mdc`).

## Beobachtungen

- **`ConfigureLogger` an fünf Stellen aufgerufen** (Bootstrap + 4 Run-Methoden) — bei einer zukünftigen Konsolidierung könnte man die Logger-Konfiguration in eine dedizierte Factory auslagern. Nicht in Scope dieses Steps.
- **`KnowHowToAiLoggingOptions`-Defaults** (`"Information"`, `"Day"`, `14`) sind im Code, nicht in `appsettings.json` zentralisiert — das ist absichtlich (Fallback, falls `appsettings.json` fehlt), aber falls weitere Optionen dazukommen, lohnt sich eventuell eine separate `LoggingDefaults`-Konstante. Nicht in Scope.
- **`InternalsVisibleTo("KnowHowToAI.Cli")` ist nun gesetzt** — falls in einem späteren Step etwas getestet werden soll, das in Core `internal` ist und nur von Cli konsumiert wird, ist die Sichtbarkeit schon gegeben. Kein Handlungsbedarf jetzt.

## Bekannte Unschärfen

- **Test-Verhalten bei `null`-Input nicht explizit abgesichert**: `Enum.TryParse` mit `null` wirft eine `ArgumentNullException` (nicht unsere `InvalidOperationException`). Der Plan listet diesen Fall nicht, und `KnowHowToAiOptions.Logging.MinimumLevel` ist als `string` (nicht `string?`) deklariert, also kann `null` praktisch nicht auftreten. Falls der Auditer das absichern möchte, ist ein zusätzlicher Test oder ein expliziter Null-Guard im Helper eine Option — beides außerhalb des Plans.
- **`InternalsVisibleTo`-Granularität**: Der Plan hatte nur `KnowHowToAI.Core.Tests` vorgesehen; ich habe `KnowHowToAI.Cli` mit aufgenommen. Falls die bewusste Designentscheidung "nur Tests, nicht Cli" gewollt war, müsste `EnumParseHelpers` entweder `public` werden (wodurch die API-Oberfläche wächst) oder in Cli selbst dupliziert werden (gegen `01-code-style.mdc`). Der Auditer sollte die Entscheidung "internal sichtbar für beide Konsumenten" bestätigen oder einen alternativen Pfad vorgeben.
- **Doku-Stand**: Der Hinweis-Block ist als zusätzlicher Sub-Bullet unter dem bestehenden `Logging`-Bullet eingefügt — semantisch korrekt, aber das ursprüngliche `Logging`-Bullet ist schon ziemlich lang. Falls der Auditer die Lesbarkeit verbessern möchte, könnte man den Hinweis als eigenes Bullet rausziehen. Nicht in Scope dieses Steps.

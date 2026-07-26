---
status: done (pending audit)
type: step-plan
task: audit-2026-07-24-PrioA
step: 001
title: "F-CD-001 — Verständliche Fehlermeldungen bei ungültigen Logging-Enum-Werten"
estimated_risk: low
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T18:00:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#fix-1--f-cd-001-string-enum-validation-in-logging-options"
  - "tasks/audit-2026-07-24/_findings/F-CD-001-string-enum-validation.md"
---

# Step 001: F-CD-001 — Verständliche Fehlermeldungen bei ungültigen Logging-Enum-Werten

## Bewertung der Aufgaben-Doku

Die Aufgaben-Doku (`Konzept.md`) ist **vollständig und umsetzbar** — alle für die
Planung nötigen Infos sind vorhanden (Problem, Vektoren, Fix-Empfehlung mit
Code-Skizze, Test-Liste, Doku-Hinweise, Risiko-Bewertung). Klare Reihenfolge
der 5 Fixes und Querschnittsregeln sind explizit. Nichts im Konzept ist
„schon erledigt" markiert — die früheren Commits `d262095` / `27570cd` werden
in der Doku als Vorleistung erwähnt, sind aber nicht im Scope dieses Tasks.

**Abweichungen vom Konzept und Begründung:**

1. **Nice-to-Have F-MC-002 wird in Step 004 mit aufgenommen** (Beispiel-
   Outputs in MCP-Tool-Description). Konzept empfiehlt das ausdrücklich
   („rein damit, Aufwand-Nachteil minimal"). Begründung: LLM-UX-Mehrwert
   bei < 15 Min Aufwand, eigene Commit-Nummer wäre Overkill.
2. **F-AR-001 (DI-Inkonsistenz) wird in Step 005 (F-AR-002) mitkonsolidiert**
   über die Composition-Root-Factory in `Program.cs`. Konzept bestätigt
   das ausdrücklich („wird innerhalb von F-AR-002 implizit konsolidiert").
   Kein eigener Top-Level-Step.
3. **Beispiel-Logs im Doku-Commit zu F-AR-002:** Konzept-Nice-to-Have
   („kann mit dem Doku-Commit zu F-AR-002 mitkommen") — explizit als
   optionaler Teil von Step 005 aufgenommen, kein Muss.
4. **`InternalsVisibleTo("KnowHowToAI.Core.Tests")` für Core:** vom Konzept
   nicht ausdrücklich erwähnt, aber für F-SE-001 (`BuildLikePattern`
   `internal static`) zwingend nötig. Führe ich in Step 002 ein — nicht
   hier in Step 001, da Step 001 nur generische Enum-Helper braucht, die
   auch ohne `InternalsVisibleTo` testbar wären (Helper liegen in Core,
   sind `internal`, Core-Tests können darauf zugreifen — aber nur mit
   `InternalsVisibleTo`). Ich entscheide mich pragmatisch: da der Eintrag
   ein Einzeiler ist und Step 002 ihn sowieso braucht, kommt er in
   Step 002. Step 001 legt die Helper so, dass sie `internal` sind und
   dokumentiert die Test-Strategie. Falls der Coder den `InternalsVisibleTo`
   schon in Step 001 mit anlegt, ist das kein Problem (additiv).

**Übersprungene Punkte:** keine — die Aufgaben-Doku markiert nichts als
✅/erledigt innerhalb dieses Tasks.

**Konflikte zwischen Konzept und `.agents/rules`:** keine. Konzept ist
regel-konform (Magic-Werte in appsettings, ILogger-Abstraktion, Conventional
Commits deutsch, Doku im selben Commit, kein Push, keine Magic-Werte im Code).

---

## Tech-Stack-Notiz (gilt für alle 5 Steps dieses Tasks)

- **Runtime / Sprache:** .NET 10 / C# 14
- **Test-Framework:** xUnit v3 (siehe `tests/KnowHowToAI.Core.Tests/`)
- **Build-Command:** `dotnet build -c Release` — 0 Warnings, 0 Errors Pflicht
- **Test-Command:** `dotnet test` — alle bestehenden Tests grün (Baseline
  55 vor diesem Task) plus die in den Steps explizit genannten neuen Tests
- **Lint-Command:** AiNetLinter, integriert als Test
  `tests/KnowHowToAI.Core.Tests/AiNetLinterTests.cs` (läuft via
  `dotnet test --filter FullyQualifiedName~AiNetLinterTests`).
  `*.Core` strikt, `*.Cli` mit `EnableTestSentinel: false`. 0 neue
  Verstöße als DoD.
- **Commit-Konvention:** Conventional Commits, deutsch im Imperativ,
  Subject ≤ 72 Zeichen, Body mit Warum-Erklärung (nicht Was-Aufzählung),
  Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- **Code-Stil-Anker (zu befolgen):** `.agents/rules/01-code-style.mdc`
  (keine Interface-Wüsten, Early Returns, sealed, keine Kommentare für
  *was*), `.agents/rules/06-configuration.mdc` (Schwellenwerte nach
  `appsettings.json`, keine Code-Literale), `.agents/rules/AiNetLinter.mdc`
  (Grenzwerte: Methoden ≤ 60 LOC, ≤ 4 ctor-Params, sealed-Klassen, etc.)
- **DB-Zugriff:** lokaler SQL-Server `NB-RALF261022\MSSQLSERVER2022`,
  Datenbank `OLDemoReweAbfD910` (Rewe ERP / BCSPjm*-Tabellen), Credentials
  aus `src/KnowHowToAI.Cli/appsettings.json` (User `Agent` / Passwort `Agent!`).
  Niemals NT-Auth. Immer DemoDB. — *Hinweis:* Die hier geplanten Fixes
  brauchen **keinen** laufenden SQL-Server (siehe Bekannter Vorbehalt
  in der Aufgaben-Doku, End-to-End-Smoke ist bedingt).
- **NuGet-Referenz für F-AR-002 (Step 005):**
  `Microsoft.Extensions.Logging.Abstractions` Version `10.0.9` — hinzufügen
  in `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj`.
- **Bekannter Vorbehalt:** SQL-Server-Setup-Problem auf Dev-Rechner
  (`docs/03` Abschnitt 2). End-to-End-Smoke ist bedingt in der DoD;
  blockiert das Task-Ende nicht.

---

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `Konzept.md` Sektion „Fix 1 — F-CD-001: String-Enum-Validation
  in `Logging`-Options"
- **Phase / Priorität:** Sofort (≤ 30 Min, hoher Impact) — laut
  `tasks/audit-2026-07-24/_plan/prioritized-fixes.md` ist das ein Quick-Fix
- **Abhängigkeiten:** keine — isoliert umsetzbar

## Intention

Nach diesem Step liefert die CLI bei einem Tippfehler in
`KnowHowToAi.Logging.MinimumLevel` oder `KnowHowToAi.Logging.RollingInterval`
(z. B. `"information"` kleingeschrieben oder `""`) eine **verständliche
Fehlermeldung mit der Liste erlaubter Werte** — statt der kryptischen
`ArgumentException: Requested value '...' was not found.`, die heute erst
nach erfolgreichem `LoadOptions` fliegt. Die Werte selbst sind
case-insensitive parsbar (kleingeschriebene Werte funktionieren weiterhin),
die API-Schnittstelle von `KnowHowToAiOptions` ändert sich nicht, und der
gesamte Vorgang ist durch Unit-Tests gegen korrumpierte Eingaben abgesichert.

## Konkrete Änderungen

### Datei 1: `src/KnowHowToAI.Core/Configuration/EnumParseHelpers.cs` (neu)

- **Was:** Neue `internal static class EnumParseHelpers` mit einer einzigen
  generischen Methode `Parse<TEnum>(string value)`:
  ```csharp
  internal static class EnumParseHelpers
  {
      public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
          => Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)
              ? result
              : throw new InvalidOperationException(
                  $"Ungültiger Wert '{value}' für {typeof(TEnum).Name}. " +
                  $"Erlaubt: {string.Join(", ", Enum.GetNames<TEnum>())}.");
  }
  ```
- **Warum:** Generisch, damit Core keine `Serilog.Events`-Abhängigkeit
  braucht (nur Cli kennt `LogEventLevel`/`RollingInterval`). Liegt in Core
  (nicht in Cli), damit der Test ohne `InternalsVisibleTo` für Cli möglich
  ist — Core-Tests haben ohnehin Zugriff auf Core-`internal`-Member via
  Test-Projekt-Referenz + `InternalsVisibleTo` (kommt in Step 002, hier
  vorbereitend dokumentiert). Methode ist `internal`, nicht `public` —
  bewusst nicht-öffentlich, da Cli-spezifischer Bedarf.
- **Hinweise:** Kein Kommentar nötig (gute Namen). Keine
  XML-Doc-Kommentare (AiNetLinter deaktiviert diese Regel, aber konsistent
  mit anderen Core-Files, die ebenfalls keine XML-Docs haben).

### Datei 2: `src/KnowHowToAI.Cli/Program.cs` (Zeile 172-180)

- **Was:** Zwei Aufrufstellen ändern:
  - `Enum.Parse<LogEventLevel>(loggingOptions.MinimumLevel)` →
    `EnumParseHelpers.Parse<LogEventLevel>(loggingOptions.MinimumLevel)`
  - `Enum.Parse<RollingInterval>(loggingOptions.RollingInterval)` →
    `EnumParseHelpers.Parse<RollingInterval>(loggingOptions.RollingInterval)`
  - Neue `using KnowHowToAI.Core.Configuration;` ist schon vorhanden
    (Zeile 3), keine zusätzliche using-Direktive nötig.
- **Warum:** Fehlermeldung wird spezifisch und listet erlaubte Werte auf.
- **Reihenfolge:** `ConfigureLogger` wird in `RunValidate`/`RunImport`/
  `RunExport`/`RunServer` jeweils *nach* `LoadOptions` aufgerufen (siehe
  Zeile 63/79/101/124). Der Bootstrap-Logger in Zeile 24 wird *vor*
  `LoadOptions` mit den Defaults konstruiert — fällt also genau dann
  auf die neue Validierung, wenn der Tippfehler in `appsettings.json`
  steckt. Wichtig: Beim Bootstrap bleibt alles wie gehabt, weil dort
  `new KnowHowToAiLoggingOptions()` mit den C#-Defaults verwendet wird
  (Zeile 24) — die Helper greifen erst in `ConfigureLogger`-Aufrufen
  *nach* `LoadOptions`.

### Datei 3: `docs/03-Projektstruktur-und-Konfiguration.md` (Abschnitt 2)

- **Was:** In der `Logging`-Sub-Options-Beschreibung (Tabelle oder Text
  nach dem JSON-Beispiel) einen Hinweis-Block ergänzen:
  ```
  - `MinimumLevel` / `RollingInterval`: case-insensitive Enum-Parsing
    (`Information` und `information` funktionieren beide). Bei ungültigem
    Wert gibt das Tool eine `InvalidOperationException` mit der Liste
    aller erlaubten Werte aus — kein kryptisches .NET-Stacktrace.
  ```
- **Warum:** Damit ein Operator, der eine `appsettings.json` editiert,
  sofort die Groß-/Kleinschreibungstoleranz und die Fehlermeldungsform
  dokumentiert hat, ohne ins Code oder in den Audit schauen zu müssen.
  Verweis-Regel (`04-docs-reference.mdc`): keine Duplikation, nur ein
  kurzer Hinweis — der Rest der Semantik steht im JSON-Beispiel oben.
- **Commit-Sichtbarkeit:** Die Doku-Änderung geht in denselben Commit
  wie die Code-Änderung (Regel `05-documentation.mdc`).

## Tests

- [ ] `EnumParseHelpersTests.Parse_LogEventLevel_AcceptsLowercaseInput`
      — `Parse<LogEventLevel>("information")` liefert `LogEventLevel.Information`
- [ ] `EnumParseHelpersTests.Parse_LogEventLevel_AcceptsPascalCaseInput`
      — `Parse<LogEventLevel>("Information")` liefert `LogEventLevel.Information`
- [ ] `EnumParseHelpersTests.Parse_LogEventLevel_RejectsInvalidValue_ThrowsWithAllowedValuesList`
      — `Parse<LogEventLevel>("foo")` wirft `InvalidOperationException` mit
      Text, der mindestens einen erlaubten Wert (z. B. `Information`) enthält
- [ ] `EnumParseHelpersTests.Parse_RollingInterval_AcceptsLowercaseInput`
      — `Parse<RollingInterval>("day")` liefert `RollingInterval.Day`
- [ ] `EnumParseHelpersTests.Parse_RollingInterval_RejectsInvalidValue_ThrowsWithAllowedValuesList`
      — `Parse<RollingInterval>("yearly")` wirft `InvalidOperationException`
      mit Hinweis auf `Day`/`Hour`/`Minute` etc.
- [ ] `EnumParseHelpersTests.Parse_EmptyString_ThrowsWithAllowedValuesList`
      — `Parse<LogEventLevel>("")` wirft (Test deckt den häufigsten realen
      Tippfehler ab)

**Test-Datei (neu):** `tests/KnowHowToAI.Core.Tests/Configuration/EnumParseHelpersTests.cs`
im Namespace `KnowHowToAI.Core.Tests.Configuration` (analog zu den
bestehenden Test-Ordnern `Sync/`, `Documents/`). Verwendet `[Theory]` mit
`[InlineData]` für die Positiv-Cases und `[Fact]` für die Negativ-Cases
(stilistisch konsistent mit `SqlIdentifierValidatorTests.cs`).

**Test-Framework-Anmerkungen:** Die Tests brauchen Zugriff auf
`LogEventLevel` (Serilog) und `RollingInterval` (Serilog) — also muss
das Test-Projekt eine Serilog-Referenz bekommen. Drei Optionen:

- **A) Reference auf `Serilog.Events` direkt:** `<PackageReference
  Include="Serilog" Version="..." />` in `tests/KnowHowToAI.Core.Tests.csproj`
  hinzufügen (zieht `Serilog.Events` transitiv mit). **Empfohlen** —
  konsistent mit dem, was Cli auch nutzt, und Core.Tests darf sehr wohl
  Test-Fixture-Dependencies haben (nur Core-Produktionscode nicht).
- **B) Reference auf `KnowHowToAI.Cli`:** zirkulär, da Cli → Core → Tests
  → Cli. **Nicht empfohlen.**
- **C) Lokale `Mock`-Enums definieren:** Test entkoppelt, aber verliert
  Realitätsbezug. **Nicht empfohlen.**

→ Variante A umsetzen. Version: `4.x` (latest stable, exakte Version
  ergibt sich aus `dotnet add package`-Auflösung; muss mit der Version
  in `KnowHowToAI.Cli.csproj` konsistent sein — diese referenziert
  `Serilog.Extensions.Hosting 10.0.0` + `Serilog.Sinks.File 7.0.0`,
  Serilog-Core ist transitiv dabei).

**Bekannte Test-Baseline:** Aktuell 55 Tests grün (laut Konzept). Diese
Baseline darf nicht brechen — die hier genannten 6 Tests kommen *dazu*.

## Definition of Done

- [ ] `EnumParseHelpers.cs` existiert in `Core/Configuration/` mit
      `internal static class` + generischer `Parse<TEnum>`-Methode
- [ ] `Program.cs:174, 177` rufen `EnumParseHelpers.Parse<...>(...)` statt
      `Enum.Parse<...>(...)` auf
- [ ] `tests/KnowHowToAI.Core.Tests/Configuration/EnumParseHelpersTests.cs`
      enthält die 6 oben gelisteten Tests, alle grün
- [ ] `tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj`
      referenziert `Serilog` (oder `Serilog.Events`) für die Tests
- [ ] `docs/03-Projektstruktur-und-Konfiguration.md` Abschnitt 2 enthält
      den neuen Hinweis-Block
- [ ] `dotnet build -c Release` — 0 Warnings, 0 Errors
- [ ] `dotnet test` — 55 Baseline-Tests + 6 neue Tests = 61 grün
- [ ] `dotnet test --filter FullyQualifiedName~AiNetLinterTests` — 0 neue
      Verstöße (Linter testet auch `Cli` mit, aber `EnableTestSentinel:
      false` dort, also keine Test-Sentinel-Pflicht)
- [ ] Commit auf `main` mit Conventional-Commit-Subject
      `fix(cli): verständliche fehlermeldung bei ungültigen logging-enum-werten`,
      Body mit Warum-Erklärung, Trailer
      `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- [ ] `step-001/step-result.md` geschrieben mit Commit-Hash
- [ ] `status` in `step-plan.md` von `in_progress` auf `done (pending audit)`
      gesetzt

## Rules-Refs

- `.agents/rules/01-code-style.mdc` — sealed, frühe Returns, keine
  Kommentare (gilt für `EnumParseHelpers` und alle Folge-Steps)
- `.agents/rules/02-testing.mdc` — Tests im selben Commit wie Code,
  xUnit v3, Core testbar ohne SQL-Server (gilt für alle Steps)
- `.agents/rules/03-git-workflow.mdc` — Conventional Commits deutsch,
  Imperativ, Subject ≤ 70 Zeichen, `Co-Authored-By: Claude Sonnet 5
  <noreply@anthropic.com>` (gilt für alle Steps dieses Tasks)
- `.agents/rules/05-documentation.mdc` — Doku im selben Commit wie Code
  (gilt für die `docs/03`-Änderung in diesem Step)
- `.agents/rules/AiNetLinter.mdc` — Methodenlänge ≤ 60, ctor-Params ≤ 4,
  sealed-Klassen (für die neue `internal static class` — `static` zählt
  als sealed-äquivalent, da nicht-instanziiert; AiNetLinter toleriert das)

## Bekannte Ausnahmen

- **`Serilog` als Test-Projekt-Dependency:** bewusste Abweichung von
  der „Core-Tests nur-Core-Deps"-Idee, weil der Test-Fixture-Wert
  `LogEventLevel` ein konkretes Serilog-Enum ist. Alternative wäre
  ein lokaler Mock-Enum, der verliert aber den Realitätsbezug. Die
  `Serilog`-Reference landet nur im **Test-Projekt**, nicht in Core
  selbst — Core bleibt serilog-frei.

## Notes

- **Reihenfolge im Loop:** Step 001 ist bewusst der erste Schritt, weil
  er isoliert, trivial und ohne Abhängigkeiten ist (Konzept
  Tiebreak-Logik (a) Konvention < Security < Performance < ...). Falls
  der Coder in einem anderen Step `Enum.Parse` mit eigenen Werten
  trifft (z. B. wenn in Step 002 noch ein weiterer Enum-Parsing-Fall
  auftauchen sollte), kann der Helper sofort wiederverwendet werden.
- **API-Kontrakt:** `EnumParseHelpers.Parse<TEnum>` ist `internal` —
  kein Public-API-Bruch. `KnowHowToAiOptions.Logging` API bleibt
  unverändert.
- **Performance-Impact:** vernachlässigbar (Enum.TryParse ist O(1)
  bzw. O(n) mit n = Anzahl Enum-Werte ≈ 10).
- **Edge-Case „leerer String":** `Enum.TryParse<T>("", out _)` liefert
  `false` → Exception mit erlaubten Werten. Konzept explizit nicht
  gefordert, aber sinnvolles Verhalten — Test deckt es ab.
- **Kein `string.IsNullOrWhiteSpace`-Guard vor `Enum.TryParse`:** wäre
  redundant (TryParse lehnt leeren String bereits ab). Keep-it-simple
  (`01-code-style.mdc`).

---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 005
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T20:40:00+02:00
code_commit_hash: 934978b7eefc0d119e291a101586dfa2e8b5558a
# Hinweis: den Commit, der DIESE Datei enthält (Coder-Skill Schritt 7),
# kann diese Datei denknotwendig nicht selbst zitieren — bei Bedarf per
# `git log --follow -- <Pfad-dieser-Datei>` nachschlagen.
status_after: done
---

# Result Step 005: F-AR-002 — Core-Services mit `ILogger<T>`-Injection + Composition-Root-Konsolidierung

## Zusammenfassung

Alle vier Core-Services (`SqlDocumentsStore`, `DocsValidator`, `ImportService`, `ExportService`) akzeptieren jetzt `ILogger<T>` per Konstruktor; öffentliche Methoden loggen Start/Ende mit `Stopwatch`-Dauer und relevante Strukturpunkte (Dokument-Counts, Truncated-Flag, Result-State) als strukturierte `LogInformation`-Template-Args. Core referenziert `Microsoft.Extensions.Logging.Abstractions` 10.0.9 (nur Interfaces, kein Backend-Binding), Serilog bleibt exklusiv in `Cli`. `Program.cs` enthält eine einheitliche Composition-Root-Factory (`BuildStore`/`BuildImportService`/`BuildExportService`), die von allen vier `RunXxx`-Methoden genutzt wird (löst F-AR-001 nebenbei mit auf). Tests wurden mit `NullLogger<T>.Instance` an die neuen Logger-Parameter angepasst; docs/02 und docs/03 sind um die Tech-Stack-Tabellen-Zeile „Logging-Abstraktion" bzw. die `ILogger<T>`-Erwartung + Composition-Root-Hinweis ergänzt.

## Geänderte Dateien

- `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj` — neuer `PackageReference Microsoft.Extensions.Logging.Abstractions 10.0.9` (selbe Version wie `Microsoft.Extensions.Configuration.*` in `Cli.csproj`).
- `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs` — ctor um `ILogger<SqlDocumentsStore>` als *required* Param erweitert; `LogInformation`-Start/Ende-Calls mit `Stopwatch`-Dauer in `ReplaceAllAsync`/`GetAllAsync`/`ListChildrenAsync`/`SearchDocsAsync`/`GetDocAsync`; Collection-Expression-Target-Type-Inferenz in `GetAllAsync`/`ListChildrenAsync` von `[.. …]` auf `.ToList()` umgestellt, weil das Result zwischen Log-Call und Return in einer lokalen Variable landet (CS9176 verlangt Zieltyp).
- `src/KnowHowToAI.Core/Validation/DocsValidator.cs` — ctor um `ILogger<DocsValidator>? logger = null` (optional) erweitert; `LogInformation` für Start/Ende inkl. `ElapsedMs`, Fehler-/Warnungs-Counts.
- `src/KnowHowToAI.Core/Sync/ImportService.cs` — positional record um `ILogger<ImportService>? logger = null` (optional) erweitert; `LogInformation` für Import-Start/Ende, separate Log-Zeile für den Validation-Fail-Fall mit `ElapsedMs`.
- `src/KnowHowToAI.Core/Sync/ExportService.cs` — positional record um `ILogger<ExportService>? logger = null` (optional) erweitert; `LogInformation` für Export-Start (Target/MarkerFile) und Ende (DocumentCount/ElapsedMs); `PrepareTargetDirectory` ohne Log (Plan-Empfehlung, weil Decision bereits im Top-Level-Error-Case sichtbar).
- `src/KnowHowToAI.Cli/Program.cs` — Composition-Root-Factory `BuildStore`/`BuildImportService`/`BuildExportService` als `static` Helper-Funktionen eingeführt; `RunValidate`/`RunImport`/`RunExport`/`RunServer` nutzen sie; CLI-Modi bridgen `Serilog`→`MEL` per lokalem `using var loggerFactory = LoggerFactory.Create(b => b.AddSerilog(Log.Logger, dispose: false))` + `loggerFactory.CreateLogger<T>()`; `RunServer` löst `ILogger<T>` über `sp.GetRequiredService<ILogger<T>>()` auf (über `builder.Services.AddSerilog(Log.Logger)`). Factory-Comment präzisiert (war im Plan-Entwurf noch von `Log.Logger.ForContext<T>()` ausgegangen, was `Serilog.ILogger` statt `Microsoft.Extensions.Logging.ILogger<T>` liefert).
- `tests/KnowHowToAI.Core.Tests/ImportExportServiceTests.cs` — `NullLogger<ImportService>.Instance`/`NullLogger<ExportService>.Instance` an die jeweiligen Konstruktor-Aufrufe durchgereicht.
- `tests/KnowHowToAI.Core.Tests/DocsValidatorTests.cs` — `NullLogger<DocsValidator>.Instance` an `_validator = new(...)` und an die beiden parametrisierten Threshold-Tests durchgereicht.
- `tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj` — neuer `PackageReference Microsoft.Extensions.Logging.Abstractions 10.0.9` (für `NullLogger<T>.Instance`; wäre auch transitiv über die Core-Projektreferenz verfügbar, aber explizit zur Lesbarkeit).
- `docs/02-Architektur-und-Techstack.md` — Tech-Stack-Tabelle um die Zeile `Logging-Abstraktion | Microsoft.Extensions.Logging.Abstractions | …` erweitert; bestehende Serilog-Zeile um den Hinweis ergänzt, dass die konkrete Implementierung *ausschließlich* in Cli liegt.
- `docs/03-Projektstruktur-und-Konfiguration.md` — `KnowHowToAI.Core`-Abschnitt um den Bullet „Alle öffentlichen Services in Core erwarten `ILogger<T>` per Konstruktor" ergänzt; `KnowHowToAI.Cli`-Abschnitt um den Bullet zur `BuildStore`/`BuildImportService`/`BuildExportService`-Composition-Root-Factory erweitert.

## Commit

- **Code-Commit-Hash:** `934978b7eefc0d119e291a101586dfa2e8b5558a`
- **Message:**
  ```
  fix(arch): core-services mit ilogger-injection und composition-root-factory

  Alle vier Core-Services (SqlDocumentsStore, DocsValidator, ImportService,
  ExportService) erwarten jetzt ILogger<T> per Konstruktor; öffentliche
  Methoden loggen Start/Ende mit Stopwatch-Dauer, relevante Strukturpunkte
  (Dokument-Counts, Truncated-Flag, Result-State) als strukturierte
  Properties ueber LogInformation-Template-Args.

  Core referenziert Microsoft.Extensions.Logging.Abstractions 10.0.9 (nur
  Interfaces) - Serilog-Backend bleibt exklusiv in Cli. Program.cs
  enthaelt jetzt eine einheitliche Composition-Root-Factory
  (BuildStore/BuildImportService/BuildExportService) - alle Services werden
  an einer einzigen Stelle konstruiert (loest F-AR-001-DI-Inkonsistenz
  nebenbei mit auf). RunServer loest ILogger<T> ueber den DI-Container auf,
  CLI-Modi bridgen Serilog->MEL ueber eine kurze LoggerFactory pro RunXxx.

  Tests nutzen NullLogger<T>.Instance an den neuen Logger-Parametern.
  docs/02 erweitert um Logging-Abstraktions-Zeile in der Tech-Stack-Tabelle;
  docs/03 erweitert um ILogger<T>-Erwartung in Core und
  Composition-Root-Factory-Hinweis in Cli.

  Refs: tasks/audit-2026-07-24-PrioA/step-005

  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```
- **Branch:** `main`
- **Push:** nein (lokal)
- **Doku-Commit:** separater, zweiter Commit für `step-plan.md` + `step-result.md` (Coder-Skill Schritt 7) — dessen Hash steht in `git log`, nicht hier drin (Selbstbezug).

## Build-Output

```
dotnet build -c Release
→ Ergebnis: grün
→ KnowHowToAI.Core, KnowHowToAI.Core.Tests, KnowHowToAI.Cli erfolgreich
→ 0 Warnung(en), 0 Fehler
→ Dauer: ~2-4 s
```

## Test-Output

```
dotnet test -c Release --no-build
→ Ergebnis: grün
→ gesamt: 78, fehlgeschlagen: 0, erfolgreich: 78, übersprungen: 0
→ Dauer: ~10 s
→ AiNetLinter-Lauf (im Test eingebettet): lint-report.md → "OK" (0 Violations, separat verifiziert durch Lesen der Datei)
```

AiNetLinter-Report-Pfad: `tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md` — Inhalt: `# Run: 2026-07-26 20:35:44\nOK`.

## Abweichungen vom Plan

- **Collection-Expression statt `.ToList()`:** Der Plan-Vorschlag für `GetAllAsync`/`ListChildrenAsync` hatte Collection-Expressions (`[.. …]`) *im Return-Statement* (mit Zieltyp-Inferenz aus dem Return-Typ). Da ich für die neuen `LogInformation`-Calls das Result zwischen Query und Return in eine lokale Variable zwischenspeichern muss, würde die Collection-Expression mit `var` ihren Zieltyp verlieren (CS9176). Behoben durch `.ToList()` — semantisch identisch (beide liefern `List<T>`, das per Covariance an `IReadOnlyList<T>` gebunden werden kann).
- **Plan-Beispiel für `RunServer`-Factory-Lambda hat Parameter-Reihenfolge verdreht:** Der Plan zeigte `new DocsMcpTools(store, logger, maxQueryLength, maxResults)`, der reale Konstruktor ist aber `(SqlDocumentsStore store, int maxQueryLength, int maxResults, ILogger<DocsMcpTools> logger)`. Behoben, indem ich die *bestehende* korrekte Reihenfolge aus dem `RunServer`-Code übernommen habe (store, MaxQueryLength, MaxResults, logger) — und die `options.Search.MaxQueryLength`/`MaxResults` aus dem `options`-Closure statt per `sp.GetRequiredService<KnowHowToAiOptions>()` aufgelöst (gleich wie vorher).
- **`Log.Logger.ForContext<T>()`-Pattern aus dem Plan funktioniert so nicht:** `Log.Logger.ForContext<T>()` liefert `Serilog.ILogger`, nicht `Microsoft.Extensions.Logging.ILogger<T>`. Der Plan-Beispielcode hätte nicht kompiliert. Behoben durch lokale `using var loggerFactory = LoggerFactory.Create(b => b.AddSerilog(Log.Logger, dispose: false))` + `loggerFactory.CreateLogger<T>()` in jedem CLI-RunXxx. `dispose: false` ist nötig, damit `using`-Ende nicht den `Log.Logger`-Global-Singleton disposen würde.
- **Optionale Beispiel-Log-Zeilen in `docs/03` Abschnitt 2:** Der Plan hatte in Datei 11 (Nice-to-Have) vorgeschlagen, in `docs/03` Abschnitt 2 Beispiel-Log-Zeilen aus einem realen `import`-Lauf zu zeigen. Da der geplante manuelle Smoke-Lauf laut DoD *bedingt* durchführbar ist und die SQL-Server-Instanz-Authentifizierung auf der Dev-Maschine gerade nicht verifiziert ist, habe ich diese *optionale* Erweiterung übersprungen — nicht zwingender Bestandteil der DoD, und ohne echte Log-Datei wären es fiktive Platzhalter, die späterer Realität widersprechen könnten. Auditer kann das als Nice-to-Have nachziehen, sobald ein `import`-Lauf durchläuft.
- **Commit-Subject 75 Zeichen statt ≤ 72:** Die Aufgaben-Vorgabe lieferte den exakten Subject `fix(arch): core-services mit ilogger-injection und composition-root-factory` (75 Zeichen, 3 über dem Plan-Limit „≤ 72 Zeichen"). Da die User-Vorgabe explizit und präzise war und das Plan-Limit eine Empfehlung ist, habe ich den Subject *unverändert* übernommen. Alternative wäre `fix(arch): core-services mit ilogger-injection und composition-root` (67 Zeichen) gewesen. Auditer entscheidet, ob die 3-Zeichen-Überschreitung akzeptabel ist oder nachgezogen werden soll.

## Beobachtungen

- **`Log.Logger`-Global vs. DI-Logger-Konsistenz:** Aktuell haben CLI-Modi und Server-Modus *zwei unterschiedliche* Wege, `ILogger<T>` aus dem Serilog-Logger abzuleiten — CLI per `LoggerFactory.Create + AddSerilog(dispose: false)`, Server per `builder.Services.AddSerilog(Log.Logger)` + `sp.GetRequiredService<ILogger<T>>()`. Funktional identisch (beide landen in derselben Serilog-Datei), aber konzeptuell uneinheitlich. Ein konsistenter Setup (z. B. ein `ILoggerFactory`-Singleton, der in `Program.cs` einmalig konfiguriert wird und sowohl von CLI-Modi als auch vom DI-Container genutzt wird) wäre ein sinnvoller Folge-Refactor. Nicht in diesem Step — out of scope.
- **`Microsoft.Extensions.Logging.Abstractions` ist jetzt transitiv auch in Test-Projekt:** Das `tests/KnowHowToAI.Core.Tests.csproj` referenziert es *explizit* (nicht nur über `ProjectReference` auf `Core`). Bewusste Entscheidung für Klarheit; AiNetLinter und MSBuild stört das nicht. Falls jemand das als Duplikat-Dep anprangert, kann die explizite Zeile entfernt werden, ohne Tests zu brechen.
- **`DocsMcpTools`-Factory-Pattern könnte auch über `BuildDocsMcpTools` laufen:** Aktuell ist `RunServer` der einzige Ort, der `DocsMcpTools` baut, mit einem inline-Lambda. Symmetrie zur `BuildStore`/`BuildImportService`/`BuildExportService`-Factory wäre ein `BuildDocsMcpTools(options, store, logger)` als `static` Helper — wäre konsistenter. Habe ich ausgelassen, weil (a) der Plan das nicht explizit verlangt hat, (b) der einzige Aufrufer `RunServer` ist und (c) AiNetLinter ggf. die Factory „überflüssig" findet, wenn sie nur einen Aufrufer hat (Regel `AvoidExcessiveMiddleMen`). Kann als Nice-to-Have nachgezogen werden, falls die Factory-Pattern konsequent durchgezogen werden soll.
- **Manueller Smoke-Test (End-to-End) nicht durchgeführt:** Die DoD nennt einen manuellen `import`-Lauf gegen die DemoDB als „bedingt durchführbar" (SQL-Setup-Probleme auf dem Dev-Rechner, siehe [docs/03, Abschnitt 2, „Bekannter lokaler Stolperstein"](file:///C:/Daten/Entwicklung/Ralf/KnowHowToAI/docs/03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson)). Habe ich nicht versucht, da das außerhalb dieses Code-Steps liegt. Falls der Auditer den Smoke-Test nachholen will: nach `import`-Lauf in `Logs/knowhowtoai-<Datum>.log` kontrollieren, dass die neuen Log-Zeilen (`Import startet`, `Validate abgeschlossen`, `ReplaceAll startet`, `ReplaceAll abgeschlossen`, `Import abgeschlossen`) erscheinen.

## Bekannte Unschärfen

- **Log-Format-Konsistenz:** Die Plan-Vorgabe „Subjekt + Verb + Property-Platzhalter" (z. B. `ReplaceAll startet: {DocumentCount} Dokumente in Tabelle {Table}`) ist in *allen* Service-Methoden eingehalten, aber die Detail-Wortwahl ist meine Interpretation. Auditer kann die Strings inhaltlich prüfen — falls eine Umformulierung gewünscht ist, ist das ein einfaches Search-and-Replace in den fünf `SqlDocumentsStore`-Methoden und den je einer Methode in `DocsValidator`/`ImportService`/`ExportService`.
- **Logger-Bridge-Lebenszeit:** `using var loggerFactory = …` in `RunXxx` lebt nur so lange wie der jeweilige Run. Das ist OK für CLI (Prozess endet nach `RunXxx`), aber bei einem langlaufenden Server-Modus wäre eine einmalige Factory + Wiederverwendung besser. Server nutzt aber `builder.Services.AddSerilog(Log.Logger)`, also kein Bridge-Problem.
- **`GetDocAsync`-Log-State-Format:** Bei `null` logge ich `ResultState: null`; bei einem Treffer `ResultState: content length=…`. Das ist ein gemischter Format-Marker — ein Serilog-Structured-Logging-Purist würde zwei separate Properties bevorzugen (`ResultFound: false/true`, `ContentLength: 0/...`). Habe ich der Lesbarkeit halber als einen Marker gemacht; Auditer kann das splitten, falls strukturierte Indizierung der beiden Felder gewünscht ist.
- **Tests haben keine Logger-Aufruf-Verifikation:** Wie im Plan vorgeschlagen, habe ich *keine* neuen Tests für die Logger-Aufrufe selbst geschrieben (kein `LoggerTesting`-Package). Die Verifikation, dass `LogInformation` mit den richtigen Args aufgerufen wurde, ist rein visuell im Log-File nach einem Smoke-Lauf. Auditer sollte das nicht als fehlende Test-Coverage werten — Logger-Aufrufe sind reine Beobachtbarkeit, nicht Programmlogik.

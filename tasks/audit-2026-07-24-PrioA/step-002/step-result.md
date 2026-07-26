---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 002
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T19:30:00+02:00
code_commit_hash: a9e41409fc0d58f3da651933aac434fee82dc99b
# Hinweis: den Commit, der DIESE Datei enthält (Coder-Skill Schritt 7),
# kann diese Datei denknotwendig nicht selbst zitieren — bei Bedarf per
# `git log --follow -- <Pfad-dieser-Datei>` nachschlagen.
status_after: done
---

# Result Step 002: F-SE-001 — LIKE-Wildcard-Injection in `BuildLikePattern` schließen + Längen-Cap

## Zusammenfassung

`BuildLikePattern` ist jetzt `internal static` mit Bracket-Escape in der korrekten Reihenfolge (`[` zuerst, dann `%`, dann `_`), und `SearchDocsAsync` validiert leere Queries (leere Liste) und zu lange Queries (`ArgumentException` mit Längen-Angabe). Die Schwellenwerte kommen aus `KnowHowToAi.Search.MaxQueryLength` (Default 200, in `appsettings.json` konfigurierbar) — kein Magic-Wert im Code. Sieben Tests in `BuildLikePatternTests` decken die Escape-Reihenfolge, Edge-Cases und das Verhalten bei leerer Eingabe ab; drei Doku-Sektionen wurden im selben Commit mitgepflegt. `DocsMcpTools` und `Program.cs` wurden mit-aktualisiert, um den neuen `maxQueryLength`-Parameter von `KnowHowToAiOptions` durchzureichen (notwendig, weil die Signatur-Erweiterung sonst die Solution bricht — nicht im Plan gelistet, aber zwingend für grünen Build).

## Geänderte Dateien

- `src/KnowHowToAI.Core/Configuration/KnowHowToAiOptions.cs` — Property `Search` (Typ `KnowHowToAiSearchOptions`) ergänzt.
- `src/KnowHowToAI.Core/Configuration/KnowHowToAiSearchOptions.cs` (neu) — `sealed record` mit `MaxQueryLength = 200` und `MaxResults = 50` (Default, `MaxResults` für Step 003 vorgemerkt).
- `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs` — `BuildLikePattern` von `private static` zu `internal static` umgestellt, mit Bracket-Escape in der Reihenfolge `[` → `[[]`, `%` → `[%]`, `_` → `[_]`. `SearchDocsAsync`-Signatur um `int maxQueryLength` erweitert, Early-Return-Guards für leere Query (→ `[]`) und zu lange Query (→ `ArgumentException`).
- `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` — Primary-Constructor um `int maxQueryLength` erweitert; reicht den Wert an `store.SearchDocsAsync` durch.
- `src/KnowHowToAI.Cli/Program.cs` — `DocsMcpTools` jetzt explizit als DI-Factory registriert, die `options.Search.MaxQueryLength` und `ILogger<DocsMcpTools>` auflöst (vorher wurde der Tool-Typ implizit von `WithToolsFromAssembly()` per `ActivatorUtilities` konstruiert — das funktioniert mit einem `int`-Primitive-Parameter nicht zuverlässig).
- `src/KnowHowToAI.Cli/appsettings.json` — Neuer `Search`-Sub-Block mit `MaxQueryLength: 200` und `MaxResults: 50` (zwischen `Validation` und Schluss-Klammer).
- `docs/04-Datenmodell-Validierung-Edgecases.md` — Abschnitt 1 (search_docs-Query): Zwei neue Absätze „Bracket-Escape in `BuildLikePattern`" und „Maximale Query-Länge" mit Verweis auf `KnowHowToAi.Search.MaxQueryLength`.
- `docs/03-Projektstruktur-und-Konfiguration.md` — Abschnitt 2: JSON-Beispiel um `Search`-Sub-Block erweitert, neue `Search`-Bullet in der Sub-Options-Aufzählung mit Verweis auf 04/F-PE-002 für `MaxResults`.
- `docs/02-Architektur-und-Techstack.md` — Abschnitt 4.D (search_docs-Tool-Block): Neue Zeile „Query-Semantik" mit Bracket-Escape-Hinweis und Längen-Cap-Verweis.
- `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs` (neu) — 7 `[Fact]`-Tests (Tests-Subject flach im Test-Root, analog zu `EnumParseHelpersTests` aus Step 001; Namespace `KnowHowToAI.Core.Tests`, nicht `Sync`-Sub-Namespace).

## Commit

- **Code-Commit-Hash:** `a9e41409fc0d58f3da651933aac434fee82dc99b`
- **Message:**
  ```
  fix(security): like-wildcard-injection und query-laengen-cap fuer search_docs

  Verhindert, dass LLM-kontrollierte query-Eingaben SQL-LIKE-Wildcards
  einschmuggeln oder via Pattern-Laenge einen trivialen DoS-Vektor gegen
  den SQL-Server ausloesen. Bracket-Escape + konfigurierbare
  Laengen-Obergrenze.

  - KnowHowToAiSearchOptions (Core/Configuration) mit MaxQueryLength=200
    und MaxResults=50 (von Step 003 konsumiert)
  - SqlDocumentsStore.BuildLikePattern: private -> internal static,
    Bracket-Escape ([->[[], %->[%], _->[_]) in dieser Reihenfolge
  - SqlDocumentsStore.SearchDocsAsync: leere Query gibt leere Liste
    zurueck, zu lange Query wirft ArgumentException
  - KnowHowToAiOptions.Search Property + appsettings.json Search-Subblock
  - DocsMcpTools + Program: maxQueryLength-Parameter aus
    Search.MaxQueryLength durchgereicht (notwendiger Aufrufer-Update)
  - 7 Tests in BuildLikePatternTests fuer Bracket-Escape, Reihenfolge
    und Edge-Cases
  - docs/02 Abschnitt 4.D, docs/03 Abschnitt 2, docs/04 Abschnitt 1:
    LIKE-Semantik und MaxQueryLength dokumentiert

  Refs: tasks/audit-2026-07-24-PrioA/step-002
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```
- **Branch:** `main`
- **Push:** nein (lokal)
- **Doku-Commit:** separater, zweiter Commit für `step-plan.md` (Status) + diese `step-result.md` (siehe Coder-Skill Schritt 7) — dessen Hash steht nicht hier drin (Selbstbezug), sondern in `git log`.

## Build-Output

```
dotnet build -c Release
→ 0 Warnung(en), 0 Fehler
→ 3 Projekte (Core, Cli, Tests) erfolgreich gebaut in ~2.8s
```

## Test-Output

```
dotnet test (über xUnit-v3 In-Process Runner)
→ Total: 72, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
→ Baseline 65 + 7 neue BuildLikePatternTests = 72 grün
→ AiNetLinterTest (Lauf gegen 7 neue/erweiterte Dateien): grün (Exit 0), aber Report zeigt 1 Violation in tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs:5 — wird in step-002/fix-01 behoben
```

## Abweichungen vom Plan

1. **`DocsMcpTools.cs` und `Program.cs` mit-aktualisiert** — der Plan listet nur `SqlDocumentsStore.cs` als zu ändernde Datei. Die `SearchDocsAsync`-Signatur-Erweiterung um `int maxQueryLength` bricht aber den einzigen Aufrufer (`DocsMcpTools.SearchDocsAsync`, Z. 27) und die DI-Konstruktion in `Program.cs` (Z. 130 via `WithToolsFromAssembly`). Ohne diese Mit-Anpassung wäre die Solution nach dem Code-Commit nicht baubar. Die Erweiterung von `DocsMcpTools` ist exakt das, was Step 003 sowieso vorsieht (siehe `step-003/step-plan.md`: „`DocsMcpTools`-Konstruktor ist um `maxQueryLength` und `maxResults` erweitert") — Step 003 muss dann nur noch `maxResults` hinzufügen.
2. **`DocsMcpTools` jetzt explizit per `AddSingleton<DocsMcpTools>(sp => ...)` registriert** — vorher wurde der Typ von `WithToolsFromAssembly()` implizit per `ActivatorUtilities.CreateInstance` aus der Service-Collection konstruiert. Das scheitert für einen `int`-Parameter ohne Typbindung in der Collection. Die Factory-Registrierung löst `SqlDocumentsStore`, `int maxQueryLength` (via `KnowHowToAiOptions.Search.MaxQueryLength`) und `ILogger<DocsMcpTools>` sauber aus dem DI-Container auf.
3. **Commit-Subject ist 77 Zeichen** (`fix(security): like-wildcard-injection und query-laengen-cap fuer search_docs`) — 5 Zeichen über der 72-Zeichen-Konvention aus `.agents/rules/03-git-workflow.mdc`. Der Subject wurde aber so explizit vom Orchestrator vorgegeben und daher 1:1 übernommen. Eine Kürzung (z. B. „query-cap" statt „query-laengen-cap") hätte die semantische Genauigkeit reduziert.
4. **`BuildLikePattern_AllowsBracketedPatternStillValidAfterEscape` aus dem Plan** wurde mit `BuildLikePattern_AllowsNormalSubstring` konsolidiert (gleicher Test: Normal-String `routing` → `%routing%` ohne Escape). Beide Test-Beschreibungen zielen auf dasselbe Verhalten; eine Trennung wäre redundantes `[Fact]`-Paar.
5. **Test-Klasse flach in `tests/KnowHowToAI.Core.Tests/BuildLikePatternTests.cs`** statt im `Sync/`-Subordner — folgt der Step-001-Konvention und der direkten Vorgabe im Orchestrator-Prompt. Der Plan schlug `Sync/BuildLikePatternTests.cs` vor, was von der bestehenden Test-Organisation (alle Tests flach, siehe `SqlIdentifierValidatorTests.cs` für ein direktes Pendant zu `SqlIdentifierValidator`) abweicht.
6. **Test-Methoden als `[Fact]` statt `[Theory]/[InlineData]`** — der Plan schlug parametrisierte `[Theory]`-Tests vor, der Orchestrator-Prompt listete aber sechs spezifische Test-Namen auf. Ich habe beide Anforderungen kombiniert: pro spezifischem Test-Name ein `[Fact]`, dafür sieben statt sechs Tests (eine Test-Methode mehr als gefordert, deckt zusätzlich den `BuildLikePattern_AllThreeWildcardsInOneInput_AllEscaped`-Fall ab, der im Plan als separater Test gelistet war).

## Beobachtungen

- **Plan-Tippfehler in Test-Erwartung:** Der Plan listet für `BuildLikePattern("[%]")` das erwartete Ergebnis `"%[[]%[]]%"`. Korrekt ist nach der vorgeschriebenen Reihenfolge (`[` zuerst, dann `%`, dann `_`) das Ergebnis `"%[[][%]]%"`. Mein Test verwendet die korrekte Variante. Der Plan-Hinweis im selben Bullet („wenn `[` *zuletzt* ersetzt würde, käme `%[[%]]%` raus") stimmt mit der korrekten Erwartung überein — die Erwartung selbst ist der Tippfehler. Der Auditer sollte das gegenchecken.
- **`SqlDocumentsStore.SearchDocsAsync` (leerer Query, zu lange Query)** ist im Plan explizit als „nicht direkt testbar" markiert (DB-abhängig, Backlog F-TS-001). Die Längen-Validierung ist trivial genug für visuelles Review; ein direkter Test wäre nur durch Dependency-Injection (z. B. SQLite-In-Memory) oder Extrahieren der Validierung in eine `internal static ValidateQuery`-Methode möglich. Beide Optionen würden Scope dieses Steps überschreiten. Der Plan merkt korrekt an, dass Step 003 diese indirekte Test-Möglichkeit schaffen könnte.
- **`MaxResults` ist in `KnowHowToAiSearchOptions` und `appsettings.json` deklariert, aber in `SearchDocsAsync` noch nicht verwendet** — das ist Absicht. Step 003 fügt das `TOP (@MaxResults)` und die `SearchResult`-Rückgabe ein. Diese Property hier vorab anzulegen, verhindert eine Folge-Änderung an `appsettings.json` (Konzept-Empfehlung im Plan).
- **`SqlDocumentsStore` Konstruktor ist unverändert geblieben** (kein `maxQueryLength` als zusätzlicher ctor-Param) — der Wert wird per Methoden-Parameter durchgereicht, nicht im Store gehalten. Das hält den Store zustandslos und konsistent mit den anderen Read-Methoden (`ListChildrenAsync`, `GetDocAsync`).
- **`SqlDocumentsStore.SearchDocsAsync` verwendet `MAXConstructorDependencies: 5` für die `DocsMcpTools`-Klasse** — der Primary-Constructor hat jetzt 3 Parameter (vorher 2), bleibt deutlich unter dem 5er-Limit. AiNetLinter bestätigt das mit 0 Verstößen.
- **`GetRequiredService` löst `KnowHowToAiOptions` aus der ServiceCollection** — `options` wird in `Program.cs:129` als `AddSingleton(options)` registriert, dadurch im DI-Container verfügbar. Sauber.

## Bekannte Unschärfen

- **Subject-Länge:** Siehe Abweichung 3. Der Subject ist 5 Zeichen über der 72-Zeichen-Konvention — bewusst übernommen, weil explizit vom Orchestrator vorgegeben.
- **`SearchDocsAsync` Validierung (leere Query, `ArgumentException`):** Nicht direkt per Test abgesichert (DB-abhängig). Der Code-Pfad ist aber 5 Zeilen lang, visuell prüfbar, und folgt einem etablierten Early-Return-Pattern. Auditer sollte im Hinterkopf behalten, dass ein zukünftiger Refactor (z. B. die in den Plan-Notes erwähnte `internal static ValidateQueryOrThrow`-Extraktion) diese Lücke ohnehin schließen würde.
- **`MaxResults` zwischen Step 002 und Step 003 ungenutzt:** die Property ist da, wird aber erst in Step 003 in `SearchDocsAsync` per `TOP (@MaxResults)` aktiv genutzt. Der Plan dokumentiert das bewusst; in `appsettings.json` ist der Wert dennoch gesetzt, damit Step 003 keine Datei-Änderung an `appsettings.json` braucht.
- **Plan vs. Orchestrator-Prompt Diskrepanz bei Test-Anzahl:** Plan sagt 7 Tests, Orchestrator-Prompt sagt 6. Ich habe 7 geliefert, weil die `BuildLikePattern_AllThreeWildcardsInOneInput_AllEscaped`-Variante wichtige zusätzliche Coverage bietet und im Plan explizit als Test gefordert war.

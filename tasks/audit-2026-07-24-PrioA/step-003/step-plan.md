---
status: done
type: step-plan
task: audit-2026-07-24-PrioA
step: 003
title: "F-PE-002 — search_docs mit TOP-Cap, Title-Ranking und Truncation-Marker fürs LLM"
estimated_risk: medium
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T18:00:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#fix-3--f-pe-002-searchdocsasync-ohne-top-limit"
  - "tasks/audit-2026-07-24/_plan/prioritized-fixes.md#fix-12--f-pe-002-top-cap-für-searchdocsasync--title-ranking"
---

# Step 003: F-PE-002 — `search_docs` mit TOP-Cap, Title-Ranking und Truncation-Marker fürs LLM

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `Konzept.md` Sektion „Fix 3 — F-PE-002: `SearchDocsAsync`
  ohne `TOP`/`LIMIT`"
- **Phase / Priorität:** Mittelfristig (Performance, High), aber zwingend
  *nach* Step 002 (gleicher Code-Pfad)
- **Abhängigkeiten:** **baut auf Step 002 auf** — die `SearchDocsAsync`-
  Signatur mit `maxQueryLength` ist bereits erweitert; `MaxResults`-
  Property ist bereits in `KnowHowToAiSearchOptions` deklariert; der
  `InternalsVisibleTo`-Eintrag existiert bereits.

## Intention

`SqlDocumentsStore.SearchDocsAsync` hat heute **drei** Probleme:
(1) Ohne `TOP`-Cap können hunderte bis tausende Treffer zurückkommen,
was das LLM-Token-Budget sprengt; (2) Sortierung ist alphabetisch nach
`title`, die relevantesten Treffer (Title-Treffer) landen verstreut;
(3) das LLM bekommt *keine* Möglichkeit zu sehen, ob die Trefferliste
gekappt wurde — wenn `truncated: true` nicht in der Antwort steht, kann
das LLM nicht wissen, dass es noch mehr Treffer gibt und handelt auf
unvollständiger Information.

Nach diesem Step:
- SQL nutzt `TOP (@MaxResults)` und Title-Ranking (`CASE WHEN title LIKE
  @Pattern THEN 0 ELSE 1 END` zuerst, dann alphabetisch nach `title`).
- Die SQL-Antwort liefert zusätzlich `TotalCount` via `COUNT(*) OVER()`,
  sodass `Truncated` aus `TotalCount > Results.Count` ableitbar ist —
  *eine* SQL-Round-Trip, kein Race-Condition-Risiko.
- Ein neuer Core-Record `SearchResult(IReadOnlyList<DocumentSummary>,
  bool Truncated)` ist der neue Rückgabetyp.
- `ResponseSize.Measure` erkennt `SearchResult` und misst
  `search.Results.Count` (sonst fällt die Log-Größe für `search_docs`
  auf 0 zurück, irreführend).
- `DocsMcpTools.SearchDocsAsync` reicht `result` (den Wrapper) als
  Tool-Antwort durch, **nicht** `result.Results` — sonst geht der
  `truncated`-Marker verloren, den das LLM laut Querschnittsregel sehen
  muss.

## Konkrete Änderungen

### Datei 1: `src/KnowHowToAI.Core/Documents/SearchResult.cs` (neu)

- **Was:** Neuer Core-Record:
  ```csharp
  namespace KnowHowToAI.Core.Documents;

  public sealed record SearchResult(
      IReadOnlyList<DocumentSummary> Results,
      bool Truncated);
  ```
- **Warum:** Konsequent mit den anderen Domain-Records (`DocumentSummary`,
  `DocumentDetail`, `Document`, `ValidationError`). Positional record,
  sealed, `bool`-Property für Wert-Type-Semantik, `IReadOnlyList<>`-
  Property für die Trefferliste. Wird in `SqlDocumentsStore` konstruiert
  und vom MCP-Tool durchgereicht.
- **Hinweise:** Keine Methoden, keine Validierung — reiner
  Daten-Container. Analog zu `DocumentSummary.cs:1`
  (`public sealed record DocumentSummary(string Slug, string Title);`).

### Datei 2: `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs`

- **Was:** `SearchDocsAsync` umbauen:
  1. **Signatur erweitern** um `maxResults`:
     ```csharp
     public async Task<SearchResult> SearchDocsAsync(
         string query, int maxQueryLength, int maxResults, CancellationToken cancellationToken)
     ```
  2. **SQL** auf neuen Shape umstellen:
     ```sql
     SELECT TOP (@MaxResults) slug AS Slug, title AS Title,
            COUNT(*) OVER() AS TotalCount
     FROM dbo.{{DocumentsTableName}}
     WHERE title LIKE @Pattern OR content LIKE @Pattern
        OR tags LIKE @Pattern OR synonyms LIKE @Pattern
     ORDER BY
         CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,
         title;
     ```
     In C#-String-Interpolation:
     ```csharp
     $"""
     SELECT TOP (@MaxResults) slug AS Slug, title AS Title,
            COUNT(*) OVER() AS TotalCount
     FROM {_table}
     WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
     ORDER BY
         CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,
         title;
     """
     ```
  3. **Dapper-Mapping** auf neuen Row-Type umstellen — neuer `private
     sealed record SearchRow(string Slug, string Title, int TotalCount)`
     (lokaler Helper, analog zu `DocumentRow` in derselben Klasse).
     `SqlDocumentsStore.SearchDocsAsync` mappt jede Row auf
     `DocumentSummary` und sammelt parallel `TotalCount` (kommt aus
     jeder Row identisch, also `row.TotalCount` aus der ersten Row).
  4. **Result-Konstruktion** mit `Truncated = (totalCount > results.Count)`.
- **Warum:**
  - `TOP (@MaxResults)` schützt das Token-Budget.
  - `COUNT(*) OVER()` ist eine Window-Function — *eine* SQL-Round-Trip
    statt zweier (kein Race-Condition-Risiko zwischen `COUNT(*)` und
    `SELECT TOP`). Konzept-Vorgabe explizit so.
  - Title-Ranking via `CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END`
    ist eine Heuristik ohne zusätzliche Komplexität — Title-Treffer
    kommen zuerst, dann alphabetisch.
  - `Truncated` leitet sich aus `TotalCount > Results.Count` ab: ist
    `TotalCount = 50` und `Results.Count = 50`, könnte es noch mehr
    geben (Truncated) oder genau 50 sein (nicht Truncated). Da `TOP 50`
    ohne `WITH TIES` exakt 50 Rows liefert, ist `Truncated = totalCount
    > results.Count` korrekt.
- **API-Bruch:** `SearchDocsAsync` wechselt von
  `Task<IReadOnlyList<DocumentSummary>>` zu `Task<SearchResult>`. Das ist
  ein *bewusst akzeptierter* Bruch (Konzept: einmaliger Bruch, kein
  Migrationspfad nötig, da v1.0.2-Tool und Konsumenten sind LLMs, die
  die Description in Step 004 neu lesen).
- **Hinweise zur Methodenlänge:** Aktuell ist `SearchDocsAsync` ~15 LOC.
  Mit der Erweiterung kommt es auf ~25-30 LOC — bleibt unter dem
  AiNetLinter-Limit von 60.

### Datei 3: `src/KnowHowToAI.Core/Logging/ResponseSize.cs`

- **Was:** Neuen Switch-Arm vor `_ => 0` einfügen:
  ```csharp
  SearchResult search => search.Results.Count,
  ```
  Import `using KnowHowToAI.Core.Documents;` ist schon vorhanden.
- **Warum:** Sonst fällt der `SearchResult`-Fall auf `_ => 0` durch und
  der Log-Eintrag in `DocsMcpTools` (`search_docs response: {Size}`)
  zeigt eine `Size=0` an — irreführend für die Beobachtbarkeit.
- **Reihenfolge im Switch:** Case-Reihenfolge in C#-Switch-Expressions
  ist irrelevant für die Semantik, aber für die Lesbarkeit:
  `IReadOnlyCollection<DocumentSummary>` zuerst (häufigster Fall),
  `SearchResult` als Sonderfall mit `Results.Count` (semantisch anders),
  dann `DocumentDetail`, dann `null`, dann `_ => 0`. Hängt der Coder
  nach Geschmack.

### Datei 4: `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs`

- **Was:** `search_docs`-Tool-Methode anpassen:
  ```csharp
  [McpServerTool(Name = "search_docs"), Description("...")]
  public async Task<SearchResult> SearchDocsAsync(string query, CancellationToken cancellationToken)
  {
      logger.LogInformation("search_docs(query={Query})", query);
      var result = await store.SearchDocsAsync(query, _maxQueryLength, _maxResults, cancellationToken);
      logger.LogInformation("search_docs response: {Size}", ResponseSize.Measure(result));
      return result;  // nicht result.Results — Truncated-Marker muss beim LLM ankommen
  }
  ```
  **Wichtig:** `maxQueryLength` und `maxResults` müssen in der Klasse
  verfügbar sein. Zwei Optionen:
  - (A) Felder im Konstruktor: `private readonly int _maxQueryLength,
    private readonly int _maxResults;` — `DocsMcpTools` wird um 2
    Konstruktor-Parameter erweitert. AiNetLinter-Limit
    `MaxConstructorDependencies: 5` (aktuell 2: `store`, `logger` — mit
    den zwei neuen Params: 4, bleibt unter Limit).
  - (B) `IOptions<KnowHowToAiOptions>` injizieren — fügt eine neue
    Core-Abhängigkeit ein (Microsoft.Extensions.Options) und eine
    Konfigurationsklasse, die derzeit nirgends injiziert wird. **Nicht
    empfohlen** — verletzt die „bewusst einfacher Code"-Regel
    (`01-code-style.mdc`) und macht den Test-Setup für `DocsMcpTools`
    komplizierter.
  → **Option A** wählen.
  - **Composition-Root-Anpassung in Step 005:** wenn `DocsMcpTools`
    weitere Konstruktor-Parameter bekommt, ist die saubere
    `DocsMcpTools`-Konstruktion Sache des Composition-Root. Step 005
    baut die `BuildStore`/`BuildDocsMcpTools`-Factory in `Program.cs`
    ohnehin aus — das `RunServer`-Wiring wird dann
    `new DocsMcpTools(store, logger, options.Search.MaxQueryLength,
    options.Search.MaxResults)` aufrufen.
  - **Übergangs-Lösung für Step 003:** der `DocsMcpTools`-Konstruktor
    wird in Step 003 um die zwei Parameter erweitert, der Aufruf
    in `Program.cs` (`RunServer`) wird ebenfalls in Step 003 angepasst
    (notwendig, sonst kompiliert der Build nicht). Step 005 *konsolidiert*
    diese Konstruktion in die Factory. Diese minimale Anpassung in
    `Program.cs` zählt nicht als DI-Inkonsistenz, weil sie rein
    konstrukt-mechanisch ist und in Step 005 ohnehin ersetzt wird.
- **Warum:** `result` durchreichen, nicht `result.Results` — der
  `Truncated`-Marker muss in der Tool-Antwort landen, sonst ist die
  Querschnittsregel „LLM-Sichtbarkeit von Begrenzungen" verletzt.

### Datei 5: `src/KnowHowToAI.Cli/Program.cs` (`RunServer`-Methode, Zeile 130)

- **Was:** Die Zeile
  ```csharp
  builder.Services.AddSingleton(new SqlDocumentsStore(options.ConnectionString, options.DocumentsTableName));
  ```
  erweitern um die zwei Werte aus `options.Search` (oder besser: in
  eine `BuildStore`-Helper-Funktion auslagern, die in Step 005
  verfeinert wird). Minimalvariante für Step 003:
  ```csharp
  builder.Services.AddSingleton(sp => new SqlDocumentsStore(
      options.ConnectionString, options.DocumentsTableName,
      sp.GetRequiredService<ILogger<SqlDocumentsStore>>()));
  builder.Services.AddSingleton(sp => new DocsMcpTools(
      sp.GetRequiredService<SqlDocumentsStore>(),
      sp.GetRequiredService<ILogger<DocsMcpTools>>(),
      options.Search.MaxQueryLength,
      options.Search.MaxResults));
  ```
  Hintergrund: `DocsMcpTools` braucht jetzt 4 Konstruktor-Parameter
  (siehe Datei 4), und der `AddSingleton<TService>(TService)`-
  Overload, der eine Instanz nimmt, akzeptiert keine Service-Provider-
  Auflösung. Umstellung auf Factory-Lambda nötig.
- **Warum:** Sonst kompiliert der Build nicht. Diese Factory-Lambda-
  Variante ist *bereits* der saubere Composition-Root-Pfad, den
  Step 005 nur noch verfeinert (Logger-Injection hinzu). Die
  minimale Vorab-Verschiebung an dieser Stelle ist kein
  Inkonsistenz-Drift — sie ist Vorgriff auf den F-AR-002-Schritt.
- **`ImportService`/`ExportService`:** unverändert in diesem Step.
  Step 005 erweitert deren Konstruktoren.

### Datei 6: `docs/02-Architektur-und-Techstack.md` (Abschnitt 4.D, search_docs-Block)

- **Was:** Den `search_docs`-Tool-Beschreibungs-Block (Z. 112-114) so
  erweitern, dass die Response-Shape dokumentiert ist:
  ```
  * **Response-Shape:** `{ results: DocumentSummary[], truncated: bool }`.
    `truncated: true` bedeutet, dass die Suche mehr Treffer hat als
    `MaxResults` (Default 50); das LLM sollte die Suche verfeinern
    statt alle Treffer zu erwarten. Volle Tool-Description siehe
    [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md).
  ```
  + einen Hinweis auf das Title-Ranking und die deterministische
  Sortierung.
- **Warum:** Konzept-Vorgabe, plus Konsistenz mit Step 002 / Step 004
  (Doku-Continuity).

### Datei 7: `docs/04-Datenmodell-Validierung-Edgecases.md` (Abschnitt 1)

- **Was:** Den `search_docs`-Query-Block (Z. 38-49) komplett
  aktualisieren:
  1. SQL-Listing auf den neuen Shape (`TOP (@MaxResults)`,
     `COUNT(*) OVER()`, `ORDER BY` mit Title-Ranking).
  2. Erklärungstext erweitern:
     ```
     - **`TOP (@MaxResults)`** schützt das Token-Budget des LLM vor
       Massen-Treffern bei sehr breiten Suchen. Default 50, konfigurierbar
       via `KnowHowToAi.Search.MaxResults` in `appsettings.json`.
     - **`COUNT(*) OVER()`** liefert die Treffer-Gesamtzahl *ohne*
       TOP-Begrenzung in derselben Query — daraus wird `Truncated` in
       der Antwort abgeleitet.
     - **Title-Ranking:** `ORDER BY (CASE WHEN title LIKE @Pattern THEN 0
       ELSE 1 END), title` — Title-Treffer kommen zuerst, dann
       alphabetisch. Bewusst keine komplexere Ranking-Heuristik (kein
       Full-Text-Search, keine Score-Berechnung), konsistent mit der
       `LIKE`-basierten Architektur.
     - **Response-Shape:** `SearchResult { results, truncated }` — der
       `truncated`-Marker ist die einzige Möglichkeit für das LLM zu
       erkennen, dass die Trefferliste gekappt wurde. Siehe auch
       [02, Abschnitt 4.D](02-Architektur-und-Techstack.md).
     ```
- **Warum:** Konzept-Vorgabe; exakte SQL und Semantik waren vorher
  unvollständig dokumentiert.

## Tests

- [ ] `SearchResultTests.Truncated_IsTrue_WhenTotalCountExceedsResultsCount`
      — `new SearchResult([a, b], Truncated: true)` für `TotalCount=5,
      Results.Count=2` (Konzept-Beispiel)
- [ ] `SearchResultTests.Truncated_IsFalse_WhenAllHitsFit`
      — `new SearchResult([a, b, c], Truncated: false)` für
      `TotalCount=3, Results.Count=3`
- [ ] `SearchResultTests.EmptyResults_NeverTruncated` — leere
      Result-Liste, `TotalCount=0` → `Truncated=false` (Konsistenz mit
      Step 002 „leerer Query")
- [ ] `SearchResultTests.IsSealedRecord_ValueSemanticsForBoolProperty`
      — Smoke-Test: zwei `SearchResult` mit gleichen Werten sind via
      `==` gleich (positional record, Wert-Type-Semantik für
      `bool`-Property)
- [ ] `ResponseSizeTests.Measure_SearchResult_ReturnsResultsCount` —
      `ResponseSize.Measure(new SearchResult([a, b, c], Truncated:
      false))` liefert `3` (neuer Switch-Arm)
- [ ] `ResponseSizeTests.Measure_SearchResultEmpty_ReturnsZero` —
      `ResponseSize.Measure(new SearchResult([], Truncated: false))`
      liefert `0`

**Test-Datei (neu, 1):** `tests/KnowHowToAI.Core.Tests/Documents/SearchResultTests.cs`
im Namespace `KnowHowToAI.Core.Tests.Documents`. 4 Tests, `[Fact]`.

**Test-Datei (erweitert):** `tests/KnowHowToAI.Core.Tests/ResponseSizeTests.cs`
um 2 Tests erweitern (siehe oben).

**Bekannte Test-Baseline:** 68 → 74 grün.

**Hinweis zu `SearchDocsAsync`-Tests (TOP, Ranking, TotalCount):** in
diesem Step *nicht* direkt testbar (DB-abhängig, Backlog F-TS-001).
Die `SearchResult`-Logik selbst (Truncated-Berechnung) ist isoliert
testbar (siehe oben). Der Coder kann in einem zukünftigen Schritt eine
`internal static SearchResult BuildResult(IReadOnlyList<DocumentSummary>
results, int totalCount)`-Helper-Methode extrahieren, um die
`Truncated`-Ableitung unabhängig vom SQL zu testen — nicht zwingend
für diesen Step.

## Definition of Done

- [ ] `SearchResult.cs` existiert mit `sealed record` +
      `IReadOnlyList<DocumentSummary> Results` + `bool Truncated`
- [ ] `SqlDocumentsStore.SearchDocsAsync` nutzt
      `TOP (@MaxResults)` + `COUNT(*) OVER() AS TotalCount` +
      `ORDER BY (CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END), title`
- [ ] `SearchDocsAsync`-Rückgabetyp ist `Task<SearchResult>`
- [ ] `ResponseSize.Measure` hat den `SearchResult`-Switch-Arm
- [ ] `DocsMcpTools.SearchDocsAsync` reicht `result` (Wrapper) durch,
      nicht `result.Results`
- [ ] `DocsMcpTools`-Konstruktor ist um `maxQueryLength` und
      `maxResults` erweitert
- [ ] `Program.cs` `RunServer` baut `SqlDocumentsStore` + `DocsMcpTools`
      via Factory-Lambda mit korrekten Service-Provider-Auflösungen
- [ ] `SearchResultTests` (4 Tests) + `ResponseSizeTests`-Erweiterung
      (2 Tests) vorhanden und grün
- [ ] `docs/02` Abschnitt 4.D + `docs/04` Abschnitt 1 sind aktualisiert
- [ ] `dotnet build -c Release` — 0 Warnings, 0 Errors
- [ ] `dotnet test` — 74 grün
- [ ] AiNetLinter 0 neue Verstöße
- [ ] Commit mit Subject
      `fix(perf): search_docs mit top-cap, title-ranking und truncation-marker`,
      Body: „Verhindert Token-Budget-Sprengung bei breiten Suchen via
      TOP(@MaxResults) und gibt dem LLM via `truncated`-Marker in der
      Antwort die Möglichkeit, eine gekappte Trefferliste zu erkennen
      und die Suche zu verfeinern. Title-Ranking verbessert die
      Treffer-Reihenfolge. `SearchResult` ist der neue Response-Shape,
      `ResponseSize.Measure` erkennt ihn. API-Bruch am
      `SearchDocsAsync`-Rückgabetyp bewusst akzeptiert (kein
      Migrationspfad — v1.0.2-Tool, LLMs lesen Description neu)."
      Trailer: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- [ ] `step-003/step-result.md` geschrieben mit Commit-Hash
- [ ] `status` in `step-plan.md` von `in_progress` auf `done (pending audit)`
      gesetzt

## Rules-Refs

- `.agents/rules/01-code-style.mdc` — Early Returns beibehalten,
  positional record für `SearchResult` (konsistent mit `Document`,
  `DocumentSummary`, `DocumentDetail`)
- `.agents/rules/02-testing.mdc` — `SearchResultTests` mit
  `[Fact]`-Cases analog zu `ResponseSizeTests.cs`
- `.agents/rules/03-git-workflow.mdc` — Conventional Commit, deutsch
- `.agents/rules/05-documentation.mdc` — Doku im selben Commit
- `.agents/rules/06-configuration.mdc` — `MaxResults` aus
  `KnowHowToAiSearchOptions`, nicht als Literal im SQL
- `.agents/rules/AiNetLinter.mdc` — `DocsMcpTools`-Konstruktor
  bekommt 4 Params (Limit ist 5); `SearchDocsAsync` bleibt < 60 LOC

## Bekannte Ausnahmen

- **`SqlDocumentsStore.SearchDocsAsync` direkte Tests (TOP, Ranking,
  TotalCount):** in diesem Step *nicht* möglich (DB-abhängig,
  Backlog F-TS-001). Die `Truncated`-Ableitungs-Logik ist über
  `SearchResultTests` indirekt abgesichert.
- **`DocsMcpTools`-Test:** schon vor diesem Step nicht direkt testbar
  (`*.Cli` hat `EnableTestSentinel: false` per AiNetLinter-Override,
  siehe `docs/03` Abschnitt 4). Der Konstruktor-Umbau wird per
  Build-Erfolg + manuellen Smoke-Test verifiziert (siehe „Bedingt
  in DoD" im Konzept).

## Code-Skizze

```csharp
// SearchDocsAsync - neuer Shape
public async Task<SearchResult> SearchDocsAsync(
    string query, int maxQueryLength, int maxResults, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return new SearchResult([], Truncated: false);
    }
    if (query.Length > maxQueryLength)
    {
        throw new ArgumentException(
            $"search_docs query ist {query.Length} Zeichen lang, max {maxQueryLength}.",
            nameof(query));
    }

    await using var connection = new SqlConnection(_connectionString);
    var rows = await connection.QueryAsync<SearchRow>(new CommandDefinition(
        $"""
        SELECT TOP (@MaxResults) slug AS Slug, title AS Title,
               COUNT(*) OVER() AS TotalCount
        FROM {_table}
        WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
        ORDER BY
            CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,
            title;
        """,
        new { Pattern = BuildLikePattern(query), MaxResults = maxResults },
        cancellationToken: cancellationToken));

    var rowList = rows.AsList();
    var results = rowList.Select(r => new DocumentSummary(r.Slug, r.Title)).ToList();
    var totalCount = rowList.Count > 0 ? rowList[0].TotalCount : 0;
    return new SearchResult(results, Truncated: totalCount > results.Count);
}

private sealed record SearchRow(string Slug, string Title, int TotalCount);
```

## Notes

- **Reihenfolge im Loop:** Step 003 kommt nach Step 002 und vor
  Step 004. Step 002 schloss den Sicherheits-Vektor; Step 003 fügt die
  Performance-Begrenzung + LLM-UX-Marker hinzu. Step 004 dokumentiert
  dann das geänderte Verhalten in den Tool-Descriptions.
- **`SqlDocumentsStore.SearchRow` (private record):** bewusst `private`,
  damit der Row-Type nicht aus Core leaked. Konsistent mit dem
  bestehenden `DocumentRow` (Zeile 113). Alternativ wäre Dappers
  `QueryAsync<dynamic>` mit Index-Zugriff — vermeide ich, weil
  typed Records die AiNetLinter-Analyse (`DetectAndBanPhantomDependencies`)
  sauber halten.
- **API-Bruch-Kommunikation:** Da der `SearchDocsAsync`-Rückgabetyp
  wechselt, müssen alle Aufrufer in einem Schritt angepasst werden
  — `DocsMcpTools` und zukünftige direkte Aufrufer. Es gibt aktuell
  keine direkten Aufrufer außer `DocsMcpTools` (keine Tests, keine
  Service-Delegation), also bleibt der Bruch lokal.
- **Step 005 baut auf Step 003 auf:** `DocsMcpTools` bekommt in
  Step 005 eine `ILogger<DocsMcpTools>`-Injection (ist schon da!) +
  die Factory-Factory-Verschönerung in `Program.cs`. Die in Step 003
  eingeführten Factory-Lambdas in `Program.cs RunServer` werden in
  Step 005 *in eine gemeinsame Factory-Funktion* zusammengeführt
  (genau das, was F-AR-001 mitkonsolidiert). Keine Doppel-Arbeit.
- **Konzept-Detail „Title-Ranking":** Konzept nennt die `CASE WHEN`-Form
  im SQL-Listing. Alternativen (z. B. separater Index auf
  `(CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END)`) sind bewusst
  Backlog — der implizite `title`-Index reicht für v1.
- **`MaxResults=0`-Edge-Case:** Wenn ein Betreiber `MaxResults=0` in
  `appsettings.json` setzt, liefert die Query 0 Rows → leere
  Results-Liste, `Truncated=false` (totalCount wäre auch 0, weil
  kein Row liefert `TotalCount`). Akzeptables Verhalten — keine
  Sonderbehandlung nötig.

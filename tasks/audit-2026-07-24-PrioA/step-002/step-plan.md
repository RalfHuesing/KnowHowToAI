---
status: done
type: step-plan
task: audit-2026-07-24-PrioA
step: 002
title: "F-SE-001 — LIKE-Wildcard-Injection in BuildLikePattern schließen + Längen-Cap"
estimated_risk: medium
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T18:00:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#fix-2--f-se-001-like-wildcard-injection-in-buildlikepattern"
  - "tasks/audit-2026-07-24/_findings/F-SE-001-like-wildcard-injection.md"
---

# Step 002: F-SE-001 — LIKE-Wildcard-Injection in `BuildLikePattern` schließen + Längen-Cap

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `Konzept.md` Sektion „Fix 2 — F-SE-001: LIKE-Wildcard-Injection
  in `BuildLikePattern`"
- **Phase / Priorität:** Sofort (Sicherheit, High) — laut
  `_plan/prioritized-fixes.md` Security-Tiebreak vor Performance
- **Abhängigkeiten:** keine — nutzt aber `InternalsVisibleTo`-Mechanismus
  (führt ihn im Core.csproj ein, falls nicht schon durch Step 001 geschehen)

## Intention

`SqlDocumentsStore.SearchDocsAsync` ist heute durch zwei
**sicherheits- und verfügbarkeitsrelevante** Vektoren angreifbar:
(1) LLM-kontrollierte `query`-Eingaben werden *unverändert* in ein
LIKE-Pattern interpoliert (`%`, `_`, `[` als Wildcards), was Token-Budget-
Sprengung erlaubt; (2) keine Längen-Begrenzung, was einen trivialen
DoS-Vektor gegen den lokalen SQL-Server öffnet. Nach diesem Step sind
beide Vektoren geschlossen: `BuildLikePattern` escapt die SQL-LIKE-
Sonderzeichen via Bracket-Form (`[` / `]`), und eine konfigurierbare
Längen-Obergrenze (`KnowHowToAi.Search.MaxQueryLength`, Default 200)
verhindert übergroße Patterns. Der Schwellenwert liegt in
`appsettings.json` (Magic-Wert-Regel) statt als Literal im Code.

## Konkrete Änderungen

### Datei 1: `src/KnowHowToAI.Core/Configuration/KnowHowToAiOptions.cs`

- **Was:** Eine neue Sub-Options-Klasse `KnowHowToAiSearchOptions` einführen
  und als Property auf `KnowHowToAiOptions` ergänzen. Inhalt:
  ```csharp
  public sealed record KnowHowToAiSearchOptions
  {
      public int MaxQueryLength { get; init; } = 200;
      public int MaxResults { get; init; } = 50;
  }
  ```
  `MaxResults` wird in Step 003 (F-PE-002) konsumiert — hier nur Property
  + Default deklarieren, noch keine Verwendungsstelle. Hält den Step
  atomar (ein Commit pro Finding).
- **Warum:** Querschnittsregel „Magic-Werte in `appsettings.json`" — die
  Schwellenwerte gehören konfigurierbar in `appsettings.json` statt als
  Literale im Code. Konsistent mit `KnowHowToAiLoggingOptions` und
  `KnowHowToAiValidationOptions`.
- **Hinweise:** `sealed record`, Property-Init-Syntax (analog zu
  `KnowHowToAiValidationOptions`). Keine XML-Docs.

### Datei 2: `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs`

- **Was:** Mehrere Änderungen an `SearchDocsAsync` und `BuildLikePattern`:
  1. `SearchDocsAsync`-Signatur erweitern:
     ```csharp
     public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(
         string query, int maxQueryLength, CancellationToken cancellationToken)
     ```
  2. Methode-Implementierung anpassen (Early Returns, keine Schachtelung):
     ```csharp
     if (string.IsNullOrWhiteSpace(query)) return [];
     if (query.Length > maxQueryLength)
     {
         throw new ArgumentException(
             $"search_docs query ist {query.Length} Zeichen lang, max {maxQueryLength}.",
             nameof(query));
     }
     // ... rest wie gehabt, aber mit BuildLikePattern(query) statt BuildLikePattern(query)
     ```
     *Hinweis:* Die Aufrufstelle von `BuildLikePattern` bleibt im
     bestehenden `CommandDefinition`-Block unverändert; die neue
     Validierung kommt davor.
  3. `BuildLikePattern` von `private static` zu `internal static`
     umstellen und mit Escape-Logik versehen:
     ```csharp
     internal static string BuildLikePattern(string query)
     {
         var escaped = query
             .Replace("[", "[[]")
             .Replace("%", "[%]")
             .Replace("_", "[_]");
         return $"%{escaped}%";
     }
     ```
- **Warum:** `internal static` ist nötig, damit `tests/KnowHowToAI.Core.Tests`
  die Methode ohne Reflection testen kann (`InternalsVisibleTo`-Eintrag
  im csproj, siehe Datei 4). Escape-Logik folgt dem etablierten
  Bracket-Escape für SQL-Server-`LIKE` (siehe Konzept §"Fix-Detail
  F-SE-001"). Reihenfolge der `.Replace`-Aufrufe wichtig: zuerst `[` → `[[]`,
  sonst würden die später eingefügten `[` selbst wieder escapet.
- **Edge-Case „leerer Query":** weiterhin leere Liste (kein Fehler), wie
  heute schon der Fall (außer in der heutigen Implementierung gibt es
  diesen Guard nicht — der ist neu). Hintergrund: docs/04 Edge Case 4.2
  verlangt „leere DB / kein Fehler"; konsistent damit ist „leerer
  Query / leere Liste / kein Fehler".
- **Edge-Case „Query zu lang":** `ArgumentException` (nicht
  `InvalidOperationException`), weil es ein Programmier-/Eingabefehler
  ist, kein Konfigurationsfehler. Konzept-Vorgabe explizit so.

### Datei 3: `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj`

- **Was:** Neuen `ItemGroup` hinzufügen, damit `BuildLikePattern` für
  Tests zugänglich ist:
  ```xml
  <ItemGroup>
    <InternalsVisibleTo Include="KnowHowToAI.Core.Tests" />
  </ItemGroup>
  ```
- **Warum:** Saubere Test-Strategie statt Reflection (Konzept
  explizit so vorgegeben, „Standard-Pattern in .NET"). Diese Zeile
  ist auch Voraussetzung dafür, dass Step 001 `EnumParseHelpers`
  testen konnte (falls dort nicht schon geschehen — Schaden tut's
  nicht, ist idempotent).

### Datei 4: `src/KnowHowToAI.Cli/appsettings.json`

- **Was:** Unter `KnowHowToAi` ein neues Sub-Objekt `Search` einfügen
  (zwischen `Validation` und Schluss-Klammer):
  ```json
  ,
  "Search": {
    "MaxQueryLength": 200,
    "MaxResults": 50
  }
  ```
  `MaxResults` ist in Step 003 relevant, wird hier nur der Vollständigkeit
  halber angelegt. Falls der Coder es vorzieht, kann `MaxResults` auch
  erst in Step 003 ergänzt werden — das spart eine Zeile in diesem
  Commit, falls Step 003 fehlschlägt und zurückgerollt werden muss.
  *Empfehlung: bereits hier anlegen*, weil es das Verhalten für
  Tests stabil hält.
- **Warum:** Konfigurationspflicht, Default-Werte aus den
  Sub-Options-Klassen.

### Datei 5: `docs/04-Datenmodell-Validierung-Edgecases.md` (Abschnitt 1, search_docs-Query)

- **Was:** Den LIKE-Semantik-Block präziser fassen. Aktueller Text
  (Z. 48-49): „Kein Ranking: Ergebnisse werden alphabetisch nach `title`
  sortiert" bleibt erhalten. Zusätzlich *vor* diesem Absatz oder als
  eigener Aufzählungspunkt:
  ```
  - `query` wird via Bracket-Escape literal behandelt: `%`, `_`, `[`
    werden zu `[%]`, `[_]`, `[[]` umgeschrieben, *bevor* sie in das
    Pattern `%query%` interpoliert werden. Wildcard-Smuggling ist
    damit ausgeschlossen; `query="%"` matched nicht alle Zeilen,
    sondern sucht literal nach dem Prozent-Zeichen.
  - Maximale Query-Länge: `KnowHowToAi.Search.MaxQueryLength`
    (Default 200, konfigurierbar via `appsettings.json`). Längere
    Queries lösen `ArgumentException` aus, kein SQL-Round-Trip.
  ```
- **Warum:** Konzept-Vorgabe (Doku im selben Commit wie Code,
  `05-documentation.mdc`); exakte LIKE-Semantik war vorher nicht
  dokumentiert.

### Datei 6: `docs/03-Projektstruktur-und-Konfiguration.md` (Abschnitt 2)

- **Was:** Zwei Erweiterungen am JSON-Beispiel (Z. 56-72) und am Text
  danach:
  1. JSON-Beispiel um den `Search`-Sub-Block ergänzen (analog zur
     bestehenden `Logging`- und `Validation`-Sektion).
  2. Eine kurze Aufzählung (analog zur bestehenden
     `Logging`-/`Validation`-Beschreibung) anfügen:
     ```
     * **`Search`** (`KnowHowToAiSearchOptions`): `MaxQueryLength`
       (Default 200) — maximale Länge der `search_docs`-Query in
       Zeichen, längere Queries lösen `ArgumentException` aus.
       `MaxResults` (Default 50) — siehe [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md)
       und F-PE-002.
     ```
- **Warum:** Konsistent mit der Doku-Form für die anderen Sub-Options.
  Verweist für `MaxResults` auf Step 003 / docs/04, damit es hier
  nicht vorzeitig dokumentiert wird (ist in Step 003 nochmal Thema).

### Datei 7: `docs/02-Architektur-und-Techstack.md` (Abschnitt 4.D, search_docs-Block)

- **Was:** Im `search_docs`-Tool-Beschreibungs-Block (Z. 112-114) einen
  kurzen Halbsatz ergänzen — nur Verweis, keine ausführliche Erklärung
  hier (die ausführliche Doku steht in `docs/04`):
  ```
  * **Query-Semantik:** `LIKE '%query%'` mit Bracket-Escaping;
    Wildcard-Zeichen werden literal behandelt. Längen-Cap via
    `KnowHowToAi.Search.MaxQueryLength` (Default 200).
  ```
- **Warum:** Step 004 (F-MC-001) wird die Tool-Description selbst
  ausführlicher machen — hier in docs/02 nur der Vorgriff auf die
  Doku-Form, damit Code + Doku konsistent sind.

## Tests

- [ ] `BuildLikePatternTests.NormalString_ReturnsSubstringPattern`
      — `BuildLikePattern("routing")` → `"%routing%"`
- [ ] `BuildLikePatternTests.EscapesPercentSign` — `BuildLikePattern("50%")`
      → `"%50[%]%"`
- [ ] `BuildLikePatternTests.EscapesUnderscore` — `BuildLikePattern("a_b")`
      → `"%a[_]b%"`
- [ ] `BuildLikePatternTests.EscapesOpeningBracket` — `BuildLikePattern("[abc")`
      → `"%[[]abc%"` (Edge-Case: `[` muss *vor* den anderen escapet
      werden, sonst kollidieren die eingefügten `[` mit den anderen
      Escapes)
- [ ] `BuildLikePatternTests.PreservesBracketsEscapedByFirstStep` —
      `BuildLikePattern("[%]")` → `"%[[]%[]]%"` (verifiziert die
      Reihenfolge der `.Replace`-Aufrufe; wenn `[` *zuletzt* ersetzt
      würde, käme `%[[%]]%` raus, was das `[` im `[%]` maskieren würde)
- [ ] `BuildLikePatternTests.AllowsBracketedPatternStillValidAfterEscape`
      — Smoke-Test: `BuildLikePattern("normal")` (kein Sonderzeichen)
      liefert unverändert das erwartete Pattern (idempotentes
      Verhalten für den häufigen Fall)
- [ ] `BuildLikePatternTests.AllThreeWildcardsInOneInput_AllEscaped`
      — `BuildLikePattern("%a_b[c]")` → `"%[%]a[_]b[[]c]%"` (kombiniert
      alle drei Wildcards in einem Input)

**Test-Datei (neu):** `tests/KnowHowToAI.Core.Tests/Sync/BuildLikePatternTests.cs`
im Namespace `KnowHowToAI.Core.Tests.Sync`. Verwendet `[Theory]` +
`[InlineData]` für die Parametrisierung, analog zum bestehenden Stil
(siehe `SlugRulesTests.cs`).

**Hinweis zu `SearchDocsAsync`-Tests:** Die Längen-Validierung wirft
`ArgumentException` und könnte direkt getestet werden, aber
`SearchDocsAsync` braucht eine SQL-Verbindung (Backlog F-TS-001). Daher
*keine* direkten `SearchDocsAsync`-Tests in diesem Step — die
Validierungs-Logik wird in Step 003 beim Refactor des Rückgabe-Typs
mitgetestet, oder als isolierte Helper-Methode extrahiert (siehe
`Step 003` Notes).

**Bekannte Test-Baseline:** 55 → 61 (Step 001) → 68 (dieser Step) Tests
grün.

## Definition of Done

- [ ] `KnowHowToAiSearchOptions` existiert mit `MaxQueryLength = 200` und
      `MaxResults = 50` Default
- [ ] `KnowHowToAiOptions.Search` Property verweist auf neue Sub-Options
- [ ] `SqlDocumentsStore.SearchDocsAsync` validiert leeren Query (leere
      Liste) und zu langen Query (`ArgumentException`)
- [ ] `BuildLikePattern` ist `internal static`, mit Bracket-Escape
- [ ] `KnowHowToAI.Core.csproj` enthält
      `<InternalsVisibleTo Include="KnowHowToAI.Core.Tests" />`
- [ ] `appsettings.json` enthält `Search`-Sub-Block mit beiden Werten
- [ ] `BuildLikePatternTests` mit 7 Tests (siehe oben) vorhanden und
      grün
- [ ] `docs/04` Abschnitt 1, `docs/03` Abschnitt 2 und `docs/02` Abschnitt
      4.D sind aktualisiert (im selben Commit)
- [ ] `dotnet build -c Release` — 0 Warnings, 0 Errors
- [ ] `dotnet test` — 68 grün
- [ ] AiNetLinter 0 neue Verstöße
- [ ] Commit mit Subject
      `fix(security): like-wildcard-injection in search_docs schließen + längen-cap`,
      Body: „Verhindert, dass LLM-kontrollierte `query`-Eingaben
      SQL-LIKE-Wildcards einschmuggeln oder via Pattern-Länge einen
      trivialen DoS-Vektor gegen den SQL-Server auslösen. Bracket-Escape
      + konfigurierbare Längen-Obergrenze."
      Trailer: `Co-Authored--by: Claude Sonnet 5 <noreply@anthropic.com>`
- [ ] `step-002/step-result.md` geschrieben mit Commit-Hash
- [ ] `status` in `step-plan.md` von `in_progress` auf `done (pending audit)`
      gesetzt

## Rules-Refs

- `.agents/rules/01-code-style.mdc` — Early Returns in
  `SearchDocsAsync` (zwei `if`-Returns vor der SQL-Query, keine
  `else`-Kaskade); keine Kommentare
- `.agents/rules/02-testing.mdc` — Tests im selben Commit, xUnit v3,
  keine DB-Tests in v1 (gilt für die `SearchDocsAsync`-Edge-Cases
  ohne direkten Test)
- `.agents/rules/03-git-workflow.mdc` — Conventional Commit, deutsch
- `.agents/rules/05-documentation.mdc` — Doku im selben Commit wie
  Code (`docs/02`, `docs/03`, `docs/04`)
- `.agents/rules/06-configuration.mdc` — Schwellenwert
  `MaxQueryLength` in `appsettings.json`, kein Literal im Code
- `.agents/rules/AiNetLinter.mdc` — `MaxMethodLineCount: 60`,
  `MaxConstructorDependencies: 5` (für `SearchDocsAsync`-Signatur
  mit jetzt 3 Params bleibt unter dem Limit)

## Bekannte Ausnahmen

- **`SearchDocsAsync`-Tests (leerer Query, zu lange Query):** in diesem
  Step *nicht* direkt testbar, weil sie eine SQL-Verbindung bräuchten
  (Backlog F-TS-001). Die Validierungs-Logik bleibt aber trivial und
  ist visuell prüfbar; Step 003 wird im Refactor-Pfad ggf. eine
  indirekte Test-Möglichkeit schaffen (z. B. durch Extraktion der
  Validierung in eine separate `internal static`-Methode, die ohne
  SQL getestet werden kann). Der Auditer sollte das im Auge behalten —
  wenn Step 003 keine indirekte Test-Möglichkeit schafft, sollte
  der Coder spätestens dann eine `Build`-time-Validierungs-Logik
  in `SearchDocsAsync` extrahieren.

## Code-Skizze

```csharp
// In SqlDocumentsStore.cs (gekürzt)

public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(
    string query, int maxQueryLength, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(query)) return [];
    if (query.Length > maxQueryLength)
    {
        throw new ArgumentException(
            $"search_docs query ist {query.Length} Zeichen lang, max {maxQueryLength}.",
            nameof(query));
    }

    await using var connection = new SqlConnection(_connectionString);
    var rows = await connection.QueryAsync<DocumentSummary>(new CommandDefinition(
        $"""
        SELECT slug AS Slug, title AS Title FROM {_table}
        WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
        ORDER BY title;
        """,
        new { Pattern = BuildLikePattern(query) },
        cancellationToken: cancellationToken));

    return [.. rows];
}

internal static string BuildLikePattern(string query)
{
    var escaped = query
        .Replace("[", "[[]")
        .Replace("%", "[%]")
        .Replace("_", "[_]");
    return $"%{escaped}%";
}
```

## Notes

- **Reihenfolge im Loop:** Step 002 kommt nach Step 001 und vor Step 003,
  weil (a) Security > Performance (Tiebreak-Logik (a)) und (b) Step 003
  am gleichen Code-Pfad ansetzt (`SearchDocsAsync`-Rückgabetyp + SQL).
- **Step 003 baut hier auf:** Die `MaxResults`-Property wird in
  `KnowHowToAiSearchOptions` bereits angelegt, aber in `SearchDocsAsync`
  *noch nicht* verwendet — Step 003 ergänzt das `TOP (@MaxResults)` und
  leitet den Wert in `SearchDocsAsync` durch. Der Parameter
  `maxQueryLength` bleibt, `maxResults` kommt hinzu — Step 003 wird
  die Signatur entsprechend erweitern.
- **Tests für `SearchDocsAsync` ohne DB:** im Backlog (F-TS-001).
  *Alternative*, die der Coder prüfen kann: die `if`-Validierung in
  eine `internal static`-Methode `ValidateQueryOrThrow(string query,
  int maxQueryLength)` extrahieren — dann ist sie ohne SQL testbar.
  Nicht zwingend für diesen Step; wird in Step 003 ggf. mitgenommen.
- **`SearchResult` ist hier noch nicht im Spiel:** der
  `IReadOnlyList<DocumentSummary>`-Rückgabetyp bleibt in diesem Step
  unverändert. Step 003 bricht den API-Vertrag und führt `SearchResult`
  ein — separater Commit, separates Review.
- **Konzept-Konsistenz:** Der Konzept nennt als Test-Beispiel
  `BuildLikePattern_AllowsNormalSubstring`. Habe ich als
  `NormalString_ReturnsSubstringPattern` übernommen. Der Konzept-Test
  `BuildLikePattern_AllowsNormalSubstring` für `routing` → `%routing%`
  ist mein erster Test. Konzept-Tests `SearchDocsAsync_EmptyQuery_ReturnsEmpty`
  und `SearchDocsAsync_QueryTooLong_ThrowsArgumentException` sind wegen
  DB-Abhängigkeit *nicht* in diesem Step (siehe „Bekannte Ausnahmen").

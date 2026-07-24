# Dimension 8 — Performance / SQL-Effizienz

> **Vergleichsbasis:** SQL-Server-Performance-Grundlagen (Index-Nutzung, Query-Plan,
> Connection-Pooling), .NET-Async-Patterns (Avoid sync-over-async, JSON-Serialisierung),
> MCP-Response-Größen-Erwartungen (LLM-Token-Budget).
> **Methodik:** Statische Code-Analyse der Hot-Path-Methoden (`SqlDocumentsStore`,
> `DocsMcpTools`, `SchemaMigrator`, `ImportService.ReadDocuments`), Bewertung gegen
> SQL-Server-Index-Strategie und JSON-Serialisierungs-Allokationsprofile.
> **Nicht im Scope:** Echte Last-Tests (kein SQL-Server lokal, siehe `docs/03:84`),
> Memory-Profiling, GC-Tuning.

## Hot-Path-Inventar

| Methode | Aufrufer | Aufruffrequenz | Kritische Ressource |
| --- | --- | --- | --- |
| `SqlDocumentsStore.ListChildrenAsync` | MCP-Tool `list_children` | pro Tool-Aufruf | SQL-Query (1 SELECT) |
| `SqlDocumentsStore.SearchDocsAsync` | MCP-Tool `search_docs` | pro Tool-Aufruf | SQL-Query (1 SELECT mit `LIKE`) |
| `SqlDocumentsStore.GetDocAsync` | MCP-Tool `get_doc` | pro Tool-Aufruf | SQL-Query (1 SELECT), JSON-Serialisierung in `LogResponseSize` |
| `SqlDocumentsStore.GetAllAsync` | `export`-Command | einmal pro Export | SQL-Query (1 SELECT), JSON-Deserialize für Tags/Synonyms pro Zeile |
| `SqlDocumentsStore.ReplaceAllAsync` | `import`-Command | einmal pro Import | N+1 INSERTs in einer Transaktion |
| `SchemaMigrator.MigrateAsync` | `import`-Command | einmal pro Import | N Script-Executes |
| `ImportService.ReadDocuments` | `import`-Command | einmal pro Import | N File-Reads (sync) |
| `DocsMcpTools.LogResponseSize` | jeder Tool-Aufruf | pro Tool-Aufruf | JSON-Serialize der gesamten Response |

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-PE-001](#f-pe-001) | **High** | `LogResponseSize` serialisiert die *gesamte* Response zu JSON-Bytes nur um die Länge zu messen — pro Tool-Aufruf, inkl. `get_doc` mit potenziell MB-großem Content | `McpTools/DocsMcpTools.cs:43-44` |
| [F-PE-002](#f-pe-002) | **High** | `SearchDocsAsync` ohne `TOP`/`LIMIT` — bei großen Tabellen können tausende Treffer zurückkommen, plus LLM-Token-Budget-Sprengung | `Sync/SqlDocumentsStore.cs:79-92` |
| [F-PE-003](#f-pe-003) | Medium | `ListChildrenAsync` ohne `ORDER BY` — Treffer-Reihenfolge unspezifiziert, LLM-UX-Inkonsistenz | `Sync/SqlDocumentsStore.cs:65-77` |
| [F-PE-004](#f-pe-004) | Medium | `ReplaceAllAsync` ist N+1-Insert (Loop mit `ExecuteAsync` pro Dokument) — bei 10.000 Dokumenten sind das 10.000 separate Round-Trips zum SQL-Server | `Sync/SqlDocumentsStore.cs:32-50` |
| [F-PE-005](#f-pe-005) | Medium | `LIKE '%...%'` über `title`/`content`/`tags`/`synonyms` mit führendem Wildcard = Sequential Scan — bewusste Entscheidung (siehe `docs/00` Grundsatzentscheidung 4), aber: die Implikation für die Token-Budget-Berechnung des LLM ist undokumentiert | `Sync/SqlDocumentsStore.cs:79-92` |
| [F-PE-006](#f-pe-006) | Medium | `ImportService.ReadDocuments` nutzt `File.ReadAllText` (synchron) in einem `async`-Methode — blockiert Thread-Pool-Thread pro Datei | `Sync/ImportService.cs:30-37` |
| [F-PE-007](#f-pe-007) | Low | Kein expliziter `JsonSerializerOptions` Cache — pro `GetAllAsync`-Aufruf wird `JsonSerializer.Serialize` ohne gecachte Options aufgerufen, was Reflection pro Aufruf verursachen kann | `Sync/SqlDocumentsStore.cs:45-46, 111-112` |
| [F-PE-008](#f-pe-008) | Low | `SqlDocumentsStore` erstellt pro Methoden-Aufruf eine neue `SqlConnection` — Connection-Pooling durch `Microsoft.Data.SqlClient` mitigiert das, aber `new` + `OpenAsync` + `Dispose` ist nicht gratis | `Sync/SqlDocumentsStore.cs:25, 57, 67, 81, 98` |
| [F-PE-009](#f-pe-009) | Info | `MarkdownLinkRegex` ist `[GeneratedRegex]` — compiled und gecached seitens .NET, kein Hot-Path-Problem | `Validation/DocsValidator.cs:92-93` |

## Detail-Findings

### F-PE-001 — Doppelte JSON-Serialisierung in `LogResponseSize`

**Schweregrad:** High (jeden Tool-Aufruf betroffen, lineares Wachstum mit Response-Größe)

**Beobachtung:**
`src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:43-44`:
```csharp
private void LogResponseSize<T>(string toolName, T response) =>
    logger.LogInformation("{ToolName} response: {ByteCount} bytes", toolName, JsonSerializer.SerializeToUtf8Bytes(response).Length);
```

**Was passiert pro Tool-Aufruf (Beispiel `get_doc` mit 100 KB Content):**
1. MCP-SDK serialisiert `DocumentDetail` zu JSON für den Output → ~100 KB Allokation + Serialisierungs-Zeit
2. **Dann:** `LogResponseSize` serialisiert *dieselbe* `DocumentDetail` *nochmal* zu JSON-Bytes, um `.Length` zu lesen → nochmal ~100 KB Allokation + Serialisierungs-Zeit
3. Die `.Length` ist die einzige Information, die wir brauchen

**Skalierung:**
- 1 KB Content: vernachlässigbar (~0,1 ms)
- 100 KB Content: ~1-2 ms pro Tool-Aufruf (vermutlich mehr unter Last)
- 1 MB Content: ~15-30 ms pro Tool-Aufruf (wenn LLM ein großes Doc anfordert)
- 10 MB Content: ~150-300 ms pro Tool-Aufruf (Catastrophic)

**Wirkung auf MCP-Server:**
- MCP-Server ist Single-Threaded pro Client (stdio ist Single-Stream). Jeder Tool-Aufruf
  ist sequenziell. Doppelte Serialisierung verdoppelt die Latenz proportional zur
  Response-Größe.
- `LogResponseSize` läuft auch, wenn `MinimumLevel` über der Log-Stufe liegt (Serilog
  optimiert die Format-String-Erstellung, aber `JsonSerializer.SerializeToUtf8Bytes(response)`
  wird *vor* `LogInformation` aufgerufen — also *immer* ausgeführt, unabhängig vom
  Log-Level!)

**Detail-Datei:** [`_findings/F-PE-001-double-json-serialize.md`](_findings/F-PE-001-double-json-serialize.md)

**Fix-Empfehlung (zwei Varianten):**

**Variante A — Properties zählen statt serialisieren:**
```csharp
private static int MeasureResponseSize<T>(T response) => response switch
{
    IReadOnlyCollection<DocumentSummary> summaries => summaries.Count,
    DocumentDetail detail => detail.Content?.Length ?? 0,
    _ => 0,
};
```
Vorteil: O(1), keine Allokation, präziser (Items-Anzahl für Listen, Content-Länge für Doc).

**Variante B — Lazy-Lambda-Serialization (Serilog-Idiom):**
```csharp
private void LogResponseSize<T>(string toolName, T response) =>
    logger.LogInformation("{ToolName} response: {Size}",
        toolName,
        new Func<int>(() => JsonSerializer.SerializeToUtf8Bytes(response).Length));
```
Vorteil: Wenn der Log-Empfänger das Lambda nicht auswertet (z.B. weil MinimumLevel
das `Information`-Level filtert), wird die Serialisierung gar nicht ausgeführt.

**Empfehlung:** Variante A. Sie ist semantisch klarer (wir messen *was das LLM
interessiert*, nicht die interne Serialisierung) und eliminates die Allokation
komplett. Logger-Aufruf ändert sich zu:
```csharp
logger.LogInformation("{ToolName} response: {Size} chars", toolName, MeasureResponseSize(result));
```

**Aufwand:** ~10 Minuten + Test-Update (`LogResponseSize` ist `private`, also nur
Test-Update, falls ein Test existiert — existiert nicht, weil `DocsMcpTools`
nicht separat getestet wird).

---

### F-PE-002 — `SearchDocsAsync` ohne `TOP`/`LIMIT`

**Schweregrad:** High (LLM-Token-Budget-Sprengung)

**Beobachtung:**
`src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:79-92`:
```csharp
public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken)
{
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
```

**Probleme:**
1. **Kein `TOP`/`LIMIT`:** Wenn `query` sehr breit matched (z.B. "e", "a", "der"),
   können hunderte oder tausende `DocumentSummary`-Datensätze zurückkommen.
2. **Sortierung alphabetisch, nicht nach Treffer-Relevanz:** Bei `LIKE '%...%'` ist
   "Relevanz" nicht ohne Full-Text-Index berechenbar. Aber: alphabetische Sortierung
   ist *die schlechteste* für LLM-UX — die relevantesten Treffer sind oft die mit
   dem Query im Title, und die landen verstreut im Alphabet.
3. **Token-Budget-Sprengung:** Ein einzelner `DocumentSummary` ist klein (~50-100
   Token), aber 1000 Treffer × 100 Token = 100.000 Token für eine einzige
   `search_docs`-Antwort. Claude Sonnet hat 200k Kontext, aber 100k in einer
   einzelnen Antwort ist ein "ich kann nicht mehr"-Limit.

**`docs/04` Zeile 48** sagt:
> "Kein Ranking: Ergebnisse werden alphabetisch nach `title` sortiert, nicht nach
> Relevanz."

Das ist die *bewusste* Entscheidung. ABER: die fehlende `TOP`-Begrenzung ist nicht
bewusst — die ist schlicht ein Loch.

**Fix-Empfehlung:**
```sql
SELECT TOP (@MaxResults) slug AS Slug, title AS Title
FROM dbo.<DocumentsTableName>
WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
ORDER BY
    CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,  -- Title-Treffer zuerst
    title;
```
Mit `MaxResults` aus `KnowHowToAiOptions.Search` (neu), Default z.B. 50.

**Logik:**
- Title-Treffer vor Content-Treffer (heuristisches Ranking)
- Cap bei `MaxResults` (Default 50)
- Optional: Pagination via `OFFSET`/`FETCH NEXT` (komplexer, nicht für v1)

**Aufwand:** ~30 Minuten + neuer Options-Eintrag + Doku + Test.

---

### F-PE-003 — `ListChildrenAsync` ohne `ORDER BY`

**Schweregrad:** Medium (LLM-UX, Performance neutral)

**Beobachtung:**
`src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:65-77`:
```csharp
public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
{
    // ...
    var rows = await connection.QueryAsync<DocumentSummary>(new CommandDefinition(
        $"""
        SELECT slug AS Slug, title AS Title FROM {_table}
        WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug;
        """,
        new { ParentSlug = parentSlug },
        cancellationToken: cancellationToken));
    return [.. rows];
}
```

**Konsequenz:**
- Ohne `ORDER BY` ist die Treffer-Reihenfolge unspezifiziert. SQL-Server *kann* bei
  einem HEAP-Table (kein Clustered-Index auf der Zugriffs-Spalte) jede beliebige
  Reihenfolge liefern, auch zwischen Aufrufen.
- Für `list_children(parentSlug="it")` ist die *erwartete* UX: alphabetische oder
  hierarchische Sortierung. Aktuell: zufällig.
- LLM bekommt bei wiederholten Aufrufen potenziell unterschiedliche Reihenfolgen
  → Caching-Invalidierung im LLM-Kontext, Verwirrung.

**Fix-Empfehlung:**
```sql
SELECT slug AS Slug, title AS Title FROM dbo.<DocumentsTableName>
WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug
ORDER BY slug;  -- alphabetisch nach Slug = deterministisch
```

**Aufwand:** ~5 Minuten + Test (falls einer existiert — existiert nicht, weil
`SqlDocumentsStore` nicht getestet wird).

---

### F-PE-004 — `ReplaceAllAsync` N+1-Insert-Pattern

**Schweregrad:** Medium (Skaliert linear mit Dokument-Anzahl, nicht katastrophal)

**Beobachtung:**
`src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:32-50`:
```csharp
foreach (var document in documents.OrderBy(document => document.Slug.Count(c => c == '/')))
{
    await connection.ExecuteAsync(new CommandDefinition(
        $"""
        INSERT INTO {_table} (slug, parent_slug, title, content, tags, synonyms)
        VALUES (@Slug, @ParentSlug, @Title, @Content, @Tags, @Synonyms);
        """,
        // ...
        transaction, cancellationToken: cancellationToken));
}
```

**Probleme:**
- Pro Dokument ein Round-Trip zum SQL-Server (Network + Parse + Plan-Build pro
  Insert).
- Bei 10.000 Dokumenten: 10.000 separate Inserts in einer Transaktion. Jeder
  Commit implizit pro Transaktion, aber jeder Insert selbst ist ein Network-
  Hop.

**Mitigation:**
- `SqlBulkCopy` (Dapper's `BulkInsert` oder direkt) — ein einzelner Bulk-Copy
  kann 10.000 Zeilen in einem Hop einfügen.
- ABER: `SqlBulkCopy` ist nicht trivial — Column-Mapping, Identity-Columns,
  Constraints. Und in der bestehenden Transaktion einzubetten erfordert
  Sonderbehandlung.

**Realistische Einschätzung:**
- Bei den typischen Doku-Bibliotheken (Sage 100, HR, ein Kundenprojekt) vermutlich
  <1000 Dokumente. 1000 Inserts in einer Transaktion dauern auf lokalem SQL-Server
  ca. 0,5-1 Sekunde. Akzeptabel.
- Bei großen Bibliotheken (z.B. komplette Rewe-Produkt-Doku mit 50.000 Artikeln)
  wäre SqlBulkCopy empfehlenswert.

**Fix-Empfehlung:**
1. Benchmark dokumentieren: bei welcher Dokument-Anzahl wird N+1 spürbar?
2. Wenn >5.000 Dokumente regelmäßig: SqlBulkCopy mit `DestinationTableName` +
   manuelles Column-Mapping.
3. Wenn <1.000: Status Quo OK.

**Aufwand:** ~2 Stunden für SqlBulkCopy-Migration + 30 Min Tests. Aktuell:
kein dringender Handlungsbedarf, dokumentieren und in Backlog aufnehmen.

---

### F-PE-005 — `LIKE '%...%'` Index-Scan

**Schweregrad:** Medium (bewusste Entscheidung, aber Token-Budget-Implikation undokumentiert)

**Beobachtung:**
`LIKE '%...%'` mit führendem Wildcard verhindert Index-Nutzung. SQL-Server macht
einen vollen Tabellen-Scan + Pattern-Match pro Zeile.

**Auswirkung:**
- 100 Dokumente: < 10 ms, kein Problem
- 1.000 Dokumente: ~50-100 ms, merklich
- 10.000 Dokumente: ~500 ms-1 s, problematisch für interaktive LLM-UX
- 100.000 Dokumente: mehrere Sekunden, unbrauchbar

`docs/00-Overview.md` Grundsatzentscheidung 4 dokumentiert die Wahl *bewusst*
(kein Full-Text-Search-Voraussetzung). Aber:
- Die Token-Budget-Konsequenz (F-PE-002) ist NICHT dokumentiert
- Die Tabellen-Größen-Schwelle, ab der die Performance spürbar wird, ist NICHT
  dokumentiert
- Die Migrations-Option zu Full-Text (oder Trigram-Index) ist nur als Backlog-
  Item erwähnt (siehe `docs/05-Roadmap.md`)

**Fix-Empfehlung:** Kurzer Abschnitt in `docs/04` (oder `docs/02` Tech-Stack):
"Performance-Erwartung `LIKE '%...%'`: O(n) Scan pro Query. Bei <1.000 Dokumenten
kein Problem. Bei >10.000 empfiehlt sich `MAXTOP`-Begrenzung (siehe F-PE-002) oder
Migration auf SQL-Server-Full-Text."

**Aufwand:** ~5 Minuten Doku.

---

### F-PE-006 — `File.ReadAllText` in async-Methode

**Schweregrad:** Medium (Thread-Pool-Blockierung, kleinere Bibliotheken OK)

**Beobachtung:**
`src/KnowHowToAI.Core/Sync/ImportService.cs:30-37`:
```csharp
private IEnumerable<Document> ReadDocuments(string docsRootPath)
{
    foreach (var filePath in Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories))
    {
        var slug = SlugRules.FromFilePath(docsRootPath, filePath);
        yield return _parser.Parse(slug, File.ReadAllText(filePath));
    }
}
```

`File.ReadAllText` ist synchron. Die Methode ist `IEnumerable<Document>` (kein
async), also nicht direkt ein `async-over-sync`-Issue. ABER: `ImportService.ImportAsync`
ruft `ReadDocuments` auf, und der `yield return` blockiert den laufenden Thread
während jedes File-Reads.

**Auswirkung:**
- Bei 1.000 kleinen Dateien: jeder File-Read < 1 ms, kaum spürbar
- Bei 100 großen Dateien (mehrere MB): jeweils 10-50 ms synchroner Block pro Datei
- Thread-Pool-Thread ist für die Dauer blockiert (kein anderer Code kann laufen)

**Fix-Empfehlung:**
```csharp
private async Task<List<Document>> ReadDocumentsAsync(string docsRootPath, CancellationToken cancellationToken)
{
    var files = Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories).ToList();
    var documents = new List<Document>(files.Count);
    foreach (var filePath in files)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var slug = SlugRules.FromFilePath(docsRootPath, filePath);
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        documents.Add(_parser.Parse(slug, content));
    }
    return documents;
}
```

**Aufwand:** ~15 Minuten + Test-Update (CancellationToken-Pfad).

---

### F-PE-007 — `JsonSerializer` ohne gecachte Options (Low)

**Beobachtung:** `JsonSerializer.Serialize(document.Tags)` und
`JsonSerializer.Deserialize<List<string>>(row.Tags)!` werden ohne explizite
`JsonSerializerOptions` aufgerufen. Default-Options werden verwendet, was
Reflection pro Aufruf verursachen kann (in modernen .NET-Versionen ist das
aber gut optimiert via Source-Generator-Pfad).

**Empfehlung:** Wenn Performance-kritisch: `JsonSerializerContext` als
Source-Generator. Für v1: kein Handlungsbedarf.

**Aufwand:** ~1 Stunde für Source-Generator-Migration.

---

### F-PE-008 — Pro Methoden-Aufruf neue `SqlConnection` (Low)

**Beobachtung:** `await using var connection = new SqlConnection(_connectionString)`
in jeder Methode. Connection-Pooling durch `Microsoft.Data.SqlClient` (transparent
bei gleichem Connection-String) bedeutet: die `SqlConnection`-Instanzen sind
cheap Pool-Handles. `OpenAsync` ist auch Pool-Optimiert.

**Realistische Einschätzung:** Kein Performance-Problem in der Praxis. Die
typische MCP-Workload hat < 100 Tool-Aufrufe pro Minute; Connection-Pool-
Aufbau passiert einmal pro Prozess, danach sind Pool-Checks billig.

**Kein Handlungsbedarf.**

---

### F-PE-009 — `GeneratedRegex` (Info)

`[GeneratedRegex]` ist der idiomatische .NET-7+-Weg für kompilierte Regex-Patterns.
`Microsoft.Extensions.Logging` und .NET-Runtime optimieren das Hot-Path-Loading.
Kein Handlungsbedarf.

---

## Performance-Token-Budget-Schätzung

Eine `DocumentSummary` ist ~50-100 Tokens. Eine `DocumentDetail` ist
`50 + Content.Length / 4` Tokens (grobe Schätzung, deutsche Markdown-Dichte).

**Token-Budget-Szenarien:**

| Szenario | Treffer | Tokens | LLM-Budget-Impact |
| --- | --- | --- | --- |
| `list_children` 5 Items | 5 | 250-500 | OK |
| `list_children` 50 Items (großes Verzeichnis) | 50 | 2.500-5.000 | OK, aber viel für eine Antwort |
| `search_docs` breit (z.B. "a") | 1.000 (ohne Cap) | 50.000-100.000 | **F-PE-002 katastrophal** |
| `search_docs` spezifisch (z.B. "personalnummer-mueller") | 3 | 150-300 | OK |
| `get_doc` 5 KB Content | 1 | ~1.300 | OK |
| `get_doc` 50 KB Content | 1 | ~12.500 | merklich, OK |
| `get_doc` 500 KB Content (Backlog-Thema) | 1 | ~125.000 | problematisch |

**Empfehlung:** Mit F-PE-002 (`TOP`-Cap, Default 50) wird das Worst-Case-Szenario
abgefangen. Andere Verbesserungen sind nice-to-have.

---

## Zusammenfassung Dim 8

- **9 Findings**, davon 2 × High, 5 × Medium, 1 × Low, 1 × Info.
- **Hot-Path-Problem:** F-PE-001 (Doppel-JSON) und F-PE-002 (kein `TOP`-Cap) sind
  die zwei wichtigsten Quick Wins. Beide klein im Aufwand, groß in der Wirkung.
- **Bewusste Entscheidungen:** F-PE-004 (N+1-Insert) und F-PE-005 (LIKE-Index-Scan)
  sind *nicht* Quick Wins — beide würden Architektur-Entscheidungen berühren, die
  bewusst zugunsten von Einfachheit getroffen wurden. Dokumentieren, nicht refaktorisieren.
- **Skalierungs-Profil:** Das Tool ist für < 5.000 Dokumente pro Bibliothek
  ausgelegt. Darüber wird es langsam, dann sollte Full-Text oder SqlBulkCopy
  evaluiert werden.

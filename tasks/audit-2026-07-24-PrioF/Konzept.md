# Audit Prio F — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** PrioA (umgesetzt), PrioB/C/D/E (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (56 Findings nach Prio A-E) wurden die 4 Findings extrahiert, die unter „Performance-Polish (Rest Dim 8)" zusammengefasst sind. Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | Status |
|---|---|---|---|---|
| [F-PE-003](#f-pe-003--listchildrenasync-ohne-order-by) | `ListChildrenAsync` ohne `ORDER BY` | Medium | ~5 Min | **erledigt** (Commit d8f22a5) |
| [F-PE-004](#f-pe-004--replaceallasync-n1-insert-pattern) | `ReplaceAllAsync` N+1-Insert-Pattern | Medium (Backlog) | ~5 Min Doku | offen |
| [F-PE-005](#f-pe-005--like--index-scan) | `LIKE '%...%'` Index-Scan | Medium | ~5 Min Doku | offen |
| [F-PE-006](#f-pe-006--filereadalltext-in-async-methode) | `File.ReadAllText` in async-Methode | Medium | ~15 Min + Tests | **erledigt** |

**Gesamt-Aufwand:** ~30 Min (5 Min SQL-Fix + 15 Min Code + 10 Min Doku). Aufteilbar in 2-3 Commits.

**Leitidee:** Kleinere Performance-Polish-Fixes, die die User-Experience verbessern, ohne Architektur zu verändern. Plus zwei Doku-Hinweise für bewusste Performance-Entscheidungen.

---

## F-PE-003 — `ListChildrenAsync` ohne `ORDER BY`

> **Schweregrad:** Medium · **Dimension:** Performance / SQL
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:65-77`

### Problem

```sql
SELECT slug AS Slug, title AS Title FROM dbo.<DocumentsTableName>
WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug;
```

Ohne `ORDER BY` ist die Treffer-Reihenfolge unspezifiziert. SQL-Server *kann* bei einem HEAP-Table (kein Clustered-Index auf der Zugriffs-Spalte) jede beliebige Reihenfolge liefern, auch zwischen Aufrufen.

**Konsequenz:** LLM bekommt bei wiederholten Aufrufen potenziell unterschiedliche Reihenfolgen → Caching-Invalidierung im LLM-Kontext, Verwirrung.

### Fix-Empfehlung

```sql
SELECT slug AS Slug, title AS Title FROM dbo.<DocumentsTableName>
WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug
ORDER BY slug;  -- alphabetisch nach Slug = deterministisch
```

### Aufwand

- ~5 Min
- 1 Commit

### Risiko

Keine. `ORDER BY slug` ist deterministisch und konsistent mit der Slug-Convention (`a-z0-9-`).

---

## F-PE-004 — `ReplaceAllAsync` N+1-Insert-Pattern

> **Schweregrad:** Medium · **Dimension:** Performance (Backlog)
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:32-50` + `docs/05-Roadmap.md` (Backlog-Verweis)

### Problem

Pro Dokument ein Round-Trip zum SQL-Server. Bei 10.000 Dokumenten: 10.000 separate Inserts in einer Transaktion. Jeder Commit implizit pro Transaktion, aber jeder Insert selbst ist ein Network-Hop.

**Realistische Einschätzung:**
- Bei den typischen Doku-Bibliotheken (Sage 100, HR, ein Kundenprojekt) vermutlich <1000 Dokumente. 1000 Inserts in einer Transaktion dauern auf lokalem SQL-Server ca. 0,5-1 Sekunde. Akzeptabel.
- Bei großen Bibliotheken (z.B. komplette Rewe-Produkt-Doku mit 50.000 Artikeln) wäre SqlBulkCopy empfehlenswert.

### Fix-Empfehlung (in PrioF: nur Doku)

**Backlog-Doku in `docs/05-Roadmap.md`** ergänzen:
> "**Bulk-Import mit `SqlBulkCopy` (Backlog):** Bei >5.000 Dokumenten regelmäßig: SqlBulkCopy mit `DestinationTableName` + manuelles Column-Mapping. Bei <1.000: Status Quo OK. Aktuelle Performance-Charakteristik: ~0,5-1 Sekunde pro 1.000 Inserts auf lokalem SQL-Server."

**Code-Fix (NICHT in PrioF):** SqlBulkCopy-Migration, ~2h + 30 Min Tests. Bei erstem Bedarf (Rewe-Doku) angehen.

### Aufwand

- ~5 Min Doku
- 1 Doku-Commit

### Risiko

Keine. Reine Doku.

---

## F-PE-005 — `LIKE '%...%'` Index-Scan

> **Schweregrad:** Medium · **Dimension:** Performance / Doku
> **Datei:** `docs/04-Datenmodell-Validierung-Edgecases.md` (neu) oder `docs/02` (Tech-Stack)

### Problem

`LIKE '%...%'` mit führendem Wildcard verhindert Index-Nutzung. SQL-Server macht einen vollen Tabellen-Scan + Pattern-Match pro Zeile.

**Auswirkung:**
- 100 Dokumente: < 10 ms, kein Problem
- 1.000 Dokumente: ~50-100 ms, merklich
- 10.000 Dokumente: ~500 ms-1 s, problematisch für interaktive LLM-UX
- 100.000 Dokumente: mehrere Sekunden, unbrauchbar

`docs/00-Overview.md` Grundsatzentscheidung 4 dokumentiert die Wahl *bewusst* (kein Full-Text-Search-Voraussetzung). Aber: die Token-Budget-Konsequenz (F-PE-002) ist NICHT dokumentiert, die Tabellen-Größen-Schwelle ist NICHT dokumentiert.

### Fix-Empfehlung

Kurzer Abschnitt in `docs/04` (oder `docs/02` Tech-Stack):
> "Performance-Erwartung `LIKE '%...%'`: O(n) Scan pro Query. Bei <1.000 Dokumenten kein Problem. Bei >10.000 empfiehlt sich `MAXTOP`-Begrenzung (siehe F-PE-002) oder Migration auf SQL-Server-Full-Text."

### Aufwand

- ~5 Min Doku
- 1 Doku-Commit (kann mit F-PE-004 kombiniert werden)

### Risiko

Keine. Reine Doku.

---

## F-PE-006 — `File.ReadAllText` in async-Methode

> **Schweregrad:** Medium · **Dimension:** Performance
> **Datei:** `src/KnowHowToAI.Core/Sync/ImportService.cs:30-37` + `ImportServiceTests.cs` (Tests)

### Problem

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

`File.ReadAllText` ist synchron. `ImportService.ImportAsync` ruft `ReadDocuments` auf, der `yield return` blockiert den laufenden Thread während jedes File-Reads.

**Auswirkung:**
- Bei 1.000 kleinen Dateien: jeder File-Read < 1 ms, kaum spürbar
- Bei 100 großen Dateien (mehrere MB): jeweils 10-50 ms synchroner Block pro Datei
- Thread-Pool-Thread ist für die Dauer blockiert (kein anderer Code kann laufen)

### Fix-Empfehlung

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

Aufrufer in `ImportAsync` anpassen: `var documents = await ReadDocumentsAsync(options.DocsRootPath, cancellationToken);`

### Aufwand

- ~15 Min Code + Test-Update (CancellationToken-Pfad)
- 1 Commit

### Risiko

Niedrig. Additiv-Verbesserung. Cancellation funktioniert besser (kein Thread-Block mehr).

---

## Warum diese 4 und nicht andere?

### Aufgenommen

1. **F-PE-003** — billiger SQL-Fix, deterministische Reihenfolge für LLM
2. **F-PE-004** — Doku-Backlog, hält SqlBulkCopy-Migration fest
3. **F-PE-005** — Doku-Hinweis für bewusste Performance-Wahl
4. **F-PE-006** — Thread-Pool-Blockierung, billig zu fixen

### Bewusst weggelassen (Kurzbegründung)

- **F-PE-007 (JsonSerializer ohne Options Cache):** Source-Generator-Migration ist 1h, größerer Brocken. Per Audit "für v1: kein Handlungsbedarf".
- **F-PE-008 (Pro Methoden-Aufruf neue SqlConnection):** Per Audit "kein Performance-Problem in der Praxis" (Connection-Pooling mitigiert).
- **F-PE-009 (GeneratedRegex Info):** Positive Bestätigung, kein Handlungsbedarf.

Alle übrigen Findings (52) gehören thematisch in andere Brocken (G: Config-Deploy, H: Code-Quality-Rest, I: Doku-Rest, J: Architektur-Rest, K: Dependencies-Rest, L: Sicherheits-Rest, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-PE-003** (~5 Min) — SQL-Fix
2. **F-PE-006** (~15 Min + Tests) — Code-Fix
3. **F-PE-004** + **F-PE-005** (~10 Min Doku) — kann in einem Doku-Commit kombiniert werden

**Gesamt-Aufwand in dieser Reihenfolge:** ~30 Min, 2 Commits.

**Commit-Clustering-Vorschlag:**
- Commit 1: F-PE-003 + F-PE-006 (Performance-Code-Fixes)
- Commit 2: F-PE-004 + F-PE-005 (Performance-Doku-Notizen)

## Querverweise zu anderen Brocken

- **F-PE-002 in PrioA** — `SearchDocsAsync` `TOP`-Cap; F-PE-005 verweist auf F-PE-002 als Token-Budget-Mitigation.
- **F-TS-001 in PrioE** — `SqlDocumentsStore`-Tests; F-PE-004 (SqlBulkCopy) wäre ein Kandidat für SQLite-Tests.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).

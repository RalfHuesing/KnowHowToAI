# Audit Prio C — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** `tasks/audit-2026-07-24-PrioA/Konzept.md` (umgesetzt), `tasks/audit-2026-07-24-PrioB/Konzept.md` (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (77 Findings nach Prio A + Prio B) wurden die 6 Findings extrahiert, die unter „Architecture & Dependencies" zusammengefasst sind. Alle übrigen Findings (71) wurden bewusst weggelassen — Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | Dimension | Status |
|---|---|---|---|---|---|
| [F-AR-001](#f-ar-001--di-inkonsistenz-zwischen-cli-commands) | DI-Inkonsistenz zwischen CLI-Commands | High | ~30 Min | Architektur | **erledigt** (Commit 934978b) |
| [F-AR-004](#f-ar-004--sqldocumentsstore-thread-safety-undokumentiert) | `SqlDocumentsStore` Thread-Safety undokumentiert | Medium | ~5-20 Min | Architektur | **erledigt** (Variante A: Code-Kommentar an ReplaceAllAsync) |
| [F-AR-007](#f-ar-007--service-lifetimes-undokumentiert) | Service-Lifetimes undokumentiert | Low | ~5 Min | Architektur | **erledigt** (in F-DK-003 docs/03) |
| [F-DP-001](#f-dp-001--zwei-preview-dependencies-stable-verfügbar) | Zwei Preview-Dependencies, Stable verfügbar | High | ~10 Min | Dependencies | **erledigt** (Commit 8fed418) |
| [F-DP-002](#f-dp-002--microsoftdatasqlclient-702-breaking-changes-undokumentiert) | `Microsoft.Data.SqlClient 7.0.2` Breaking Changes (undokumentiert) | Medium | ~5 Min | Dependencies | **erledigt** |
| [F-DP-003](#f-dp-003--keine-dotnet-list-package---vulnerable-policy) | Keine `dotnet list package --vulnerable` Policy | Medium | ~15 Min | Dependencies | **erledigt** |

**Gesamt-Aufwand:** ~1 Stunde (35 Min Code + 30 Min Doku + 5 Min Build-Konfig). Aufteilbar in 3-5 Commits.

**Leitidee:** Architektur-Inkonsistenzen aufräumen, die in Prio A (F-AR-002) sichtbar geworden sind, plus Dependency-Hygiene etablieren (Preview-Deps auflösen, Breaking Changes dokumentieren, NuGet-Vulnerability-Check).

---

## F-AR-001 — DI-Inkonsistenz zwischen CLI-Commands

> **Schweregrad:** High · **Dimension:** Architektur
> **Datei:** `src/KnowHowToAI.Cli/Program.cs:64, 84-86, 105-107`

### Problem

Innerhalb *einer* Datei (`Program.cs`) werden Services je nach Command-Pfad auf zwei völlig unterschiedliche Weisen konstruiert:

| Command | Service-Konstruktion |
| --- | --- |
| `RunValidate` | `new DocsValidator(options.Validation.MaxContentLengthWarning)` |
| `RunImport` | `new SqlDocumentsStore(...)` + `new ImportService(store.ReplaceAllAsync, ...)` |
| `RunExport` | `new SqlDocumentsStore(...)` + `new ExportService(store.GetAllAsync)` |
| `RunServer` | `Host.CreateApplicationBuilder()` + `AddSingleton<SqlDocumentsStore>` + `AddMcpServer(...).WithToolsFromAssembly()` |

**Konsequenzen:**
1. **Doppelte ConnectionString-Validierung:** `SqlDocumentsStore`-Konstruktor ruft `SqlIdentifierValidator.EnsureValid` auf. In `RunImport`/`RunExport` einmal pro Command-Lauf. In `RunServer` einmal beim App-Build. Inkonsistent.
2. **Inkonsistente Test-Coverage:** `ImportService` ist getestet, `SqlDocumentsStore` nicht. In `RunImport` werden beide ohne Schutzwall verkettet.
3. **Doppelter ConnectionString-Pool:** OK durch `Microsoft.Data.SqlClient`-Pooling. Aber: schwer zu refaktorisieren bei zukünftigen Pool-Konfigurationen.
4. **Schwerer zu refaktorisieren:** Ein zukünftiger Decorator (z.B. `CachingDocumentsStore`) müsste an *drei* Stellen in `Program.cs` plus im DI-Setup eingeführt werden.

### Fix-Empfehlung

Composition-Root-Pattern: Eine zentrale Factory-Funktion `BuildCoreServices(KnowHowToAiOptions options)`, die `DocsValidator`, `SqlDocumentsStore`, `ImportService`, `ExportService` konstruiert und als Tupel zurückgibt.

```csharp
static (DocsValidator validator, SqlDocumentsStore store, ImportService import, ExportService export)
    BuildCoreServices(KnowHowToAiOptions options)
{
    var store = new SqlDocumentsStore(options.ConnectionString, options.DocumentsTableName);
    var validator = new DocsValidator(options.Validation.MaxContentLengthWarning);
    var import = new ImportService(store.ReplaceAllAsync, options.Validation.MaxContentLengthWarning);
    var export = new ExportService(store.GetAllAsync);
    return (validator, store, import, export);
}

// RunImport:
var (_, store, import, _) = BuildCoreServices(options);
await SchemaMigrator.MigrateAsync(...);
var result = await import.ImportAsync(options.DocsRootPath, cancellationToken);

// RunServer:
builder.Services.AddSingleton(sp => BuildCoreServices(options).store);
```

**Synergie mit F-AR-002 (Prio A):** Wenn Prio A umgesetzt ist (Core-Services nehmen `ILogger<T>` entgegen), muss `BuildCoreServices` die Logger mitgeben. Empfehlung: Prio C *nach* Prio A umsetzen, damit die Logger-Injection in einem Aufwasch passiert.

### Aufwand

- ~30 Min + Test-Run
- 1 Commit (idealerweise nach Prio A)

### Risiko

Niedrig. Funktional keine Änderung. Tests bleiben unverändert (Services werden weiterhin per `new` in Tests konstruiert).

---

## F-AR-004 — `SqlDocumentsStore` Thread-Safety undokumentiert

> **Schweregrad:** Medium · **Dimension:** Architektur
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs` (alle Methoden)

### Problem

`SqlDocumentsStore` ist `sealed`, hat nur private Felder (Connection-String + Table), alle Methoden erstellen ihre eigene `SqlConnection`. Read-Methoden (`ListChildrenAsync`, `SearchDocsAsync`, `GetDocAsync`, `GetAllAsync`) sind thread-safe (Connection wird pro Aufruf geöffnet, kein Shared State).

`ReplaceAllAsync` ist **nicht** thread-safe — zwei parallele Imports würden race condition auf `DELETE FROM` und `INSERT`s auslösen. Aktuell nicht möglich (CLI ruft `import` nicht parallel auf, Server ruft `ReplaceAllAsync` nicht auf), aber: ein zukünftiger Refactor, der Import via MCP-Tool exposed (Backlog-Item in `docs/05`: "Schreib-Tools via MCP"), würde das brechen.

`docs/04` Edge-Case 4.3 erwähnt das implizit:
> "Ein parallel laufender MCP-Server sieht dadurch nie einen halb-geleerten Zustand"

Aber: kein expliziter Hinweis "ReplaceAllAsync darf nicht parallel aufgerufen werden" im Code selbst.

### Fix-Empfehlung

**Variante A — Nur Code-Kommentar (~5 Min):**
```csharp
// Not thread-safe. Do not call concurrently with other instances or with
// read methods that need snapshot consistency.
public async Task ReplaceAllAsync(...)
```

**Variante B — `SemaphoreSlim` als Schutzwall (~20 Min):**
```csharp
private readonly SemaphoreSlim _replaceAllLock = new(1, 1);

public async Task ReplaceAllAsync(...)
{
    await _replaceAllLock.WaitAsync(cancellationToken);
    try
    {
        // existing implementation
    }
    finally
    {
        _replaceAllLock.Release();
    }
}
```

**Empfehlung:** Variante A jetzt (billig), Variante B wenn Schreib-Tools via MCP (Backlog) angegangen werden. Doku-Hinweis in `docs/04` ergänzen: "`import` ist Single-Process; zwei parallele `import`-Aufrufe sind nicht erlaubt."

### Aufwand

- Variante A: ~5 Min Code + 5 Min Doku
- Variante B: ~20 Min Code + Tests

### Risiko

Niedrig. Variante A ist nur Doku. Variante B serialisiert `ReplaceAllAsync` pro Instance — solange nur ein Import läuft, kein Overhead.

---

## F-AR-007 — Service-Lifetimes undokumentiert

> **Schweregrad:** Low · **Dimension:** Architektur + Doku
> **Datei:** `docs/03-Projektstruktur-und-Konfiguration.md`

### Problem

- `SqlDocumentsStore` ist Singleton (`RunServer`-Pfad, `AddSingleton`).
- Andere Services sind implizit Transient (per `new` in `RunImport`/`RunExport`).
- Doku zeigt *wie* der Server verdrahtet wird, aber nicht *warum* diese Lifetime gewählt wurde.

### Fix-Empfehlung

Ein-Satz-Erklärung in `docs/03` zu Singleton-Lifetime:
> "`SqlDocumentsStore` ist zustandslos auf Instance-Ebene und teilt sich den DB-Connection-Pool, daher Singleton."

### Aufwand

- ~5 Min
- 1 Doku-Commit (kann mit F-AR-001 Doku-Anteilen kombiniert werden)

### Risiko

Keine. Reine Doku.

---

## F-DP-001 — Zwei Preview-Dependencies, Stable verfügbar

> **Schweregrad:** High · **Dimension:** Dependencies
> **Datei:** `src/KnowHowToAI.Cli/KnowHowToAI.Cli.csproj:16, 19`

### Problem

```xml
<PackageReference Include="ModelContextProtocol" Version="2.0.0-preview.2" />
<PackageReference Include="System.CommandLine" Version="3.0.0-preview.5.26302.115" />
```

**Externe Recherche (2026-07-24):**

- **`ModelContextProtocol`:** Aktuelle Stable `1.4.1` (2026-06-04). Preview `2.0.0-preview.2` richtet sich auf 2026-07-28-Spec. SDK-Maintainer empfehlen Stable für kritische Workloads. 2.0-Breaking-Changes: Deprecation von `roots`, `sampling`, `logging` Capabilities; `EnableLegacySse` als obsolet markiert; HTTP-Transport-Default auf Stateless-Mode.
- **`System.CommandLine`:** Aktuelle Stable `2.0.10` (2026-06-09). 3.0 ist seit über einem Jahr in Preview-Phase (Issue #2500: "Roadmap for release?"). 2.0.x ist die breit genutzte Stable (~231k Downloads für 2.0.9).

**Warum die Preview-Wahl?**
- `System.CommandLine 3.0` bringt voraussichtlich den "Powderhouse"-Source-Generator. Aber: nicht GA, nicht kritisch für v1.
- `ModelContextProtocol 2.0`: bringt die neue 2026-07-28-Spec (4 Tage nach Audit-Datum final). Vor 28.7. ist 2.0-preview vermutbar OK. Nach 28.7. ist Stable 2.0 verfügbar.

### Fix-Empfehlung

1. **`System.CommandLine` auf `2.0.10` downgraden** — wenn keine 3.0-spezifischen Features benötigt werden (sehr wahrscheinlich). Build + Tests sollten mit 2.0.10 grün sein.
2. **`ModelContextProtocol` auf `1.4.1` (Stable) lassen**, bis 2.0.0 Stable verfügbar ist (Ende Juli / Anfang August 2026). Bei 2.0.0-Stable-Release: gezielter Major-Version-Bump mit Changelog-Review.

### Aufwand

- ~10 Min für beide Downgrades + Test-Run
- 1 Commit (oder 2 Commits, einer pro Paket)

### Risiko

Niedrig. Beide Downgrades sind in den jeweils aktuellen 1.x/2.x Stable-Versionen. `Microsoft.Data.SqlClient 7.0.2` ist bereits stabil.

---

## F-DP-002 — `Microsoft.Data.SqlClient 7.0.2` Breaking Changes (undokumentiert)

> **Schweregrad:** Medium · **Dimension:** Dependencies + Doku
> **Datei:** `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj:11` + `docs/03`

### Problem

**Breaking Changes in 7.0:**

1. **`Azure.Identity` ist NICHT MEHR transitive Dependency** des Core-Pakets. Wer Entra-ID-Auth braucht, muss explizit `Microsoft.Data.SqlClient.Extensions.Azure` referenzieren. **Für dieses Projekt irrelevant** (SQL-Login, kein Entra-ID).
2. **`SqlBulkCopy` bricht auf SQL Server 2016**: dynamische Spalten-Metadata-Query (graph_type-Column gibt es erst ab SQL 2017). Issue #3714. Behoben in 7.0.1. 7.0.0 wirft `Invalid column name 'graph_type'`.
3. **`SqlVector` ist jetzt `readonly struct` (vorher `class`)**: Breaking Change für Code, der `SqlVector<float>` als `class` verwendet. **Irrelevant** (kein Vektor-Datentyp).
4. **`Packet Multiplexing` default disabled**: Performance-relevant, Default ist konservativ.
5. **`CER` (Constrained Execution Region) Cleanup geändert**: Connection-Pool-Cleanup leicht anders. Edge-Case.
6. **`ActiveDirectoryPassword` als Obsolete markiert**. **Irrelevant**.

**Konsequenz für KnowHowToAI:**
- Aktueller Use-Case (lokales SQL-Server, SQL-Login `Agent`): **keinerlei Impact**.
- Auf Azure-SQL mit Entra-ID: F-DP-004 relevant.
- Auf SQL Server 2016: `SqlBulkCopy` würde scheitern. Aber: aktuell wird kein `SqlBulkCopy` im Code verwendet (alle Inserts via Dapper `ExecuteAsync`).

### Fix-Empfehlung

Kurzer Doku-Abschnitt in `docs/03` Abschnitt 2:
> "Microsoft.Data.SqlClient 7.0+ ist gepinnt; siehe Release-Notes für Breaking Changes. Bei SQL-Server-Versionen ≤ 2016 ist 6.x zu verwenden (kein automatisches Downgrade). Pinning auf 7.0.2 (nicht 7.0.0) ist korrekt, weil 7.0.1 den SqlBulkCopy-Fix bringt."

### Aufwand

- ~5 Min Doku
- 1 Doku-Commit (kann mit F-DP-001 kombiniert werden)

### Risiko

Keine. Reine Doku.

---

## F-DP-003 — Keine `dotnet list package --vulnerable` Policy

> **Schweregrad:** Medium · **Dimension:** Dependencies + CI-Hygiene
> **Datei:** beide `*.csproj` + `scripts/publish.ps1` + ggf. CI-Workflow

### Problem

Weder in `publish.ps1` noch in `.github/workflows/release.yml` ist ein Audit-Lauf für vulnerable Packages vorgesehen. NuGet hat seit .NET 8 einen `<AuditMode>` in der csproj, der beim `dotnet restore` automatisch prüft.

### Fix-Empfehlung

1. **In beiden csproj-Dateien:**
   ```xml
   <NuGetAuditMode>direct</NuGetAuditMode>
   <NuGetAuditLevel>high</NuGetAuditLevel>
   ```
2. **Im `publish.ps1` als Smoke-Check:**
   ```powershell
   dotnet list package --vulnerable --include-transitive
   if ($LASTEXITCODE -ne 0) { throw "Vulnerable packages gefunden — Publish abgebrochen." }
   ```
3. **Optional (CI):** `dotnet restore --audit` im Release-Workflow.

### Aufwand

- ~15 Min (5 Min csproj, 5 Min publish.ps1, 5 Min Test)
- 1 Commit

### Risiko

Niedrig. `<NuGetAuditLevel>high` ist konservativ (lässt moderate/low Vulnerabilities durch). Bei einem Fund muss man manuell entscheiden.

---

## Warum diese 6 und nicht andere?

### Aufgenommen

**Architektur (3):**
1. **F-AR-001** — logischer Nachfolger zu F-AR-002 (Prio A); wenn Prio A umgesetzt ist, kommt F-AR-001 direkt danach
2. **F-AR-004** — echte Lücke, die real wird, sobald Schreib-Tools via MCP kommen (Backlog)
3. **F-AR-007** — analog zu F-DK-003 in Prio B, gleicher Charakter (interne Doku-Lücke, billig zu beheben)

**Dependencies (3):**
4. **F-DP-001** — High, klares Risiko, schneller Downgrade-Fix
5. **F-DP-002** — Doku-Hinweis zu einem Breaking-Change, der "irrelevant" nur ist, solange man nichts ändert
6. **F-DP-003** — CI-Hygiene, klein und vorbeugend

### Bewusst weggelassen (Kurzbegründung)

- **F-AR-003 (LogResponseSize in falscher Schicht):** Per Audit obsolet nach F-PE-001 ✅. Wird in diesem Commit komplett aus dem Original-Audit entfernt.
- **F-AR-005 (Keine zentrale Constants-Datei):** Regel sagt "erst bei 2. Fall handeln", aktuell nur 1 Fall (`FrontMatterParser.delimiter`). Beobachten.
- **F-AR-006 (Logging.Abstractions nicht in Core):** Folge von F-AR-002 (Prio A), automatisch erledigt.
- **F-DP-004 (Azure-Extensions-Package nicht referenziert):** Per Audit "irrelevant für Use-Case".
- **F-DP-005 (Fixed Versions, keine automatischen Patches):** Bewusste Entscheidung (Reproduzierbarkeit), Prozess-Thema.
- **F-DP-006 (ModelContextProtocol.Core transitiv):** Sauberkeit, niedriger Impact, kann jederzeit gefixt werden.

Alle übrigen Findings (71) gehören thematisch in andere Brocken (D: Test-Coverage, E: Performance-Polish, F: Config-Deploy, G: Code-Quality-Rest, plus die 5 Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-AR-001** (~30 Min) — *nach* F-AR-002 (Prio A), weil die Factory-Funktion die Logger-Parameter durchreichen muss
2. **F-AR-004** (Variante A, ~10 Min) — billig, sofort
3. **F-AR-007** (~5 Min) — Doku, kann mit F-AR-001 Doku-Anteilen kombiniert werden
4. **F-DP-002** (~5 Min) — Doku, kann mit F-DP-001 kombiniert werden
5. **F-DP-001** (~10 Min) — Code-Änderung in csproj, separater Commit
6. **F-DP-003** (~15 Min) — csproj + publish.ps1, eigener Commit

**Gesamt-Aufwand in dieser Reihenfolge:** ~1 Stunde, 4-6 Commits.

**Commit-Clustering-Vorschlag:**
- Commit 1: F-AR-001 (Composition-Root-Factory)
- Commit 2: F-AR-004 + F-AR-007 (Thread-Safety-Doku + Service-Lifetime-Doku)
- Commit 3: F-DP-001 (Dependency-Downgrades)
- Commit 4: F-DP-002 (SqlClient-Breaking-Changes-Doku)
- Commit 5: F-DP-003 (NuGet-Audit-Mode + publish.ps1-Check)

## Querverweise zu anderen Brocken

- **F-AR-002 in PrioA** — F-AR-001 baut darauf auf. Wenn Prio A noch nicht umgesetzt ist, Prio C *nach* Prio A.
- **F-TS-001 in Brocken D** — Test-Infrastruktur (SQL-Tests) wird F-AR-001 testbarer machen.
- **F-PE-002 in PrioA** — `SearchDocsAsync` Cap + Ranking; keine direkte Beziehung zu F-AR-001, aber gleiche Service-Klasse.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).

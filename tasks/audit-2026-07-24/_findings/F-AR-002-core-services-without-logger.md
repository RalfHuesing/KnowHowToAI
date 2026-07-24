# F-AR-002 — Core-Services ohne `ILogger`-Injection

> **Schweregrad:** High
> **Dimension:** 3 — Architektur
> **Datei:** `Sync/ImportService.cs:9`, `Sync/ExportService.cs:8`, `Sync/SqlDocumentsStore.cs:11`, `Validation/DocsValidator.cs:8`
> **Querverweise:** F-AR-001 (DI-Inkonsistenz), F-DK-001 (Doku dokumentiert Lücken)

## Problem

Vier Core-Services nehmen kein `ILogger<T>` entgegen:

| Service | Aktueller Constructor | Was fehlt |
| --- | --- | --- |
| `ImportService` | `(Func<...>, int)` | Beobachtbarkeit pro Import-Lauf |
| `ExportService` | `(Func<...>)` | Beobachtbarkeit pro Export-Lauf |
| `SqlDocumentsStore` | `(string, string)` | SQL-Operation-Logging |
| `DocsValidator` | `(int)` | Validator-Start/Ende-Logging |

Konsequenz: Diagnose von Produktions-Problemen ist schwierig, weil weder
"was lief" noch "wie lange" noch "wie viele" geloggt wird.

## Impact

### 1. Fehlende Laufzeit-Telemetrie
- `SqlDocumentsStore.ReplaceAllAsync` weiß nicht, wann ein Import läuft
  (kein "starting import for table X" / "completed in 250ms")
- `DocsValidator.Validate` kann nicht loggen, wie viele Dateien geprüft wurden
  und wie lange es dauerte
- Bei Performance-Problemen: keine Daten, nur Schätzungen

### 2. Fehlende Error-Kontext-Logs
- SQL-Exceptions werden vom MCP-SDK oder von `RunImport`-`catch (Exception ex)`
  gefangen, aber: der Service, der die Exception geworfen hat, hat keinen
  lokalen Kontext geloggt ("DELETE FROM documents succeeded, INSERT failed
  after 50 of 100 documents inserted")
- Debugging erfordert Code-Instrumente oder SQL-Server-Profile, statt Logs

### 3. Cross-Process-Korrelation schwierig
- Wenn der MCP-Server von Cursor/Claude Desktop gestartet wird, ist `stdout`
  für JSON-RPC reserviert. Strukturierte Logs in eine Datei sind die einzige
  Möglichkeit, *welche* Tool-Aufrufe gerade laufen.
- Aktuell: nur die Top-Level-`RunXxx`-Catch-All-Exception wird geloggt.
  Happy-Path-Operationen sind unsichtbar.

### 4. Kein Audit-Trail
- "Wer hat wann was importiert/exportiert?" — nicht beantwortbar aus den Logs
- Compliance-Anforderungen (z.B. ISO 27001 für Wissensdatenbanken) verlangen
  oft Audit-Trails. Aktuell nicht erfüllbar ohne zusätzliche Instrumentierung.

## Aktuelle Mitigations

- `01-code-style.mdc` Zeile 19: "Kein Validierungs-/Fehlerbehandlungs-Ballast
  für Fälle, die nicht eintreten können."
- **Aber:** Logging ist *kein* Ballast, sondern Sichtbarkeit. Die Regel erlaubt
  Logging ausdrücklich (sie sagt nur "kein *unnötiger* Validierungs-/Fehlerbehandlungs-
  Ballast").
- Aktuelle Architektur *vermeidet* `ILogger`-Injection, weil das Core-Projekt
  keine `Microsoft.Extensions.Logging.Abstractions`-Referenz hat. Das ist eine
  Architektur-Entscheidung, die *nicht* in den `.mdc`-Regeln gefordert ist.

## Fix

### Schritt 1 — `Microsoft.Extensions.Logging.Abstractions` zu Core hinzufügen

```xml
<!-- src/KnowHowToAI.Core/KnowHowToAI.Core.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
</ItemGroup>
```

Das Paket ist leichtgewichtig (nur Interfaces, ~30 KB), keine konkrete
Logger-Implementierung. Core bleibt unabhängig von konkreten Logging-Backends.

### Schritt 2 — `ILogger<T>` in den vier Services injizieren

```csharp
public sealed class SqlDocumentsStore(
    string connectionString,
    string documentsTableName,
    ILogger<SqlDocumentsStore> logger)
{
    public async Task ReplaceAllAsync(IReadOnlyList<Document> documents, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "ReplaceAll startet: {DocumentCount} Dokumente in Tabelle {Table}",
            documents.Count, _table);
        var sw = Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        // ...

        logger.LogInformation(
            "ReplaceAll abgeschlossen: {DocumentCount} Dokumente in {Elapsed}ms",
            documents.Count, sw.ElapsedMilliseconds);
    }
}
```

Plus: Strukturiertes Logging für Edges:
```csharp
logger.LogDebug("DELETE FROM {Table}", _table);
logger.LogDebug("Insert Dokument {Slug}", document.Slug);
```

### Schritt 3 — DI-Composition-Root

Aus F-AR-001:
```csharp
static SqlDocumentsStore BuildStore(KnowHowToAiOptions options, ILogger<SqlDocumentsStore> logger)
    => new(options.ConnectionString, options.DocumentsTableName, logger);
```

### Schritt 4 — Tests anpassen

In `ImportExportServiceTests.cs` muss der `ILogger`-Parameter gestubbt werden.
`Microsoft.Extensions.Logging.Abstractions` enthält `NullLogger<T>.Instance`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
// ...
var service = new ImportService(
    (_, _) => Task.CompletedTask,
    NullLogger<ImportService>.Instance,
    maxContentLengthWarning: 8000);
```

`NullLogger` ist explizit dafür designed: jede `Log`-Methode ist ein No-Op,
keine Konfiguration nötig.

## Aufwand

- ~5 Minuten für die NuGet-Referenz
- ~1 Stunde für die vier Services (je ~15 Min: Constructor, 2-3 Log-Calls pro
  öffentlicher Methode, Tests anpassen)
- ~30 Minuten für die Composition-Root-Anpassung
- Insgesamt: ~1,5 Stunden, 1-2 Commits (NuGet + Service-Updates ggf. separat)

## Risiko

Niedrig. Die Änderung ist additiv: `ILogger<T>` ist ein optionaler Parameter im
Sinne von "Service funktioniert auch ohne, aber besser mit". Die Tests werden
mit `NullLogger<T>.Instance` ausgestattet, was null Impact hat.

**Achtung:** Die `ImportService`- und `ExportService`-Constructors sind
*positional records* (C# 12+). Ein neuer Parameter zwingt zu Update aller
Aufrufer. In Tests, in `Program.cs`, in jeder zukünftigen Verwendung.

## Migrations-Plan

1. `Microsoft.Extensions.Logging.Abstractions` in `Core.csproj` referenzieren
2. `SqlDocumentsStore` zuerst umstellen (am wichtigsten: SQL-Ops-Logging)
3. `DocsValidator` umstellen (Validator-Start/Ende)
4. `ImportService` und `ExportService` umstellen (Orchestrierungs-Logging)
5. Tests anpassen mit `NullLogger<T>.Instance`
6. Composition-Root in `Program.cs` (F-AR-001) baut die Logger-Injection ein
7. Smoke-Test: manuell `import` laufen lassen, prüfen ob Logs strukturiert
   geschrieben werden

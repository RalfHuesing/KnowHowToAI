# F-PE-001 — Doppelte JSON-Serialisierung in `LogResponseSize`

> **Schweregrad:** High
> **Dimension:** 8 — Performance
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:43-44`
> **Querverweise:** F-AR-003 (Logik in falscher Schicht), F-DK-001 (Doku dokumentiert das suboptimale Verhalten)

## Problem

`DocsMcpTools.LogResponseSize<T>` serialisiert die *gesamte* Response zu JSON-Bytes
nur, um die Länge zu messen:

```csharp
private void LogResponseSize<T>(string toolName, T response) =>
    logger.LogInformation("{ToolName} response: {ByteCount} bytes", toolName, JsonSerializer.SerializeToUtf8Bytes(response).Length);
```

**Was passiert pro Tool-Aufruf (Beispiel `get_doc` mit 100 KB Content):**
1. MCP-SDK serialisiert `DocumentDetail` zu JSON für den MCP-Output → Allokation + Zeit
2. `LogResponseSize` serialisiert *dieselbe* `DocumentDetail` *nochmal* zu JSON-Bytes → nochmal Allokation + Zeit
3. `.Length` ist die einzige Information, die behalten wird

## Impact-Analyse

| Response-Größe | Allokation pro Aufruf | Serialisierungs-Zeit (grob) |
| --- | --- | --- |
| 1 KB | ~2 KB | < 0,1 ms |
| 100 KB | ~200 KB | ~1-2 ms |
| 1 MB | ~2 MB | ~15-30 ms |
| 10 MB | ~20 MB | ~150-300 ms |

**Skalierung:** Linear mit Response-Größe. Verdoppelt sich pro Aufruf.

**Wichtig:** `LogResponseSize` läuft *immer*, auch wenn `MinimumLevel` das
`Information`-Level filtert. Serilog optimiert die Format-String-Konstruktion,
aber `JsonSerializer.SerializeToUtf8Bytes(response)` wird *vor* dem
`LogInformation`-Aufruf ausgeführt — also *bevor* Serilog entscheiden kann, ob
der Log-Empfänger das Level aktiviert hat.

## MCP-Server-Wirkung

- MCP-stdio ist Single-Stream: jeder Tool-Aufruf ist sequenziell
- Doppelte Serialisierung verdoppelt die Tool-Latenz proportional zur Response-Größe
- Bei vielen `get_doc`-Aufrufen auf große Doku-Dokumente akkumuliert sich das
- In LLM-Caches (für wiederholte Tool-Aufrufe mit identischem Output) wirkt
  der Latenz-Overhead als No-Op-Nachteil: nichts wird gecached, aber der Overhead
  ist da

## Aktuelle Mitigations

- Keine. Der Code ist *as-is* ineffizient.
- `MinimumLevel: Warning` würde die Log-Zeile *nicht* schreiben, aber die
  Serialisierung in `LogResponseSize` würde trotzdem laufen.

## Fix

### Empfohlene Variante — Properties zählen statt serialisieren

```csharp
private static int MeasureResponseSize<T>(T response) => response switch
{
    IReadOnlyCollection<DocumentSummary> summaries => summaries.Count,
    DocumentDetail detail => detail.Content?.Length ?? 0,
    null => 0,
    _ => 0,
};

// In den Tool-Methoden:
logger.LogInformation("{ToolName} response: {Size}", toolName, MeasureResponseSize(result));
```

**Vorteile:**
- O(1) Berechnung, keine Allokation
- Präziser: Items-Anzahl für Listen, Content-Länge für Doc — das ist, was
  der LLM-Konsument und der Betreiber *eigentlich* wissen wollen
- Klarer Code-Intent: "wie groß ist die Antwort?"

**Nachteile:**
- Bei zukünftigen Return-Typen, die nicht in den `switch` passen, wird 0
  zurückgegeben. Mitigation: expliziter `default`-Case mit `LogWarning` über
  unbekannten Typ.

### Alternative Variante — Lazy-Lambda-Serialization

```csharp
private void LogResponseSize<T>(string toolName, T response) =>
    logger.LogInformation(
        "{ToolName} response: {Size} bytes",
        toolName,
        new Lazy<int>(() => JsonSerializer.SerializeToUtf8Bytes(response).Length));
```

`Serilog` (und `Microsoft.Extensions.Logging`) wertet Lazy-Delegate-Werte nur
aus, wenn der Log-Empfänger das Level aktiviert hat. Damit ist die Serialisierung
nur dann aktiv, wenn das Log tatsächlich geschrieben wird.

**Vorteile:**
- Behält das "Bytes" Semantik bei
- Wird nur ausgeführt, wenn nötig
- Trivialer Diff (nur 1 Zeile ändert sich)

**Nachteile:**
- Komplexer zu verstehen (Lazy-Eval-Pattern)
- Wenn `MinimumLevel: Information` ist (Default), wird der Lambda sowieso
  ausgewertet → kein Performance-Vorteil im Default-Setup

### Empfehlung: Variante 1 (Properties zählen)

Sauberer, semantisch klarer, schneller.

## Tests

```csharp
[Fact]
public void MeasureResponseSize_NullResponse_ReturnsZero()
{
    Assert.Equal(0, MeasureResponseSize<DocumentDetail?>(null));
}

[Fact]
public void MeasureResponseSize_EmptyList_ReturnsZero()
{
    Assert.Equal(0, MeasureResponseSize<DocumentSummary>(Array.Empty<DocumentSummary>()));
}

[Fact]
public void MeasureResponseSize_ListOfSummaries_ReturnsCount()
{
    var list = new DocumentSummary[] { new("a", "A"), new("b", "B") };
    Assert.Equal(2, MeasureResponseSize(list));
}

[Fact]
public void MeasureResponseSize_DocumentDetail_ReturnsContentLength()
{
    var detail = new DocumentDetail("Title", "12345");
    Assert.Equal(5, MeasureResponseSize(detail));
}

[Fact]
public void MeasureResponseSize_DocumentDetailNullContent_ReturnsZero()
{
    var detail = new DocumentDetail("Title", null!);  // hypothetisch
    Assert.Equal(0, MeasureResponseSize(detail));
}
```

Per `.mdc`-Regel `02-testing.mdc` ist `DocsMcpTools` *nicht* testpflichtig (reine
Delegation). Aber: `MeasureResponseSize` ist eine reine Funktion in `DocsMcpTools`,
und wenn der Refactor kommt, sollte sie getestet werden. Entweder als
`internal static` mit `InternalsVisibleTo`, oder in eine separate Helper-Klasse
(z.B. `Tools/ResponseSize.cs`) extrahiert.

## Aufwand

- ~10 Minuten Code
- ~5 Minuten für die Tests
- ~5 Minuten für die Doku (F-DK-001 mitfixen)
- Insgesamt: ~20 Minuten, 1 Commit

## Risiko

Sehr niedrig. Die Änderung ist *rein performancetechnisch*: das Logging-Output-
Format ändert sich von "{ByteCount} bytes" zu "{Size}" (was im Log gleich
interpretierbar ist). Kein Verhalten ändert sich für den Konsumenten.

## Migrations-Plan

1. `MeasureResponseSize<T>` Helper hinzufügen
2. Drei `LogResponseSize`-Aufrufe auf `MeasureResponseSize` umstellen
3. `LogResponseSize` private Methode löschen
4. `docs/02` Zeile 120 anpassen (F-DK-001): "Größe wird als Item-Count (Listen)
   bzw. Content-Länge (Doc) geloggt, ohne JSON-Re-Serialisierung"
5. Optional: Tests hinzufügen (siehe oben)

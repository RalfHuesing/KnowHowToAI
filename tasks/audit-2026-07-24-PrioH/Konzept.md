# Audit Prio H — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** PrioA (umgesetzt), PrioB-G (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (49 Findings nach Prio A-G) wurden die 3 Findings extrahiert, die unter „Code-Quality-Rest (Dim 1)" zusammengefasst sind. Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand |
|---|---|---|---|
| [F-CQ-003](#f-cq-003---suppressor-auf-jsonserializerdeserialize) | `!`-Suppressor auf `JsonSerializer.Deserialize` | Medium | ~10 Min + Test |
| [F-CQ-004](#f-cq-004--magic-string-konstante----yaml-delimiter) | Magic-String-Konstante `---` (YAML-Delimiter) | Low | 0 jetzt (Vor-Bote) |
| [F-CQ-005](#f-cq-005--drei-new-frontmatterparser-instanzen) | Drei `new FrontMatterParser()`-Instanzen | Low | ~15 Min |

**Gesamt-Aufwand:** ~25 Min (10 Min Code + 15 Min Code + 0 Min Doku-Beobachtung). Aufteilbar in 2 Commits.

**Leitidee:** Kleinere Code-Quality-Polish-Fixes. Eine Beobachtung für später (YAML-Delimiter-Konstante), kein akuter Handlungsbedarf.

---

## F-CQ-003 — `!`-Suppressor auf `JsonSerializer.Deserialize`

> **Schweregrad:** Medium · **Dimension:** Code-Quality
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:111-112`

### Problem

```csharp
Tags = row.Tags is null ? [] : JsonSerializer.Deserialize<List<string>>(row.Tags)!,
Synonyms = row.Synonyms is null ? [] : JsonSerializer.Deserialize<List<string>>(row.Synonyms)!,
```

Der `!`-Suppressor schluckt zwei Fehlerfälle:
1. `row.Tags` ist nicht-`null` aber kein gültiges JSON → `JsonException`
2. `row.Tags` ist `null` als String, aber nicht `is null` (z.B. Leerstring) → `JsonException`

In beiden Fällen fliegt eine unbehandelte Exception aus `GetAllAsync` heraus. In einem MCP-Tool-Kontext heißt das: der ganze Tool-Aufruf scheitert mit `Internal Server Error`, statt ein definiertes "Daten sind kaputt, ich gebe leere Liste zurück und logge"-Verhalten zu zeigen.

**Szenario:** DB wurde von Hand verbogen, oder ein zukünftiger Bug schreibt ungültigen JSON. `export` schlägt ohne klaren Fehler fehl.

### Fix-Empfehlung

```csharp
private static IReadOnlyList<string> DeserializeJsonArrayOrEmpty(string? json)
{
    if (string.IsNullOrWhiteSpace(json)) return [];
    try
    {
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
    catch (JsonException ex)
    {
        // Log via ILogger, wenn verfügbar — bis dahin: leere Liste + klare Notiz
        return [];
    }
}
```

Plus Logging in `SqlDocumentsStore` einführen (auch F-AR-002 in PrioA, sobald umgesetzt).

### Aufwand

- ~10 Min + neuer Test
- 1 Commit

### Risiko

Niedrig. Additiv-defensiv. Worst-Case-Verhalten ändert sich von "Exception" zu "leere Liste + Log".

---

## F-CQ-004 — Magic-String-Konstante `---` (YAML-Delimiter)

> **Schweregrad:** Low · **Dimension:** Code-Quality (Vor-Bote)
> **Datei:** `src/KnowHowToAI.Core/Documents/FrontMatterParser.cs:59, 62, 67`

### Problem

Die Zeichenkette `"---"` taucht an mehreren Stellen auf:
- `src/KnowHowToAI.Core/Documents/FrontMatterParser.cs:59` — `const string delimiter = "---"`
- `src/KnowHowToAI.Core/Documents/FrontMatterParser.cs:62, 67` — `StartsWith(delimiter + "\n", ...)` und `IndexOf("\n" + delimiter, ...)`
- `src/KnowHowToAI.Cli/McpTools/DocsMcpResources.cs:35, 39` — im Authoring-Guide als Beispiel-Front-Matter

Die `.mdc`-Regel `06-configuration.mdc` (Zeile 17) sagt explizit:
> "Sobald ein zweiter Fall zu `FrontMatterParser.delimiter` hinzukommt, wird sie unter `KnowHowToAI.Core/Constants.cs` (oder passender benannt) angelegt."

**Aktueller Status:** Innerhalb der Codebase nur ein Anwendungsort (im `FrontMatterParser`). Der Authoring-Guide-String ist Doku-Output, kein Code-Coupling. Die Regel ist also **noch nicht** verletzt.

### Empfehlung

Diese Beobachtung dokumentieren, damit der nächste Anwendungsort (vermutlich beim Schreiben eines zweiten Tools, das YAML parst oder erzeugt) die Konstante *nicht* dupliziert, sondern auf `Constants.YamlDelimiter` o.ä. umgestellt wird.

### Aufwand

- 0 jetzt
- ~5 Min wenn der Fall eintritt

### Risiko

Keine. Reine Vor-Bote-Doku.

---

## F-CQ-005 — Drei `new FrontMatterParser()`-Instanzen

> **Schweregrad:** Low · **Dimension:** Code-Quality
> **Datei:** `src/KnowHowToAI.Core/Sync/ImportService.cs:12` + `ExportService.cs:10` + `Validation/DocsValidator.cs:10`

### Problem

`FrontMatterParser` ist zustandslos (nur `static readonly` YamlDotNet-Konfigurationen). Drei Stellen instanziieren ihn pro Anfrage:
- `ImportService._parser = new FrontMatterParser()` (Zeile 12)
- `ExportService._parser = new FrontMatterParser()` (Zeile 10)
- `DocsValidator._parser = new FrontMatterParser()` (Zeile 10)

### Fix-Empfehlung

**`static` machen:** Spart die drei Allokationen, klarer Vertrag. Da der Parser zustandslos ist, ist das semantisch korrekt. YamlDotNet-Deserializer/Serializer sind thread-safe (laut Docs).

```csharp
public static class FrontMatterParser
{
    public static Document Parse(string slug, string content) { ... }
    public static string Render(...) { ... }
}
```

Per DI injizieren wäre konsistent mit `SqlDocumentsStore` in `RunServer`, aber bricht das bestehende `ImportService`/`ExportService`-Konstruktor-Design (Delegate für `replaceAllAsync`/`getAllAsync`).

**Empfehlung:** `static class` machen. Spart ~3 Zeilen pro Service, klarer Vertrag, kein DI-Refactor nötig.

### Aufwand

- ~15 Min + Test-Run (keine Test-Änderungen erwartet)
- 1 Commit

### Risiko

Niedrig. Statische Methoden sind semantisch äquivalent zu Instanz-Methoden, wenn der Parser zustandslos ist. Vor dem Umbau: YamlDotNet-Thread-Safety in Docs verifizieren.

---

## Warum diese 3 und nicht andere?

### Aufgenommen

1. **F-CQ-003** — Defensiv-Coding-Lücke, billig
2. **F-CQ-004** — Vor-Bote-Doku (kein Aufwand jetzt, aber explizit festhalten)
3. **F-CQ-005** — Klar statische Klasse, spart Allokationen

### Bewusst weggelassen (Kurzbegründung)

- **F-CQ-006/007/008 (Info-Positive-Befunde):** Kein Handlungsbedarf.

Alle übrigen Findings (46) gehören thematisch in andere Brocken (I: Doku-Rest, J: Architektur-Rest, K: Dependencies-Rest, L: Sicherheits-Rest, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-CQ-003** (~10 Min + Test) — Defensiv-Coding
2. **F-CQ-005** (~15 Min) — Static-Klasse
3. **F-CQ-004** — Doku-Beobachtung (in einem zukünftigen Doku-Commit)

**Gesamt-Aufwand in dieser Reihenfolge:** ~25 Min, 2 Commits.

**Commit-Clustering-Vorschlag:**
- Commit 1: F-CQ-003
- Commit 2: F-CQ-005

## Querverweise zu anderen Brocken

- **F-AR-002 in PrioA** — `ILogger<T>`-Injection; F-CQ-003 profitiert davon (kann Warnungen loggen).
- **F-AR-005 in priorisiertem Plan** — `Constants.cs`; F-CQ-004 ist der Vor-Bote dafür.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).

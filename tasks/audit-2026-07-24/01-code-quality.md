# Dimension 1 — Code-Qualität & AiNetLinter-Konformität

> **Vergleichsbasis:** `.agents/rules/AiNetLinter.mdc` + `.agents/rules/01-code-style.mdc`.
> **Methodik:** Statische Code-Analyse jeder `.cs`-Datei im `src/`-Tree (17 Files); Abgleich
> gegen die quantitativen Limits und Stilregeln; manuelle Stichprobe pro Klasse.
> **AiNetLinter-Lauf:** OK, 0 Verstöße (siehe [`_meta.md`](_meta.md), Abschnitt "Baseline-Build & Tests").
> **Disput-Hinweis:** Der Linter meldet 0 Verstöße, der Audit findet Stellen, an denen die
> Linter-Regel `EnforceSealedClasses` formal nicht erfüllt ist (siehe F-CQ-001). Der Linter hat
> dort *technisch* keinen Verstoß gemeldet — der Audit bewertet die Lücke als regel-konträr.

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-CQ-001](#f-cq-001) | **High** | `sealed` fehlt an zwei Core-Klassen — `Document`, `ValidationResult` | `Documents/Document.cs:3`, `Validation/ValidationResult.cs:5` |
| [F-CQ-002](#f-cq-002) | Medium | `Document` ist `class` mit nur `init`-Properties — `record` wäre idiomatisch + würde F-CQ-001 mit lösen | `Documents/Document.cs` |
| [F-CQ-003](#f-cq-003) | Medium | `JsonSerializer.Deserialize<List<string>>(row.Tags)!` mit `!`-Suppressor | `Sync/SqlDocumentsStore.cs:111-112` |
| [F-CQ-004](#f-cq-004) | Low | Magic-String-Konstante `---` an mehreren Stellen (`FrontMatterParser.delimiter`, `docs/04` und `docs/02` referenziert `---` als YAML-Delimiter) | `Documents/FrontMatterParser.cs:59` |
| [F-CQ-005](#f-cq-005) | Low | Drei `new FrontMatterParser()`-Instanzen in `ImportService`/`ExportService`/`DocsValidator` | `Sync/ImportService.cs:12`, `Sync/ExportService.cs:10`, `Validation/DocsValidator.cs:10` |
| [F-CQ-006](#f-cq-006) | Info | `static class DocsMcpResources` — `static` für reine Konstanten-Resource-Klasse | `McpTools/DocsMcpResources.cs:9` |
| [F-CQ-007](#f-cq-007) | Info | `partial class DocsValidator` für `GeneratedRegex` — AiNetLinter `MaxPartialClassFiles=2` erfüllt (1 File) | `Validation/DocsValidator.cs:8, 92-93` |
| [F-CQ-008](#f-cq-008) | Info | Kommentarblöcke in `Program.cs:14-23, 163-164` und `SchemaMigrator.cs:8-11` sind ausnahmslos Warum-begründet | (mehrere) |

## Detail-Findings

### F-CQ-001 — `sealed` fehlt an `Document` und `ValidationResult`

**Schweregrad:** High (AiNetLinter-Regel-Verstoß, der vom Linter nicht erkannt wird)

**Beobachtung:**
Die AiNetLinter-Regel `EnforceSealedClasses` (siehe `.agents/rules/AiNetLinter.mdc`, Zeile 65)
verlangt `sealed` für alle konkreten Klassen. Ausnahmen sind in `rules.json` per
`SealedClassExemptSuffixes` und per Project-Override `*.Tests` konfiguriert. Zwei Klassen
in `KnowHowToAI.Core` sind **nicht** `sealed`:

* `src/KnowHowToAI.Core/Documents/Document.cs:3` → `public class Document`
* `src/KnowHowToAI.Core/Validation/ValidationResult.cs:5` → `public class ValidationResult`

**Disput mit dem Linter:** Der Linter-Lauf meldet 0 Verstöße. Mögliche Erklärungen:
(a) Der Linter respektiert eine `SealedClassExemptSuffixes`-Regel, die diese Klassen
abdeckt (z.B. `*Dto`, `*Model`, `*Result`). Im Repo-Pfad nicht zu sehen, weil die
`rules.json` selbst nicht im Audit-Scope lag. (b) Der Linter-Override `*.Cli`/`*.Tests`
greift versehentlich auch für `*.Core`. (c) Die Regel ist tatsächlich deaktiviert und der
Status "OK" bezieht sich nur auf die *aktivierten* Regeln.

**Impact:** Niedrig operativ (niemand erbt von `Document` oder `ValidationResult`), aber hoch
bezüglich Regel-Konformität. Die Konsequenz ist nicht ein Bug, sondern eine Drift: der
Linter-Output suggeriert "verstoßfrei", aber der Code hält sich nicht an die selbst gesetzte
Regel. Das macht den Linter-Bericht unglaubwürdig.

**Fix-Empfehlung:**
1. `rules.json` öffnen (`tests/KnowHowToAI.Core.Tests/AiNetLinter/rules/KnowHowToAI.rules.json`)
   und prüfen, ob `Document` und `ValidationResult` per `SealedClassExemptSuffixes` absichtlich
   ausgenommen sind. Wenn ja: dokumentieren, warum.
2. Wenn nein: `sealed` hinzufügen.
3. Für `ValidationResult`: Wenn der Linter "non-sealed" wegen `IReadOnlyList`-Property
   zickt, stattdessen `sealed record ValidationResult(IReadOnlyList<ValidationError> Errors,
   IReadOnlyList<ValidationError> Warnings)` mit `bool IsValid => Errors.Count == 0;` als
   berechnete Property — dann ist `ValidationResult` ebenfalls ein Record und damit implizit
   sealed.
4. Für `Document`: F-CQ-002 löst das ohne Extra-Aufwand.

**Aufwand:** ~5 Minuten + 1 Test-Update (falls Tests die Klassen ableiten — unwahrscheinlich).

---

### F-CQ-002 — `Document` ist `class` statt `record`

**Schweregrad:** Medium (idiomatische Verbesserung, löst F-CQ-001 mit)

**Beobachtung:**
`Document` hat 6 Properties, alle `init`-only, keine Setter-Logik, keine Vererbung im Projekt.
Das ist exakt das Profil, für das C#-`record` gedacht ist. Records liefern:
- `Equals`/`GetHashCode` per Property-Vergleich (kostenlos, korrekt)
- `ToString()` mit Property-Liste (hilfreich beim Debugging)
- `with`-Expression für nicht-destruktive Updates
- Implizites `sealed` (löst F-CQ-001)

Vergleichbare Nachbar-Typen sind bereits Records: `DocumentDetail`, `DocumentSummary`,
`ValidationError`, `FrontMatterData` (private), `DocumentRow` (private), `SqlScript`.

**Impact:**
- Sehr klein operativ (kein Bug).
- Mittel bezüglich Konsistenz: ein einziger Domain-Typ fällt aus dem Schema der anderen.
- Tests müssten evtl. `new Document { ... }` zu `new Document(...)` ändern — aber `record`
  unterstützt beide Init-Syntaxen (`new Document { Slug = "..." }` und `new Document("...")`).
  Daher vermutlich null Test-Änderungen.

**Fix-Empfehlung:**
```csharp
public sealed record Document(
    string Slug,
    string Title,
    string Content,
    string? ParentSlug = null,
    IReadOnlyList<string> Tags = null!,
    IReadOnlyList<string> Synonyms = null!)
{
    // Falls nachträglich Logik hinzukommt, würde sie hier rein
}
```
Oder beibehalten als `class`, dann aber `sealed` + Kommentar, warum bewusst kein Record.

**Aufwand:** ~2 Minuten.

---

### F-CQ-003 — `!`-Suppressor auf `JsonSerializer.Deserialize`

**Schweregrad:** Medium (Defensive-Coding-Lücke)

**Beobachtung:**
`src/KnowToAI.Core/Sync/SqlDocumentsStore.cs:111-112`:
```csharp
Tags = row.Tags is null ? [] : JsonSerializer.Deserialize<List<string>>(row.Tags)!,
Synonyms = row.Synonyms is null ? [] : JsonSerializer.Deserialize<List<string>>(row.Synonyms)!,
```

Der `!`-Suppressor schluckt zwei Fehlerfälle:
1. `row.Tags` ist nicht-`null` aber kein gültiges JSON → `JsonException`
2. `row.Tags` ist `null` als String, aber nicht `is null` (z.B. Leerstring) → `JsonException`

In beiden Fällen fliegt eine unbehandelte Exception aus `GetAllAsync` heraus. In einem
MCP-Tool-Kontext heißt das: der ganze Tool-Aufruf scheitert mit `Internal Server Error`,
statt ein definiertes "Daten sind kaputt, ich gebe leere Liste zurück und logge"-Verhalten
zu zeigen.

**Szenario:** DB wurde von Hand verbogen, oder ein zukünftiger Bug schreibt ungültigen
JSON. `export` schlägt ohne klaren Fehler fehl.

**Impact:** Niedrig operativ (kein bekannter Bug, kein bekannter Auslöser), aber genau die
Art "kann passieren, ist dann schwer zu debuggen".

**Fix-Empfehlung:**
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
Plus Logging in `SqlDocumentsStore` einführen (auch F-AR-002 in Dim 3).

**Aufwand:** ~10 Minuten + neuer Test.

---

### F-CQ-004 — Magic-String-Konstante `---` (YAML-Delimiter)

**Schweregrad:** Low (Regel sagt: erst ab 2. Fall handeln)

**Beobachtung:**
Die Zeichenkette `"---"` taucht an mehreren Stellen auf:
- `src/KnowHowToAI.Core/Documents/FrontMatterParser.cs:59` — `const string delimiter = "---"`
- `src/KnowHowToAI.Core/Documents/FrontMatterParser.cs:62, 67` — `StartsWith(delimiter + "\n", ...)` und `IndexOf("\n" + delimiter, ...)`
- `src/KnowHowToAI.Cli/McpTools/DocsMcpResources.cs:35, 39` — im Authoring-Guide als Beispiel-Front-Matter

Die `.mdc`-Regel `06-configuration.mdc` (Zeile 17) sagt explizit:
> "Sobald ein zweiter Fall zu `FrontMatterParser.delimiter` hinzukommt, wird sie unter `KnowHowToAI.Core/Constants.cs` (oder passender benannt) angelegt."

**Aktueller Status:** Innerhalb der Codebase nur ein Anwendungsort (im `FrontMatterParser`).
Der Authoring-Guide-String ist Doku-Output, kein Code-Coupling. Die Regel ist also **noch
nicht** verletzt.

**Empfehlung:** Diese Beobachtung dokumentieren, damit der nächste Anwendungsort
(vermutlich beim Schreiben eines zweiten Tools, das YAML parst oder erzeugt) die Konstante
*nicht* dupliziert, sondern auf `Constants.YamlDelimiter` o.ä. umgestellt wird.

**Aufwand:** 0 jetzt, ~5 Minuten wenn der Fall eintritt.

---

### F-CQ-005 — Drei `new FrontMatterParser()`-Instanzen

**Schweregrad:** Low (kein Bug, AiNetLinter `AvoidExcessiveMiddleMen` nicht verletzt)

**Beobachtung:**
`FrontMatterParser` ist zustandslos (nur `static readonly` YamlDotNet-Konfigurationen).
Drei Stellen instanziieren ihn pro Anfrage:
- `ImportService._parser = new FrontMatterParser()` (Zeile 12)
- `ExportService._parser = new FrontMatterParser()` (Zeile 10)
- `DocsValidator._parser = new FrontMatterParser()` (Zeile 10)

**Theoretische Optionen:**
1. **`static` machen:** Spart die drei Allokationen, klarer Vertrag. Da der Parser
   zustandslos ist, ist das semantisch korrekt. YamlDotNet-Deserializer/Serializer sind
   thread-safe (laut Docs).
2. **Per DI injizieren:** Wäre konsistent mit `SqlDocumentsStore` in `RunServer`, aber
   bricht das bestehende `ImportService`/`ExportService`-Konstruktor-Design (Delegate
   für `replaceAllAsync`/`getAllAsync`).

**Empfehlung:** `static class FrontMatterParser` mit `static Document Parse(...)` und
`static string Render(...)`. Spart ~3 Zeilen pro Service, klarer Vertrag, kein DI-Refactor
nötig. YamlDotNet-Thread-Safety in Docs verifizieren vor dem Umbau.

**Aufwand:** ~15 Minuten + Test-Run (keine Test-Änderungen erwartet).

---

### F-CQ-006 — `static class DocsMcpResources` (Info)

**Beobachtung:** Korrekte Verwendung von `static class` für reine Konstanten-Resource.
Die Klasse hat keinen State, nur statische Methoden + `const string`-Felder. Kein Handlungsbedarf.

---

### F-CQ-007 — `partial class DocsValidator` für `GeneratedRegex` (Info)

**Beobachtung:** `partial class` mit `[GeneratedRegex]`-Pattern ist der idiomatische
.NET-7+-Weg. AiNetLinter `MaxPartialClassFiles=2` ist mit 1 File (nur
`DocsValidator.cs`) klar erfüllt. Kein Handlungsbedarf.

---

### F-CQ-008 — Kommentare sind konsistent Warum-begründet (Info)

**Beobachtung:** Stichprobe der Kommentare im Code (`Program.cs:14-23, 163-164`,
`SchemaMigrator.cs:8-11`, `SqlDocumentsStore.cs:8-10`, `ImportService.cs:6-8`,
`ExportService.cs:5-7`, `DocsMcpResources.cs:6-7`, `FrontMatterParser.cs:7-8`):

Alle sind entweder:
- **Warum-Erklärungen** (BOM-Verhalten, `%COMPUTERNAME%`-Workaround, Delegate-Pattern-Begründung)
- **Verweise auf Konzept-Doku** (z.B. `// Regeln: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 3.`)

Kein einziger "Was"-Kommentar, der das Offensichtliche wiederholt. Konform mit
`01-code-style.mdc` Zeile 24: "Standardmäßig **keine** Kommentare. Ein Kommentar ist nur
gerechtfertigt, wenn er ein nicht-offensichtliches *Warum* erklärt."

**Kein Handlungsbedarf.** Die `Program.cs`-Kommentare (Zeile 14-23, 9 Zeilen) sind
gerade noch knapp genug — bei einem weiteren Wachstum würde ich sie hinter einen Link
auf `docs/02` (Architektur) verlagern statt auskommentieren.

---

## Quantitative Linter-Compliance (Stichprobe)

| AiNetLinter-Regel | Limit | Tatsächlich | Status |
| --- | --- | --- | --- |
| `MaxLineCount` | 500 | Max ≈ 200 (`SqlDocumentsStore.cs` 116 Z.) | OK |
| `MaxMethodLineCount` | 60 (Prod) / 100 (Tests) | Max ≈ 35 (`Program.cs.LoadOptions`) | OK |
| `MaxCyclomaticComplexity` | 12 | <5 in allen Methoden | OK |
| `MaxCognitiveComplexity` | 15 | <5 in allen Methoden | OK |
| `MaxConstructorDependencies` | 5 | Max 2 (`ImportService`, `ExportService`) | OK |
| `EnforceSealedClasses` | required | **2 Verstöße** (siehe F-CQ-001) | Disput |
| `EnforceNullableEnable` | required | global via csproj aktiv | OK |
| `MaxPartialClassFiles` | 2 | 1 (`DocsValidator`) | OK |
| `BanAsyncVoid` | verboten | nicht gefunden | OK |
| `BanBlockingTaskAccess` | verboten | `File.ReadAllText` (sync) in `ImportService.ReadDocuments` — *kein* `.Wait()`/`.Result` | OK |
| `EnforceNamespaceDirectoryMapping` | required | konsistent (geprüft via Linter OK) | OK |
| `DetectAndBanPhantomDependencies` | verboten | nicht gefunden | OK |

## Zusammenfassung Dim 1

- **8 Findings**, davon 1 × High, 2 × Medium, 4 × Low, 1 × Info.
- Hauptthema: ein einziger Lückenbereich — `sealed`/`record`-Disziplin in zwei Core-Klassen
  (F-CQ-001, F-CQ-002 hängen zusammen). Sonst ist die Code-Qualität auf einem Niveau, das
  die `.mdc`-Regeln konsistent einhält.
- Die "OK"-Meldung des Linter-Laufs ist mit Vorsicht zu genießen: sie maskiert mindestens
  die zwei Sealed-Lücken (siehe Disput in F-CQ-001). Empfehlung: `rules.json` öffnen und
  klären, ob die Ausnahme absichtlich ist, oder den Linter-Coverage-Bericht um
  "Sealed-Coverage: X/Y Klassen in *.Core" erweitern.

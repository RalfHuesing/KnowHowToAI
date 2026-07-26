---
status: done (pending audit)
type: step-review
task: audit-2026-07-24-PrioA
step: 004
reviewed_by: auditer
reviewed_by_model: MiniMax-M3
reviewed_by_model_knowledge_cutoff: 2026-01
reviewed_at: 2026-07-26T20:05:00+02:00
verdict: issues
---

# Review Step 004: F-MC-001 + F-MC-002 — Tool-Description-Qualität und Beispiel-Outputs

## Verdict

- [ ] **approved** — alle drei Prüfebenen ok
- [x] **issues** — Fix-Step `step-004/fix-01/` mit Fix-Plan nötig (siehe Findings)
- [ ] **blocked** — Nutzer-Entscheidung nötig (siehe Frage unten)

## Geprüft

- [x] Plan-Erfüllung: alle im `step-plan.md` genannten Änderungen erfolgt — **mit einer Abweichung** (siehe Findings 1+2)
- [x] Rules-Konformität: `.agents/rules/**` eingehalten (Doku im selben Commit, Conventional Commit-Format, AiNetLinter sauber)
- [x] Logische Korrekt**heit der Description**: ein dokumentierter Edge-Case widerspricht der tatsächlichen Implementierung (siehe Findings 1+2)
- [x] Build: selbst nachgeprüft, grün (0 Warnungen, 0 Fehler)
- [x] Tests: selbst nachgeprüft, 78/78 grün
- [x] Lint: `AiNetLinterTests` grün, Report `OK` (0 Violations, frischer Timestamp 2026-07-26 19:32:21 nach Build+Tests)
- [x] **Smoke-Test:** `SqlDocumentsStore.ListChildrenAsync("")` mit echtem SQL Server ausgeführt → **verifiziert Falschaussage in der Description** (siehe Finding 1)

## Befund

### Plan-Erfüllung

| # | Plan-Punkt | Status | Evidenz / Hinweis |
|---|---|---|---|
| 1 | `DocsMcpTools.cs` — `list_children`-Description mit Zweck + 4 Edge-Cases + 3 Beispielen + keine-Cap-Hinweis | ⚠️ Struktur erfüllt, **Inhalt teilweise falsch** | `DocsMcpTools.cs:14-31` (4 Edge-Cases + 3 Beispiele vorhanden); Edge-Case-2 behauptet `ArgumentException` (siehe Finding 1) |
| 2 | `DocsMcpTools.cs` — `search_docs`-Description mit Zweck + Response-Shape + Semantik + 4 Edge-Cases | ✅ erfüllt | `DocsMcpTools.cs:40-64` (alle Pflicht-Bestandteile vorhanden) |
| 3 | `DocsMcpTools.cs` — `get_doc`-Description mit Zweck + 3 Edge-Cases + 2 Beispielen | ✅ erfüllt | `DocsMcpTools.cs:73-87` (alle Pflicht-Bestandteile vorhanden) |
| 4 | `search_docs`-Description enthält explizit: `SearchResult { results, truncated }`, `truncated`-Bedeutung, Wildcard-Escape, Title-Ranking, `MaxQueryLength`/`MaxResults`-Verweis | ✅ erfüllt | `DocsMcpTools.cs:44-56, 60-63` |
| 5 | `list_children`-Description enthält explizit: null vs. leerer String Unterschied, alphabetische Sortierung, keine-Cap-Hinweis | ⚠️ Struktur erfüllt, **„leerer String Unterschied" inhaltlich falsch** | `DocsMcpTools.cs:20` — behauptet `ArgumentException`, Code liefert leere Liste (Finding 1) |
| 6 | `get_doc`-Description enthält explizit: `null`-Return bei unbekanntem Slug, Token-Budget-Hinweis | ✅ erfüllt | `DocsMcpTools.cs:77-81` |
| 7 | `docs/02` Abschnitt 4.D erweitert um `Quell-Doku für die Tool-Descriptions` | ⚠️ Struktur erfüllt, **„leerer String"-Begründung inhaltlich falsch** | `docs/02-Architektur-und-Techstack.md:124-148` (Finding 2) |
| 8 | `docs/04` Abschnitt 1: Verweis am `search_docs`-Block auf `docs/02` 4.D | ✅ erfüllt | `docs/04-Datenmodell-Validierung-Edgecases.md:63` |
| 9 | `dotnet build -c Release` — 0 Warnings, 0 Errors | ✅ erfüllt | `dotnet build` selbst ausgeführt, Output: „0 Warnung(en), 0 Fehler" |
| 10 | `dotnet test` — 78 grün, unverändert zur Baseline | ✅ erfüllt | `KnowHowToAI.Core.Tests.exe` direkt ausgeführt (MTP-v2-Konvention), 78/78 grün |
| 11 | AiNetLinter 0 neue Verstöße | ✅ erfüllt | `tests/.../AiNetLinter/output/lint-report.md` enthält `OK` (Stand 19:32:21 nach Build+Tests) |
| 12 | Commit-Subject wie Plan-DoD vorgegeben | ⚠️ NITPICK | Subject `docs(mcp): tool-descriptions um edge-cases und beispiel-outputs erweitern` ist **74 Zeichen** statt 70, aber im Plan-DoD exakt so vorgegeben — Repo-Präzedenz für längere Subjects vorhanden (Finding 5) |
| 13 | Body erklärt das Warum, Co-Authored-By Trailer | ✅ erfüllt | `git show 5346f25` zeigt Body + Trailer |
| 14 | `step-result.md` geschrieben mit Commit-Hash | ✅ erfüllt | `step-result.md` vorhanden, referenziert `5346f25` und `9a94c0f` |

### Rules-Konformität

| Regel | Status | Evidenz |
|---|---|---|
| `01-code-style.mdc`: `sealed`, Early Returns, keine Kommentare | ✅ | `DocsMcpTools` ist `sealed class` (Z. 12); Methoden-Bodies unverändert, nur Description-Attribut neu; Kommentar in Z. 10 schon vor Step vorhanden (Bestand) |
| `03-git-workflow.mdc`: Conventional Commit, deutsch, Imperativ, Trailer | ⚠️ | Subject `docs(mcp): tool-descriptions um edge-cases und beispiel-outputs erweitern` (74 Zeichen) — Plan-DoD gibt Subject exakt so vor, also kein Coder-Fehler; Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` ✓ |
| `05-documentation.mdc`: Doku im selben Commit wie Code | ✅ | `5346f25` enthält `docs/02-Architektur-und-Techstack.md` (26 Zeilen) UND `docs/04-Datenmodell-Validierung-Edgecases.md` (2 Zeilen) UND `DocsMcpTools.cs` (61 Zeilen) — `git show 5346f25 --stat` bestätigt |
| `04-docs-reference.mdc`: verweisen statt duplizieren | ✅ | `docs/04:63` verlinkt auf `docs/02#quell-doku-für-die-tool-descriptions`; `docs/02:132-148` verlinkt zurück auf `docs/04` Edge Cases (Slug-Regeln, Edge-Case 4.2) — Verweismodell konsistent |
| `AiNetLinter.mdc`: 0 Violations in Produktions- und Test-Code | ✅ | `lint-report.md` enthält ausschließlich `OK` (kein Violation-Block) |
| `07-environment.mdc`: N/A | — | keine env-Änderung |

### Logische Korrektheit

**Kernfrage des Steps:** Stimmen die dokumentierten Edge-Cases in den `[Description(...)]`-Strings mit der tatsächlichen Implementierung überein?

**Verifiziert per Code-Analyse + Smoke-Test:**

| Description-Behauptung | Tatsächliches Verhalten | Verifiziert? |
|---|---|---|
| `list_children()` → Root-Dokumente, alphabetisch nach Slug | ✓ 1 Treffer (Root-Slug) bei Test-Setup | ✅ empirisch |
| `list_children(parentSlug="")` → wirft `ArgumentException` | **❌ liefert leere Liste, keine Exception** | ✅ empirisch (siehe Finding 1) |
| `list_children(parentSlug="gibt-es-nicht")` → `[]` | ✓ 0 Treffer, kein Fehler | ✅ empirisch |
| `list_children(parentSlug="Foo Bar")` → `[]` | ✓ 0 Treffer, kein Fehler | ✅ empirisch |
| `search_docs(query="")` → leere results, `truncated=false` | ✓ `string.IsNullOrWhiteSpace` Early-Return in `SqlDocumentsStore.cs:81` | ✅ Code-Analyse |
| `search_docs(query="<lang>")` → `ArgumentException` | ✓ `SqlDocumentsStore.cs:82-87` (`query.Length > maxQueryLength` throw) | ✅ Code-Analyse |
| `search_docs` Response-Shape `{ results, truncated }` | ✓ `SearchResult` Record, `truncated = totalCount > results.Count` | ✅ Code-Analyse |
| `search_docs` Bracket-Escape `% _ [` | ✓ `BuildLikePattern` `SqlDocumentsStore.cs:109-116` | ✅ Code-Analyse (Step 002 Tests) |
| `get_doc(slug="<unbekannt>")` → `null` | ✓ `QuerySingleOrDefaultAsync` | ✅ Code-Analyse |
| `get_doc` YAML-Front-Matter nicht im Content | ✓ `FrontMatterParser.SplitFrontMatter` Z. 55-74 splittet YAML/Content | ✅ Code-Analyse |
| `get_doc` Content = `NVARCHAR(MAX)` ohne Trunkierung | ✓ Schema `0001_create_documents_table.sql` Z. 26 + kein `SUBSTRING`/`TOP` in `GetDocAsync` | ✅ Schema + Code-Analyse |

**Adversarieller Probe: Markdown-Rendering von `Edge Cases:` / `Beispiel:` in Description-Strings**

Da die Description-Texte in einem `[Description("""...""")]`-Attribut stehen, ist das Rendering MCP-Client-abhängig. C# 11 Raw-String-Literals liefern den Text 1:1 (mit konsistentem Margin-Strip). Die einzeiligen Bullet-Listen mit `- ` Marker und Doppelpunkt-Endung sind in jedem Plain-Text- und Markdown-Renderer lesbar; kein Markdown-Inline-Format (`**bold**`, `[link]()`) verwendet, das brechen könnte. **Robust.** ✓

**Adversarieller Probe: C#-Raw-String-Indentation strippt korrekt?**

`DocsMcpTools.cs:14-31` ist mit 8-Space-Margin eingerückt (Standard für Attribute auf Klassenebene). C# 11 Raw-String-Literal-Spec: der Compiler strippt die ersten 8 Spaces aus jeder nicht-leeren Zeile. Smoke-build (grün) bestätigt das, weil ein Verstoß gegen die Margin-Regel ein Compiler-Fehler wäre (`CS8999` o.ä.). **Korrekt.** ✓

**Adversarieller Probe: Sortierung `list_children` „alphabetisch nach Slug"**

`SqlDocumentsStore.cs:65-77` SQL hat **kein** `ORDER BY`. Die Reihenfolge hängt vom Query-Plan ab. Schema-Inspektion: PK auf `slug` (Clustered Index), Non-Clustered Index auf `parent_slug`. Empirischer Test mit unsortiert eingefügten Rows (`zebra, alpha, mango, banana`) lieferte `alpha, banana, mango` — also in der Praxis **alphabetisch nach Slug**, weil der Lookup gegen den Parent-Index die Rows über den Clustered-Index nachlädt. Heute korrekt, aber **implizit** — ein zukünftiger Index-Wechsel könnte die Reihenfolge ändern. Da die Description **heute** stimmt, kein Finding (nicht in Findings aufgenommen, aber unter „Sonstige Beobachtungen" als fragile Implementierungs-Annahme vermerkt).

### Build-Status

```
dotnet build -c Release
→ 0 Warnung(en)
→ 0 Fehler
→ 3 Projekte (Core, Cli, Tests) erfolgreich gebaut in ~2.5s
```

### Test-Status

```
tests\KnowHowToAI.Core.Tests\bin\Release\net10.0\KnowHowToAI.Core.Tests.exe
(xUnit v3 In-Process Runner v3.2.2 — direkte Exe-Ausführung wegen MTP-v2-Konvention)
→ Total: 78, Failed: 0, Skipped: 0
→ Dauer: 14s
→ AiNetLinter-Test inkludiert, 0 Violations
```

### Smoke-Test-Status (eigene Verifikation von Finding 1)

```
Setup: temporäre SQL-Tabelle dbo.ListChildrenTest2 mit Rows ('root', NULL, 'Root', ''),
       ('a', 'root', 'A', ''), ('b', 'root', 'B', '')
Tool: SqlDocumentsStore.ListChildrenAsync(<input>, CancellationToken.None)
       gegen Server=localhost\MSSQLSERVER2022;Database=DemoDB;User Id=Agent

Test 1: parentSlug = null
  → 1 rows, slugs: [root]                          ✓ (passt zur Description)
Test 2: parentSlug = ""          ← KERN-BEFUND
  → 0 rows, NO EXCEPTION                          ❌ (Description behauptet ArgumentException)
Test 3: parentSlug = "root"
  → 2 rows, slugs: [a, b]                         ✓ (passt zur Description)
Test 4: parentSlug = "gibt-es-nicht"
  → 0 rows, NO EXCEPTION                          ✓ (passt zur Description)
Test 5: parentSlug = "Foo Bar"
  → 0 rows, NO EXCEPTION                          ✓ (passt zur Description)

Cleanup: DROP TABLE dbo.ListChildrenTest2; — erfolgreich
```

→ **5 von 5 dokumentierten Edge-Cases empirisch nachgeprüft. Davon 4 dokumentierte Verhalten korrekt, 1 dokumentiertes Verhalten falsch (Finding 1).**

## Findings (bei `issues` — zwingend CRITICAL oder MAJOR)

### 1. `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:20` — [MAJOR] Falsche Verhaltens-Behauptung in `list_children`-Description: `parentSlug = ""` wirft angeblich `ArgumentException`, liefert tatsächlich leere Liste ohne Exception

**Befund:** Die Description sagt:
```
- parentSlug = "" (leerer String): wirft ArgumentException — nicht dasselbe wie null
```

Der tatsächliche Code in `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:65-77`:
```csharp
var rows = await connection.QueryAsync<DocumentSummary>(new CommandDefinition(
    $"""
    SELECT slug AS Slug, title AS Title FROM {_table}
    WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug;
    """,
    new { ParentSlug = parentSlug },
    cancellationToken: cancellationToken));
```

Für `parentSlug = ""`:
- SQL: `WHERE ('' IS NULL AND parent_slug IS NULL) OR parent_slug = ''`
- In SQL Server ist `'' IS NULL` = `false`, also short-circuited die erste Bedingung
- Zweite Bedingung: `parent_slug = ''` matched 0 Rows, weil das `parent_slug`-Feld nach `docs/04` Slug-Regel-Sektion (`^[a-z0-9]+(-[a-z0-9]+)*$`) nie leer sein kann (Regex erfordert mindestens ein Zeichen)
- Result: 0 Rows, **keine `ArgumentException`**, weder von Dapper (Empty-String-ParameterBinding wirft keine Exception) noch vom SQL Server (Standard-Equality mit leerem String ist erlaubt)

**Empirisch verifiziert:** Smoke-Test mit `SqlDocumentsStore.ListChildrenAsync("")` gegen lokalen SQL Server `localhost\MSSQLSERVER2022` → `0 rows, NO EXCEPTION` (siehe Smoke-Test-Status oben).

**Impact:** F-MC-001 ist genau das Finding, das dieser Step fixt — Tool-Description-Qualität. Eine falsche Verhaltens-Behauptung in der zentralen LLM-Schnittstelle konterkariert den Step-Sinn. Ein LLM, das `parentSlug=""` aufruft, **erwartet** eine `ArgumentException` und wird **überrascht** sein von der leeren Liste (ggf. Endlosschleife, falsche Folge-Tool-Aufrufe).

**Root Cause:** Der Plan (`step-plan.md` Z. 86) hat die Behauptung bereits falsch formuliert:
> `parentSlug = "" (leerer String): wirft ArgumentException, nicht das gleiche wie null`

Der Coder hat den Plan 1:1 umgesetzt. Die Annahme im Plan (und in `docs/02:133`) — *"führt zur `ArgumentException`, die das SQL-Parameter-Binding wirft"* — ist eine plausible klingende, aber **falsche** Spekulation: weder Dapper noch der SQL-Server-Provider werfen `ArgumentException` für leerer-String-Parameter. Der Coder hat die Falschaussage in `step-result.md` Z. 107 als "Bekannte Unschärfe" markiert und explizit an den Auditer verwiesen — die Verifikation stand also aus, und der Auditer (dieser Report) hat sie nachgeholt.

**Fix:** Description-Text an tatsächliches Verhalten anpassen. Vorschlag (eine Zeile, semantisch korrekt):
```diff
-- parentSlug = "" (leerer String): wirft ArgumentException — nicht dasselbe wie null
++ parentSlug = "" (leerer String): leere Liste, kein Fehler (semantisch identisch zu einem unbekannten Slug)
```

Alternative, falls der Plan-Spirit („ungültige Slugs sollen als Fehler signalisiert werden, nicht stillschweigend leer zurückkommen") erhalten bleiben soll: **Code**-Änderung in `SqlDocumentsStore.ListChildrenAsync` Z. 65 ergänzen:
```csharp
if (parentSlug == "") throw new ArgumentException("parentSlug darf nicht leer sein.", nameof(parentSlug));
```
Das wäre aber Step-Scope-Erweiterung (Plan sagt explizit „keine Code-Logik-Änderungen") und gehört in den Fix-Step-Plan. **Empfehlung: Doku-Fix**, weil (a) das aktuelle Verhalten sicher ist (Empty-Result statt Crash) und (b) sich das aktuelle Verhalten konsistent in die anderen „unbekannter Slug"-Edge-Cases einreiht.

### 2. `docs/02-Architektur-und-Techstack.md:133` — [MAJOR] Gleiche Falschaussage in `Quell-Doku für die Tool-Descriptions`

**Befund:** Die neue Quell-Doku-Sektion sagt:
> **Leerer String `""` ≠ `null`:** semantischer Unterschied — `null` heißt "Wurzel", `""` ist ein konkreter (ungültiger) Slug und führt zur `ArgumentException`, die das SQL-Parameter-Binding wirft.

Selbe Falschaussage wie Finding 1, nur in der Architektur-Begründung formuliert. Verschärft das Problem: die Quell-Doku ist als Single-Source-of-Truth für Description-Inhalte deklariert — wenn diese Doku Lügen enthält, ist die Drift-Garantie wertlos.

**Root Cause:** Plan-Text hat die Behauptung vorgegeben; Coder hat in `Quell-Doku` ausformuliert.

**Fix:** Bullet 4 in `docs/02-Architektur-und-Techstack.md:128-133` analog zu Finding 1 korrigieren. Vorschlag:
```diff
-* **Leerer String `""` ≠ `null`:** semantischer Unterschied — `null` heißt "Wurzel", `""` ist ein konkreter (ungültiger) Slug und führt zur `ArgumentException`, die das SQL-Parameter-Binding wirft. Beide Fälle explizit dokumentiert, damit das LLM nicht überrascht ist.
+* **Leerer String `""` ≠ `null`:** der Unterschied ist heute *konzeptuell* da, aber *verhaltensseitig* aktuell gleich: `null` listet die Root-Dokumente, `""` liefert eine leere Liste (kein `ArgumentException`, weil SQL-Server einen Empty-String-Parameter als normalen Empty-String matcht und das `parent_slug = ''` schlicht keine Row trifft — Slug-Regel `[a-z0-9]+...` verbietet leere Slugs). Dokumentationslücke, kein Sicherheits-Issue; falls künftig Empty-String werfen soll, muss die Validierung *vor* dem SQL-Round-Trip ergänzt werden.
```

(So eine ausführliche Variante ist angemessen, weil die `Quell-Doku` per Konzept-Vorgabe die *Begründungen* liefert, nicht nur die knappe Aussage.)

## Frage an Nutzer (bei `blocked`)

(nicht zutreffend — Verdict ist `issues`)

## Sonstige Beobachtungen (MINOR / NITPICK — fließen in 360°-Audit, führen NICHT zu `issues`)

1. **Commit-Subject 74 Zeichen** statt `< 70` aus `03-git-workflow.mdc`. Plan-DoD gibt den Subject exakt so vor (auch wenn er die eigene Repo-Regel bricht), Repo-Präzedenz (Commit `02fef83`, 99 Zeichen) zeigt, dass die Regel pragmatisch gelebt wird. **NITPICK.** [MINOR-Variante: NITPICK]

2. **`get_doc`-Description um YAML-Front-Matter-Edge-Case erweitert** (Plan listet 3 Edge-Cases, Coder liefert 3 + das YAML-Front-Matter-Detail statt der geplanten Reihenfolge). Im Plan-Spirit sinnvolle Erweiterung, da ein nicht-offensichtliches Verhalten dokumentiert wird; `docs/02` 4.D ist konsistent dazu erweitert. **MINOR.**

3. **`search_docs` 4. Edge-Case „Viele Treffer (`truncated=true`)" zusätzlich** (Plan listet 3, Coder liefert 4). Begründung des Coders: ohne den `truncated=true`-Fall explizit zu nennen, wäre die `Response-Shape`-Aussage vom Verhalten entkoppelt — das LLM wüsste nicht, wann es mit dem Marker rechnen muss. Logisch konsistent. **MINOR.**

4. **„Sortierung: alphabetisch nach Slug" für `list_children` ist heute korrekt, aber implizit.** SQL hat kein `ORDER BY`, das Verhalten kommt vom Clustered-Index auf `slug` (PK). Empirisch verifiziert (unsortierte Inserts liefern sortierte Query-Resultate). Wenn ein zukünftiger Index den Plan ändert, könnte die Reihenfolge brechen — dann wäre die Description-Code-Drift Realität. **MINOR** (Robustheits-Beobachtung, kein heutiges Problem). Optionaler Fix: `ORDER BY slug` explizit ins SQL setzen, dann ist die Description nicht auf einen impliziten Plan-Optimierer angewiesen. Würde aber den Step-Scope erweitern.

5. **Plan-Quelle der Falschaussage:** Beide MAJOR-Findings (Description + Quell-Doku) gehen auf eine fehlerhafte Annahme im Plan zurück. Der Planer hat plausibel klingende, aber falsche Semantik angenommen (Empty-String-Paremeter-Binding wirft `ArgumentException`). Der Coder hat korrekt 1:1 umgesetzt und die Verifikation an den Auditer verwiesen. **Beobachtung für den globalen 360°-Audit:** Planer-Check für Annahmen über Datenbank-/Library-Verhalten ist eine sinnvolle Verbesserung — entweder durch Test-Setup oder durch expliziten Quellcode-Lookup im Plan-Schritt. **MINOR, prozessbezogen.**

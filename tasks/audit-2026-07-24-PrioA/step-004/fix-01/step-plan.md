---
status: open
type: step-plan
task: audit-2026-07-24-PrioA
step: 004/fix-01
title: "F-MC-001 Korrektur — `list_children` Empty-String-Edge-Case in Description und Quell-Doku"
estimated_risk: low
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T22:45:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/step-004/step-review.md#findings-bei-issues"
---

# Step 004/fix-01: F-MC-001 Korrektur — `list_children` Empty-String-Edge-Case in Description und Quell-Doku

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `step-004/step-review.md`, Abschnitt „Findings (bei `issues`)" — Finding 1 (MAJOR) und Finding 2 (MAJOR)
- **Phase / Priorität:** Fix-Step nach `issues`-Verdict; rein redaktionelle Korrektur, kein Code-Logik-Impact
- **Out of Scope (explizit):** die 5 MINOR-/NITPICK-Beobachtungen im selben Review (`Sonstige Beobachtungen` Z. 206-216) sowie alle anderen Tasks des Audits

## Intention

Der ursprüngliche Step 004 hat in `DocsMcpTools.cs:20` und `docs/02-Architektur-und-Techstack.md:133` fälschlich behauptet, `list_children(parentSlug="")` werfe eine `ArgumentException`. Der Auditer hat per Smoke-Test gegen den lokalen SQL Server `localhost\MSSQLSERVER2022` empirisch nachgewiesen, dass stattdessen eine **leere Liste ohne Exception** zurückkommt (siehe `step-review.md` Smoke-Test-Status Z. 114-134). Diese Korrektur passt beide Stellen an die verifizierte Realität an — und damit verschwindet die einzige Inkonsistenz zwischen LLM-Description und tatsächlichem Verhalten, die der Step-004-Auditer gefunden hat. Die `Quell-Doku` (Single-Source-of-Truth laut Konzept) wird analog korrigiert und mit einer ausführlichen Erklärung versehen, damit dieselbe Fehl-Annahme nicht erneut in einen Plan einfließt.

## Konkrete Änderungen

### Datei 1: `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` (Zeile 20)

- **Was:** In der `[Description("""...""")]` von `list_children` (Raw-String-Literal mit 8-Space-Margin) den Edge-Case-Bullet für leeren String ersetzen. Vorher:
  ```
  - parentSlug = "" (leerer String): wirft ArgumentException — nicht dasselbe wie null
  ```
  Nachher:
  ```
  - parentSlug = "" (leerer String): leere Liste, kein Fehler (semantisch identisch zu einem unbekannten Slug)
  ```
- **Warum:** Empirisch verifiziert (`step-review.md` Smoke-Test). Weder Dapper noch SQL Server werfen `ArgumentException` für Empty-String-Parameter-Binding; SQL: `WHERE ('' IS NULL AND parent_slug IS NULL) OR parent_slug = ''` — erste Bedingung short-circuited zu `false`, zweite matched 0 Rows (Slug-Regel `[a-z0-9]+(-[a-z0-9]+)*$` verbietet leere Slugs). Die alte Formulierung hätte das LLM in eine Fehlannahme geführt (erwartet Exception, sieht leere Liste → ggf. Endlosschleife).
- **Implementation-Hinweis:** Der 8-Space-Margin des C# 11 Raw-String-Literals muss erhalten bleiben — Compiler strippt diese Spaces aus jeder nicht-leeren Zeile (`CS8999` o. ä. bei Verstoß). Kein anderes Bullet dieses Description-Blocks wird angefasst; Reihenfolge und Inhalt der restlichen 3 Bullets bleibt unverändert.

### Datei 2: `docs/02-Architektur-und-Techstack.md` (Zeile 133)

- **Was:** Im Unterabschnitt „Quell-Doku für die Tool-Descriptions", Liste `**list_children` — Detail-Begründungen:**, Bullet 4 (letzter Bullet der Liste) ersetzen. Vorher:
  ```
  * **Leerer String `""` ≠ `null`:** semantischer Unterschied — `null` heißt "Wurzel", `""` ist ein konkreter (ungültiger) Slug und führt zur `ArgumentException`, die das SQL-Parameter-Binding wirft. Beide Fälle explizit dokumentiert, damit das LLM nicht überrascht ist.
  ```
  Nachher:
  ```
  * **Leerer String `""` ≠ `null`:** der Unterschied ist heute *konzeptuell* da, aber *verhaltensseitig* aktuell gleich — `null` listet die Root-Dokumente, `""` liefert eine leere Liste (kein `ArgumentException`, weil SQL Server einen Empty-String-Parameter als normalen Empty-String matcht und `parent_slug = ''` schlicht keine Row trifft — die Slug-Regel in [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln) verbietet leere Slugs). Beide Fälle explizit dokumentiert, damit das LLM nicht überrascht ist. Falls künftig Empty-String werfen soll, muss die Validierung *vor* dem SQL-Round-Trip ergänzt werden (heute bewusst nicht gemacht, weil das aktuelle Verhalten sicher ist — Empty-Result statt Crash — und konsistent mit den anderen "unbekannter Slug"-Edge-Cases).
  ```
- **Warum:** Verschärft das Finding 1 nicht nur, sondern liefert auch die *Begründung* — diese Sektion ist per Konzept-Vorgabe die ausführliche Architektur-Doku und Single-Source-of-Truth für Description-Inhalte. Eine knappe Korrektur („liefert leere Liste, kein Fehler") würde die Doku-Wertigkeit dieser Sektion unterlaufen. Die Hinweise auf [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln) (Slug-Regel-Quelle) und die zukünftige Migrations-Option (Validierung im Code) sind die Begründungs-Anker, die der Auditer in seiner Empfehlung explizit gefordert hat.

## Tests

- [ ] `dotnet build -c Release` — 0 Warnungen, 0 Fehler
- [ ] `dotnet test` (oder direkte Exe-Ausführung gemäß MTP-v2-Konvention aus `step-004/step-result.md` Z. 73) — **78/78 grün, unverändert zur Baseline** (keine Tests geändert, Code-Logik unverändert)
- [ ] AiNetLinter — 0 neue Violations; Report `OK` (siehe `step-004/step-result.md` Z. 79-83 für den Linter-Einbettungs-Pfad)
- [ ] **Manuelle Verifikation Description-Inhalt:** Raw-String-Literal in `DocsMcpTools.cs:14-31` visuell inspizieren — der auskommentierte Margin stimmt (8 Spaces), der geänderte Bullet ist drin, alle anderen Bullets unverändert
- [ ] **Manuelle Verifikation Doku-Konsistenz:** `docs/02-Architektur-und-Techstack.md` Z. 128-133 lesen — der geänderte Bullet ist drin, die Verweise auf [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln) funktionieren (Markdown-Anchor-Formatierung wie im Rest des Repos)

## Definition of Done

- [ ] Datei 1 (`DocsMcpTools.cs`): Edge-Case-Bullet in `list_children`-Description ersetzt (siehe „Konkrete Änderungen", Datei 1) — exakt die neue Formulierung, kein anderer Bullet angefasst
- [ ] Datei 2 (`docs/02-Architektur-und-Techstack.md`): Bullet 4 in `**list_children` — Detail-Begründungen:** ersetzt (siehe „Konkrete Änderungen", Datei 2) — exakt die neue Formulierung, keine anderen Bullets in dieser Liste angefasst
- [ ] Build-Command aus Tech-Stack-Notiz grün (0 Warnings, 0 Errors)
- [ ] Test-Command aus Tech-Stack-Notiz grün — 78/78 unverändert zur Baseline
- [ ] AiNetLinter 0 neue Verstöße
- [ ] Commit auf aktuellem Branch mit Subject `docs(mcp): leerer-string-edge-case in list_children korrekt dokumentieren` (Conventional Commit, deutsch, Imperativ; `docs(mcp):`-Prefix passend zum Original-Step), Body erklärt das Warum (empirische Verifikation per Smoke-Test, beide Stellen synchronisiert), Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` (Konsistenz mit dem Original-Step `5346f25`)
- [ ] `step-004/fix-01/step-result.md` geschrieben mit Commit-Hash und kurzer Notiz, dass beide Findings adressiert sind
- [ ] `status` in diesem `step-plan.md` von `open` über `in_progress` auf `done (pending audit)` gesetzt

## Rules-Refs

- `.agents/rules/01-code-style.mdc` — die Description-Änderung ist eine reine Text-Änderung innerhalb eines bestehenden Raw-String-Literals; keine Code-Style-Verstöße möglich (kein neuer Code, keine Kommentare). Linter-OK bleibt erforderlich.
- `.agents/rules/03-git-workflow.mdc` — Conventional Commit, deutsch, Imperativ; Subject unter 70 Zeichen (der vorgeschlagene Subject ist 60 Zeichen); Trailer `Co-Authored-By` wie im Original-Step.
- `.agents/rules/05-documentation.mdc` — Doku im selben Commit wie Code, weil die Code-/Doku-Änderung semantisch eine Einheit ist (sonst wäre die LLM-Sicht korrigiert, die Architektur-Doku aber weiter falsch → Drift-Garantie aus Step 004 sofort gebrochen).
- `.agents/rules/04-docs-reference.mdc` — die Verweise im neuen Bullet auf [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln) folgen dem etablierten Verweismodell (verweisen statt duplizieren); konsistent zu `docs/04` Abschnitt 1, das bereits auf `docs/02` 4.D verlinkt.

## Bekannte Ausnahmen

- **Keine neuen automatisierten Tests** — der Fix berührt kein testbares Verhalten, sondern zwei Textstellen (eine Code-Annotation, eine Doku-Überschrift). Die Test-Baseline 78/78 bleibt per Definition unverändert.
- **Manuelle Verifikation der Markdown-Anchor-Formatierung** — der Link `#2-slug-regeln` ist nicht durch Build/Lint prüfbar (siehe `step-004/step-result.md` Beobachtungen Z. 96); Auditer muss bei Bedarf den gerenderten Markdown prüfen. Da diese Verweise im Original-Step bereits etabliert sind, ist das Risiko minimal — der Fix übernimmt nur die bestehende Konvention.

## Code-Skizze (optional)

```csharp
[McpServerTool(Name = "list_children"), Description("""
    Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn
    parentSlug weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

    Edge Cases:
    - parentSlug = null oder weggelassen: listet die Root-Dokumente
    - parentSlug = "" (leerer String): leere Liste, kein Fehler (semantisch identisch zu einem unbekannten Slug)   // ← GEÄNDERT
    - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
    - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server akzeptiert
      und liefert eine leere Liste

    Beispiel:
    - list_children() → DocumentSummary[] der Root-Dokumente
    - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"
    - list_children(parentSlug="gibt-es-nicht") → []

    Es gibt keine Cap; bei sehr breiten Verzeichnissen sind ggf. >100 Treffer möglich.
    """)]
```

## Notes

- **Reihenfolge der Bullets bleibt:** nur Bullet 2 (leerer String) ändert sich inhaltlich; die anderen drei Bullets (`null/weggelassen`, `existiert nicht`, `ungültiger Slug`) bleiben Wort für Wort wie im Original-Step-Commit `5346f25`. Das hält den Diff minimal und macht den Fix für den Auditer trivial nachvollziehbar.
- **Warum kein Code-Logik-Fix (Audit-Alternative):** der Auditer hat in Finding 1 explizit zwei Optionen erwogen — Doku-Fix (gewählt hier) oder Validierung in `SqlDocumentsStore.ListChildrenAsync`. Doku-Fix, weil (a) das aktuelle Verhalten sicher ist (Empty-Result statt Crash), (b) konsistent mit den anderen "unbekannter Slug"-Edge-Cases, (c) Step-Scope nicht erweitert wird (der Plan sagte explizit „keine Code-Logik-Änderungen"). Falls das Team das Verhalten später härten will, kann das in einem separaten Folge-Step erfolgen — der Doku-Bullet erwähnt diese Option explizit als „falls künftig Empty-String werfen soll".
- **Konsistenz mit Original-Step-Subject-Praxis:** der Original-Step `5346f25` hatte einen 74-Zeichen-Subject (NITPICK im Review, aber Plan-DoD-konform und mit Repo-Präzedenz). Der hier vorgeschlagene 60-Zeichen-Subject ist deutlich unter dem 70-Zeichen-Limit von `03-git-workflow.mdc` — kein MINOR-Verdacht, kein Repo-Präzedenz nötig.
- **Quell-Doku-Umfang:** die ausführlichere Formulierung in Datei 2 ist bewusst länger als die knappe Description in Datei 1 — das ist genau der Sinn der `Quell-Doku`-Sektion: ausführliche Architektur-Begründung vs. knappe LLM-Sicht. Der Auditer hat in Finding 2 explizit eine „ausführliche Variante" empfohlen („weil die `Quell-Doku` per Konzept-Vorgabe die *Begründungen* liefert, nicht nur die knappe Aussage").
- **Was dieser Fix NICHT macht:** er ändert weder `SqlDocumentsStore.cs` noch `FrontMatterParser.cs` noch sonstige Codedateien. Er ist ein reiner Doku-/Description-Text-Fix, kein Code-Logik-Fix.

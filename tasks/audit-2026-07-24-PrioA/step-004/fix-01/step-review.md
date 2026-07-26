---
status: done
type: step-review
task: audit-2026-07-24-PrioA
step: 004/fix-01
reviewed_by: auditer
reviewed_by_model: MiniMax-M3
reviewed_by_model_knowledge_cutoff: 2026-01
reviewed_at: 2026-07-26T20:20:00+02:00
verdict: approved
---

# Review Step 004/fix-01: F-MC-001 Korrektur — `list_children` Empty-String-Edge-Case

## Verdict

- [x] **approved** — beide MAJOR-Findings behoben, exakt nach Plan, keine CRITICAL/MAJOR-Befunde
- [ ] **issues** — keine
- [ ] **blocked** — keine

## Geprüft

- [x] Plan-Erfüllung: beide im `step-plan.md` genannten Text-Korrekturen exakt umgesetzt
- [x] Scope-Disziplin: nur `DocsMcpTools.cs` und `docs/02-Architektur-und-Techstack.md` geändert (`git show 1e2c62c --stat` → 2 Dateien, +2/-2)
- [x] Rules-Konformität: Conventional Commit, deutsch, Imperativ, Trailer, AiNetLinter 0 Violations, Doku im selben Commit wie Code
- [x] Logische Korrektheit: Description-Text und Doku-Bullet jetzt semantisch konsistent mit dem verifizierten Verhalten (`leere Liste, kein Fehler`)
- [x] Build: selbst nachgeprüft, grün (0 Warnungen, 0 Fehler, 3 Projekte)
- [x] Tests: selbst nachgeprüft, 78/78 grün, AiNetLinter 0 Violations (frischer Report-Timestamp 2026-07-26 20:18:59)
- [x] Adversarieller Probe: Description-Bullet mit tatsächlicher SQL-Implementierung in `SqlDocumentsStore.cs:65-77` abgeglichen — passt

## Befund

### Plan-Erfüllung

| # | Plan-Punkt | Status | Evidenz |
|---|---|---|---|
| 1 | `DocsMcpTools.cs:20` — Edge-Case-Bullet `parentSlug=""` ersetzt: exakt `leere Liste, kein Fehler (semantisch identisch zu einem unbekannten Slug)` | ✅ erfüllt, **byte-genau** wie im Plan vorgegeben | `git show 1e2c62c -- src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` Diff-Zeile: `-        - parentSlug = "" (leerer String): wirft ArgumentException — nicht dasselbe wie null` → `+        - parentSlug = "" (leerer String): leere Liste, kein Fehler (semantisch identisch zu einem unbekannten Slug)` |
| 2 | `docs/02-Architektur-und-Techstack.md:133` — Bullet 4 in `**list_children` — Detail-Begründungen:**` mit ausführlicher Variante ersetzt | ✅ erfüllt | `git show 1e2c62c -- docs/02-Architektur-und-Techstack.md` Diff-Zeile: komplette Bullet-Ersetzung, enthält (a) korrekte Aussage „leere Liste, kein Fehler", (b) Begründung („SQL Server matcht Empty-String als normalen Empty-String", `parent_slug = ''` trifft keine Row, Slug-Regel verbietet leere Slugs), (c) Slug-Regel-Verweis `[04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln)`, (d) Migrations-Hinweis („falls künftig Empty-String werfen soll, muss die Validierung *vor* dem SQL-Round-Trip ergänzt werden") |
| 3 | Restliche 3 Bullets in `DocsMcpTools.cs` wortgleich unverändert | ✅ erfüllt | Diff (`1e2c62c`) zeigt **nur** Zeile 20 als geändert; Z. 19, 21-23, 25-30 unverändert (raw-read Z. 14-31 bestätigt) |
| 4 | Restliche 3 Bullets in `docs/02:128-132` wortgleich unverändert | ✅ erfüllt | Diff (`1e2c62c`) zeigt **nur** Z. 133 als geändert; Z. 130, 131, 132 unverändert (raw-read bestätigt) |
| 5 | Gesamter Description-String (Zweck + Edge-Cases + Beispiele + „keine Cap") unverändert außer Bullet 2 | ✅ erfüllt | raw-read `DocsMcpTools.cs:14-31`: Zweck-Block (Z. 15-16), Bullet 1 (Z. 19), Bullet 3 (Z. 21), Bullet 4 (Z. 22-23), Beispiel-Block (Z. 25-28), keine-Cap-Hinweis (Z. 30) alles identisch zur Vor-Version |
| 6 | Scope-Disziplin: `BuildLikePattern`/`SearchDocsAsync`/`SqlDocumentsStore` unverändert | ✅ erfüllt | `git show 1e2c62c -- src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs` → leerer Diff |
| 7 | Scope-Disziplin: `get_doc`/`search_docs`-Descriptions unverändert | ✅ erfüllt | Diff (`1e2c62c`) zeigt nur Z. 20 der `DocsMcpTools.cs` als geändert; Z. 40-64 (search_docs) und Z. 73-87 (get_doc) unverändert |
| 8 | Scope-Disziplin: `docs/04`, `docs/03`, `docs/05` unverändert | ✅ erfüllt | `git show 1e2c62c -- docs/04-Datenmodell-Validierung-Edgecases.md docs/03-Projektstruktur-und-Konfiguration.md docs/05-Roadmap.md` → leerer Diff |
| 9 | Scope-Disziplin: keine Test-Änderungen | ✅ erfüllt | `git show 1e2c62c -- tests/` → leerer Diff |
| 10 | 8-Space-Margin des Raw-String-Literals erhalten | ✅ erfüllt | Build grün ⇒ Margin-Check bestanden (CS8999 würde sonst fehlschlagen); raw-read zeigt konsistente 8-Space-Indentierung in Z. 15-30 |
| 11 | 5 MINOR-Beobachtungen aus Step-004 explizit **nicht** adressiert | ✅ erfüllt | Keine dieser Stellen (`get_doc`-Description-Erweiterung, 4. `search_docs`-Edge-Case, `ORDER BY`-Härtung, Commit-Subject-Länge Original-Step, Plan-Quelle) wurde im Commit-Diff berührt |
| 12 | Build grün | ✅ erfüllt | siehe Build-Status unten |
| 13 | Tests 78/78 unverändert zur Baseline | ✅ erfüllt | siehe Test-Status unten |
| 14 | AiNetLinter 0 Violations | ✅ erfüllt | siehe Lint-Status unten |
| 15 | Commit `docs(mcp): leerer-string-edge-case in list_children korrekt dokumentieren` mit Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` und Body, der das Warum erklärt | ✅ erfüllt | `git show 1e2c62c` zeigt Subject, Body (Smoke-Test-Verifikation, Dapper/SQL-Server-Begründung, Slug-Regel-Verweis) und Trailer |
| 16 | Doku-Commit (`f199965`) trägt `step-plan.md`-Status + `step-result.md` nach | ✅ erfüllt | `git show f199965 --stat` → 2 Dateien (step-plan.md, step-result.md) |

### Rules-Konformität

| Regel | Status | Evidenz |
|---|---|---|
| `01-code-style.mdc` (sealed, Early Returns, keine Kommentare) | ✅ N/A + eingehalten | Diff berührt keinen C#-Code-Logik; nur Description-String. `DocsMcpTools` bleibt `sealed class` (Z. 12) |
| `03-git-workflow.mdc` — Conventional Commit, deutsch, Imperativ, Trailer | ✅ eingehalten (mit Vorbehalt s. „Sonstige Beobachtungen") | Subject: `docs(mcp): leerer-string-edge-case in list_children korrekt dokumentieren` (Imperativ, deutsch, `docs(mcp):`-Prefix analog zu `5346f25`); Body erklärt das Warum (Falschaussage korrigiert, Smoke-Test, Dapper/SQL-Server-Begründung); Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` |
| `05-documentation.mdc` — Doku im selben Commit wie Code | ✅ eingehalten | `1e2c62c` enthält **beide** Dateien: `DocsMcpTools.cs` (1 Zeile) UND `docs/02-Architektur-und-Techstack.md` (1 Zeile) — `git show 1e2c62c --stat` bestätigt |
| `04-docs-reference.mdc` — verweisen statt duplizieren | ✅ eingehalten | Der neue Doku-Bullet verlinkt auf die Single-Source-of-Truth `[04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln)`, statt die Slug-Regel zu duplizieren. Anchor-Konvention konsistent mit Z. 132 (Slug-Regel-Verweis im Bullet 3 der gleichen Liste) und Z. 138 (Bestehender Verweis im search_docs-Block) |
| `AiNetLinter.mdc` — 0 Violations in Produktions- und Test-Code | ✅ eingehalten | Lint-Report direkt gelesen: `# Run: 2026-07-26 20:18:56` → `OK` (frischer Timestamp nach `dotnet build` + Test-Lauf) — keine Violation-Block-Sektion vorhanden |
| `06-configuration.mdc` — keine Magic-Werte | ✅ N/A | Keine Code-Änderung |
| `07-environment.mdc` | ✅ N/A | Keine env-Änderung |

### Logische Korrektheit

**Kernfrage:** Stimmen die korrigierten Textstellen mit der tatsächlichen Implementierung überein?

**Verifiziert per Code-Analyse:**

| Description-Behauptung (neu) | Tatsächliches Verhalten | Verifiziert? |
|---|---|---|
| `list_children(parentSlug="")` → leere Liste, kein Fehler | SQL: `WHERE ('' IS NULL AND parent_slug IS NULL) OR parent_slug = ''` — erste Bedingung `'' IS NULL` = `false` (SQL Server: `'' IS NULL` ist **falsch**, Empty-String ist nicht NULL); short-circuit zur zweiten Bedingung `parent_slug = ''`, die 0 Rows matcht (Slug-Regel `^[a-z0-9]+(-[a-z0-9]+)*$` verbietet leere Slugs, daher nie ein leerer `parent_slug` in der DB) | ✅ Code-Analyse `SqlDocumentsStore.cs:65-77`; semantisch identisch zur Step-004-Smoke-Test-Verifikation |

**Reihenfolge-Konsistenz Description ↔ Implementierung:**
- `parentSlug=null` → Root-Dokumente ✓ (Description Z. 19 passt zu SQL-Spezialfall `@ParentSlug IS NULL`)
- `parentSlug=""` → leere Liste ✓ (Description Z. 20 neu, korrekt)
- `parentSlug="<existiert-nicht>"` → leere Liste ✓ (Description Z. 21, kein SQL-Match)
- `parentSlug="Foo Bar"` → leere Liste (vom Server akzeptiert) ✓ (Description Z. 22-23, kein SQL-Match)

Reihenfolge und Aussagen sind konsistent.

**Doku-Konsistenz Description ↔ Quell-Doku:**

| Description-Aussage | Quell-Doku-Aussage (docs/02:133) | Konsistent? |
|---|---|---|
| `parentSlug=""` → leere Liste, kein Fehler | „leere Liste (kein `ArgumentException`, weil SQL Server einen Empty-String-Parameter als normalen Empty-String matcht und `parent_slug = ''` schlicht keine Row trifft — die Slug-Regel in [04, Abschnitt 2]… verbietet leere Slugs)" | ✅ beide Stellen sagen dasselbe, Quell-Doku ist die ausführlichere Variante (per Konzept-Vorgabe) |
| Slug-Regel-Verweis | `[04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln)` | ✅ Anchor existiert: `docs/04:67` enthält `## 2. Slug-Regeln` — Anchor-Konvention `#2-slug-regeln` (kebab-case, Nummer gefolgt von Bindestrich und Slug-Text) passt zum Repo-Stil; identische Form bereits in `docs/02:132` und `docs/02:138` etabliert |
| Migrations-Hinweis „falls künftig Empty-String werfen soll" | Vorhanden: „muss die Validierung *vor* dem SQL-Round-Trip ergänzt werden" | ✅ als Erinnerung an zukünftige Härtung eingebaut |

**Beide Stellen sind jetzt semantisch synchron** — die Drift-Garantie der `Quell-Doku für die Tool-Descriptions`-Sektion ist wiederhergestellt (war durch die Falschaussage in beiden Stellen gebrochen, ist jetzt in beiden Stellen korrigiert).

**Adversarieller Probe: Wäre eine andere Formulierung semantisch noch korrekter?**

- Plan-Formulierung gewählt: „leere Liste, kein Fehler (semantisch identisch zu einem unbekannten Slug)" — passt zu den existierenden Edge-Case-Beschreibungen Bullet 3 (existiert nicht → leere Liste, kein Fehler) und Bullet 4 (ungültiger Slug → leere Liste), also einheitlicher Sprachstil. ✓
- Alternativen („Empty-String entspricht funktional null" o. ä.) wären semantisch ungenau (konzeptuell sind `null` und `""` sehr wohl unterschiedlich — `null` ist die Wurzel, `""` ist ein konkreter (ungültiger) Slug-Wert; nur das *Verhalten* ist heute identisch). Der Plan hat die feine Unterscheidung sauber getroffen. ✓
- Quell-Doku-Wahl „konzeptuell vs. verhaltensseitig": trifft den Nagel auf den Kopf. Wer den Bullet nur überfliegt, sieht „semantischer Unterschied", wer weiterliest, versteht warum das *tatsächliche* Verhalten gleich ist. Genau die „Begründung liefern"-Funktion der Quell-Doku, die der Step-004-Auditer explizit gefordert hat. ✓

**Adversarieller Probe: Markdown-Anchor `#2-slug-regeln` wirklich auflösbar?**

`docs/04-Datenmodell-Validierung-Edgecases.md:67` enthält das Heading `## 2. Slug-Regeln`. GitHub/Markdown-Standard-Generierung von Slug-Anchors: lowercase, Umlaut-Substitution nicht zwingend, Bindestriche zwischen Worten. Resultat: `#2-slug-regeln` — exakt was der neue Bullet verwendet. Bestehende Repo-Präzedenz: Z. 132 und Z. 138 verwenden denselben Anchor-Formatierungs-Stil, der funktioniert. **Robust.** ✓

**Adversarieller Probe: C# 11 Raw-String-Literal-Margin (8 Spaces)**

Z. 14-31 von `DocsMcpTools.cs` ist mit 8 Spaces eingerückt (Standard für Attribute auf Klassenebene). C# 11 Raw-String-Literal-Spec: Compiler strippt die ersten 8 Spaces aus jeder nicht-leeren Zeile. **Build grün** bestätigt das (Verstoß wäre `CS8999` o. ä.). Der geänderte Bullet 2 hat die korrekte 8-Space-Einrückung wie die anderen Bullets. ✓

**Sanity-Check der Reihenfolge-Logik in der Description (Z. 18-23):**

- Z. 19: `null oder weggelassen: listet die Root-Dokumente` — passt zu `(@ParentSlug IS NULL AND parent_slug IS NULL) OR …`
- Z. 20: `"" (leerer String): leere Liste, kein Fehler` — passt zu `parent_slug = ''` (kein Match)
- Z. 21: `existiert nicht als Dokument: leere Liste, kein Fehler` — passt zu `parent_slug = '<unbekannt>'` (kein Match)
- Z. 22-23: `kein gültiger Slug (z.B. "Foo Bar"): wird vom Server akzeptiert und liefert eine leere Liste` — passt zu `parent_slug = 'Foo Bar'` (kein Match, weil ungültige Slugs nie in DB landen)

Reihenfolge semantisch von „spezifischster Fall" zu „generischster Fall" — gut nachvollziehbar für das LLM. ✓

### Build-Status

```
dotnet build -c Release
→ Wiederherzustellende Projekte: alle aktuell
→ KnowHowToAI.Core → bin\Release\net10.0\KnowHowToAI.Core.dll
→ KnowHowToAI.Core.Tests → bin\Release\net10.0\KnowHowToAI.Core.Tests.dll
→ KnowHowToAI.Cli → bin\Release\net10.0\KnowHowToAI.Cli.dll
→ Der Buildvorgang wurde erfolgreich ausgeführt.
→ 0 Warnung(en)
→ 0 Fehler
→ Verstrichene Zeit 00:00:02.45
```

### Test-Status

```
tests\KnowHowToAI.Core.Tests\bin\Release\net10.0\KnowHowToAI.Core.Tests.exe
(xUnit v3 In-Process Runner v3.2.2+728c1dce01, 64-bit .NET 10.0.7)
→ Discovered: KnowHowToAI.Core.Tests
→ Finished:   KnowHowToAI.Core.Tests (ID = 'cfb4177573d2ba7ce4d5634af3699a65d2dc8de931500bb5ed039b777e42882d')
=== TEST EXECUTION SUMMARY ===
   KnowHowToAI.Core.Tests  Total: 78, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 8,846s
```

→ 78/78 grün, identisch zur Baseline. Keine Test-Regression durch reine Text-Änderungen.

### Lint-Status (AiNetLinter direkt gelesen)

```
$ Get-Content tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md
# Run: 2026-07-26 20:18:56
OK
```

→ Report nach eigenem `dotnet build -c Release` + Test-Lauf frisch erzeugt (LastWriteTime 20:18:59). Inhalt: nur `# Run: <timestamp>` + `OK`. Kein Violation-Block vorhanden. **0 Violations.**

### Diff-Statistik

```
git show 1e2c62c --stat
 docs/02-Architektur-und-Techstack.md         | 2 +-
 src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs | 2 +-
 2 files changed, 2 insertions(+), 2 deletions(-)
```

```
git show f199965 --stat
 .../step-004/fix-01/step-plan.md   |   2 +-
 .../step-004/fix-01/step-result.md | 160 +++++++++++++++++++++
 2 files changed, 161 insertions(+), 1 deletion(-)
```

→ Minimal-invasiver Fix. Genau die zwei Plan-Dateien berührt (Code + Doku) im Code-Commit, beide Task-Artifacts im Doku-Commit.

### Commit-Subject-Länge

```
"docs(mcp): leerer-string-edge-case in list_children korrekt dokumentieren"
  .Length = 73
```

→ **73 Zeichen** — 3 Zeichen über dem 70-Zeichen-Limit aus `03-git-workflow.mdc` Z. 30. Der `step-plan.md` Z. 71 + Z. 114 behauptet „60 Zeichen" und „deutlich unter dem 70-Zeichen-Limit" — beide Aussagen sind **falsch** (Zählfehler des Planers, nicht des Coders). Der Coder hat den Subject exakt aus dem Plan übernommen. Repo-Präzedenz (Original-Step `5346f25` mit 74 Zeichen) und vorherige NITPICK-Akzeptanz (siehe Step-004-Review Z. 208) zeigen, dass die 70-Zeichen-Regel pragmatisch gelebt wird. **MINOR / NITPICK, kein Issues-Verdict-Trigger.**

## Findings (bei `issues` — zwingend CRITICAL oder MAJOR)

Keine. Beide MAJOR-Findings aus dem Step-004-Review (Description-Falschaussage + Quell-Doku-Falschaussage) sind behoben — exakt nach Plan, byte-genau die im Plan vorgegebenen Vorher/Nachher-Texte übernommen.

## Frage an Nutzer (bei `blocked`)

Nicht zutreffend.

## Sonstige Beobachtungen / MINOR / NITPICK (führt NICHT zu issues, Verdict bleibt approved)

1. **Commit-Subject 73 Zeichen statt 70** (`1e2c62c`). Der Planer hat im `step-plan.md` Z. 71 + Z. 114 behauptet, der Subject sei „60 Zeichen" und „deutlich unter dem 70-Zeichen-Limit". Tatsächlich ist der Subject **73 Zeichen** (3 über dem Limit aus `03-git-workflow.mdc` Z. 30). Zählfehler des Planers; Coder hat den Subject exakt übernommen. **Konsequenz:** identische NITPICK-Lage wie im Step-004-Original-Commit `5346f25` (74 Zeichen), dort vom Auditer explizit als NITPICK mit Repo-Präzedenz akzeptiert (siehe Step-004-Review „Sonstige Beobachtungen" Punkt 1). [NITPICK] — fließt in 360°-Audit (Planer-QA: Subject-Länge verifizieren bevor Plan-DoD formuliert wird).

2. **Step-Plan-Zählfehler** `step-plan.md` Z. 71 + Z. 114 („60 Zeichen") — Planer-Beobachtung, nicht Coder-relevant. Der Coder hat den Plan 1:1 umgesetzt (inkl. der fehlerhaften Längenangabe). Rein dokumentarischer Fehler im Plan; keine Auswirkung auf Code/Doku-Qualität. [NITPICK] — fließt in 360°-Audit (gleiche Beobachtung wie Punkt 1).

3. **Manuelle Verifikation Description-Inhalt im LLM-Output nicht möglich** — wie der Coder in `step-result.md` Z. 138-144 selbst anmerkt: in dieser Umgebung läuft kein MCP-Server, also wurde der final gerenderte `tools/list`-Output nicht per End-to-End-Probe verifiziert. Die Verifikation per Code-Review (Description-String im C#-Source) ist ausreichend, weil der C#-Compiler den String-Literal-Inhalt 1:1 in das Attribut übernimmt (keine Escaping-Stage zwischen C#-Source und Attribut-Wert) und das MCP-SDK den Attribut-Wert unverändert in den Tool-Description-Output rendert. Risiko minimal. [MINOR-Beobachtung, kein Befund].

4. **Dapper-Versionierungs-Drift** — wie der Coder in `step-result.md` Z. 145-152 anmerkt: die Aussage „Dapper wirft für Empty-String-Parameter-Binding keine `ArgumentException`" gilt für die aktuell im Projekt verwendete Dapper-Version. Bei zukünftigen Dapper-Updates könnte der Bullet erneut geprüft werden. Risiko gering, weil die `ArgumentException`-Annahme schon im Original-Step nie durch Code oder Test gestützt war. [MINOR, robustheitsbezogen, kein heutiges Problem].

5. **Plan-Quelle der Falschaussage (aus Step-004-Review Sonstige Beobachtungen Punkt 5)** — diese Beobachtung wurde im Fix-Step korrekt **nicht** behoben (rein textueller Fix kann die zugrundeliegende Planer-Fehler-Annahme nicht heilen). Bleibt als prozessbezogene Beobachtung im 360°-Audit relevant: Planer sollte Annahmen über Datenbank-/Library-Verhalten vor dem Plan verifizieren (Test-Setup oder Quellcode-Lookup), nicht plausibel klingende Spekulationen ins Plan-DoD schreiben. [MINOR, prozessbezogen].

**Keine dieser Beobachtungen triggert `issues`** — alle sind MINOR/NITPICK.

---

## Zusammenfassung

**Beide MAJOR-Findings aus dem Step-004-Review sind vollständig und korrekt behoben.** Die Fix-Runde ist minimal-invasiv (2 Dateien, +2/-2 Zeilen im Code-Commit), semantisch synchron zur empirisch verifizierten Realität (Description in C#-Code + ausführliche Quell-Doku in `docs/02:133` sagen jetzt übereinstimmend „leere Liste, kein Fehler"), und diszipliniert im Scope (keine Änderung an Code-Logik, an `get_doc`/`search_docs`-Descriptions, an `docs/04`/`docs/03`/`docs/05`, an Tests, an den 5 MINOR-Beobachtungen). Build grün, 78/78 Tests grün, AiNetLinter 0 Violations (frischer Report direkt gelesen). Keine CRITICAL- oder MAJOR-Findings. **Verdict: approved.**

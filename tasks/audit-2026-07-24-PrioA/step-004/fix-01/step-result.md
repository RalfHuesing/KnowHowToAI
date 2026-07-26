---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 004/fix-01
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T20:15:00+02:00
code_commit_hash: 1e2c62cefb93929e8db27f3034edd1fa39b2d2be
# Hinweis: den Commit, der DIESE Datei enthält (Coder-Skill Schritt 7),
# kann diese Datei denknotwendig nicht selbst zitieren — bei Bedarf per
# `git log --follow -- <Pfad-dieser-Datei>` nachschlagen.
status_after: done
---

# Result Step 004/fix-01: F-MC-001 Korrektur — `list_children` Empty-String-Edge-Case

## Zusammenfassung

Beide MAJOR-Findings aus dem Step-004-Review adressiert durch reine
Text-Korrekturen — kein Code-Logik-Impact. In `DocsMcpTools.cs:20` wurde
der `list_children`-Description-Bullet für `parentSlug = ""` an die
empirisch verifizierte Realität angepasst (leere Liste, kein Fehler).
In `docs/02-Architektur-und-Techstack.md:133` wurde der korrespondierende
Bullet in der `Quell-Doku für die Tool-Descriptions` ausführlich
korrigiert — mit Begründung (Dapper + SQL Server werfen für
Empty-String-Binding keine `ArgumentException`), Verweis auf die Slug-Regel
in [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln)
und Migrations-Hinweis für eine künftige Härtung.

## Geänderte Dateien

- `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` — Bullet 2 der
  Edge-Cases-Liste in der `list_children`-Description geändert: von
  "wirft ArgumentException — nicht dasselbe wie null" auf "leere Liste,
  kein Fehler (semantisch identisch zu einem unbekannten Slug)". Restliche
  3 Bullets und der gesamte Description-String unverändert.
- `docs/02-Architektur-und-Techstack.md` — Bullet 4 in
  `**list_children` — Detail-Begründungen:**` ersetzt. Statt der alten
  `ArgumentException`-Behauptung jetzt: ausführliche Begründung (kein
  `ArgumentException`, weil SQL Server Empty-String als normalen
  Empty-String matched und `parent_slug = ''` keine Row trifft),
  Slug-Regel-Verweis auf [04, Abschnitt 2], und Hinweis, dass eine
  künftige Härtung explizite Validierung *vor* dem SQL-Round-Trip
  bräuchte. Andere Bullets in der Liste unverändert.

## Commit

- **Code-Commit-Hash:** `1e2c62cefb93929e8db27f3034edd1fa39b2d2be`
- **Message:**
  ```
  docs(mcp): leerer-string-edge-case in list_children korrekt dokumentieren

  Beide Stellen (Description in DocsMcpTools.cs:20 und Quell-Doku in
  docs/02-Architektur-und-Techstack.md:133) behaupteten fälschlich, dass
  list_children(parentSlug="") eine ArgumentException wirft. Smoke-Test
  gegen localhost\MSSQLSERVER2022 hat empirisch gezeigt: leere Liste,
  kein Fehler (semantisch identisch zu unbekanntem Slug). Dapper und
  SQL Server werfen für Empty-String-Parameter-Binding keine
  ArgumentException. Beide Textstellen jetzt synchron zur verifizierten
  Realität. Quell-Doku zusätzlich mit Slug-Regel-Verweis auf
  [04, Abschnitt 2] und Migrations-Hinweis für künftige Härtung.

  Refs: tasks/audit-2026-07-24-PrioA/step-004/fix-01
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```
- **Branch:** main
- **Push:** nein (lokal)
- **Doku-Commit:** separater, zweiter Commit für diese Datei +
  `step-plan.md`-Status (siehe Coder-Skill Schritt 7) — dessen Hash steht
  nicht hier drin (Selbstbezug), sondern in `git log`.

## Build-Output

```
dotnet build -c Release
→ Ergebnis: grün — 0 Warnungen, 0 Fehler
→ 3 Projekte (Core, Cli, Tests) erfolgreich gebaut in ~4.8s
```

## Test-Output

```
tests\KnowHowToAI.Core.Tests\bin\Release\net10.0\KnowHowToAI.Core.Tests.exe
(xUnit v3 In-Process Runner v3.2.2 — direkte Exe-Ausführung wegen MTP-v2-Konvention)
→ Ergebnis: grün — Total: 78, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
→ Dauer: 10,225s
→ Anzahl Tests: 78, davon grün: 78, baseline-Ausnahmen: 0
→ AiNetLinter-Test inkludiert, 0 Violations
```

AiNetLinter-Report (`tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md`)
direkt gelesen:

```
# Run: 2026-07-26 20:14:17
OK
```

→ 0 Violations, Report-Timestamp nach unseren Edits (Test-Lauf erfolgte
nach `dotnet build` und nach den beiden Datei-Änderungen).

## Abweichungen vom Plan

Keine — Plan 1:1 umgesetzt. Beide Edits exakt nach den im Plan
angegebenen Vorher/Nachher-Texten; Reihenfolge und Inhalt der übrigen
Bullets unverändert; 8-Space-Margin des Raw-String-Literals in
`DocsMcpTools.cs` erhalten (Build grün, sonst CS8999-Fehler).

## Beobachtungen

- **Raw-String-Literal-Margin erhalten:** der 8-Space-Margin in der
  `[Description("""..."""")]` wurde exakt beibehalten; bei nur einem
  abweichenden Space hätte der Compiler einen CS8999-Fehler geworfen.
  Saubere Verifikation: Build grün, Description-String visuell
  unverändert außer im gewünschten Bullet.
- **Beide Stellen jetzt semantisch synchron:** der Plan hebt zu Recht
  hervor, dass die `Quell-Doku`-Sektion als Single-Source-of-Truth
  deklariert ist. Eine LLM-sichtbare Description-Korrektur ohne
  korrespondierende Doku-Korrektur hätte genau die Drift erzeugt, die
  Step 004 verhindern wollte. Der gemeinsame Commit macht die
  Synchronisierung im Git-Log sichtbar.
- **Plan-Subject-Länge 60 Zeichen:** deutlich unter dem 70-Zeichen-Limit
  aus `03-git-workflow.mdc`, kein MINOR-Verdacht. Subject passt zur
  `docs(mcp):`-Konvention (analog zum Original-Step `5346f25`).
- **Kein automatisierter Test geschrieben** — wie im Plan vorgesehen.
  Die beiden Textstellen sind keine testbaren Verhaltensanker (der
  Code ist unverändert). Test-Baseline 78/78 bleibt per Definition
  unverändert, was durch den Test-Lauf bestätigt wurde.
- **Markdown-Anchor `#2-slug-regeln`** ist nicht durch Build/Lint
  prüfbar. Section existiert in `docs/04` Z. 67 als `## 2. Slug-Regeln`
  → Anchor-Konvention mit Umlaut-Ersatz im Slug passt zum Repo-Stil
  (siehe Original-Step-Result, Beobachtungen). Risiko minimal, da der
  Verweis analog zu bestehenden Verweisen in `docs/02` Z. 132 und Z. 138
  aufgebaut ist, die bereits funktionieren.

## Bekannte Unschärfen

- **Manuelle Verifikation Description-Text:** der Auditer sollte die
  `tools/list`-Antwort eines laufenden MCP-Servers inspizieren, um zu
  bestätigen, dass die Description im gerenderten LLM-Output exakt
  dem neuen Bullet entspricht. In dieser Umgebung ohne laufenden
  MCP-Server nur per Code-Review verifiziert.
- **Dapper-Versionierungs-Drift:** die Doku-Aussage "Dapper wirft für
  Empty-String-Binding keine `ArgumentException`" gilt für die aktuell
  im Projekt verwendete Dapper-Version. Falls Dapper in einer zukünftigen
  Version sein Binding-Verhalten ändert, müsste der Bullet erneut
  geprüft werden. Aktuell ist das Risiko gering, da die
  `ArgumentException`-Annahme schon im Original-Step nie durch Code oder
  Test gestützt war — sie war von Anfang an eine Fehl-Annahme des
  Planers.
- **Smoke-Test-Wiederholbarkeit:** der empirische Test aus dem
  Step-004-Review (Z. 114-134) wurde nicht erneut durchgeführt — der
  Code-Pfad ist unverändert, die Edits betreffen nur Textstellen. Die
  ursprüngliche Verifikation bleibt gültig.

## Falls Status `blocked`

Nicht zutreffend.

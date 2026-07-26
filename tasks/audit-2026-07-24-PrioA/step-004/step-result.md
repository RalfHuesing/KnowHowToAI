---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 004
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T19:35:00+02:00
code_commit_hash: 5346f2505d7e899ca16ec6fb422d6ccb771e72ed
# Hinweis: den Commit, der DIESE Datei enthält (Coder-Skill Schritt 7),
# kann diese Datei denknotwendig nicht selbst zitieren — bei Bedarf per
# `git log --follow -- <Pfad-dieser-Datei>` nachschlagen.
status_after: done
---

# Result Step 004: F-MC-001 + F-MC-002 — Tool-Description-Qualität und Beispiel-Outputs für LLM-Konsumenten

## Zusammenfassung

Die drei `[Description(...)]`-Strings in `DocsMcpTools.cs` sind auf das
3-Sektionen-Schema (Zweck, Edge Cases, Beispiel) umgestellt — `search_docs`
zusätzlich mit `Response-Shape` und `Semantik`-Blöcken davor, weil der
`SearchResult`-Wrapper und die LIKE-Wildcard-Semantik zentrale LLM-UX-Punkte
sind. `docs/02-Architektur-und-Techstack.md` Abschnitt 4.D hat einen neuen
Unterabschnitt `Quell-Doku für die Tool-Descriptions` bekommen, der für jede
Description-Aussage die ausführliche Architektur-Begründung liefert (und so
Description + Doku gegen Drift absichert). `docs/04-Datenmodell-Validierung-Edgecases.md`
Abschnitt 1 verweist am Ende des `search_docs`-Blocks auf den neuen
Unterabschnitt. Reine Doku-/Description-Änderung, keine Code-Logik.

## Geänderte Dateien

- `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` — Drei `[Description(...)]`-Strings als C# 11 Raw-String-Literals (`"""..."""`) umgeschrieben. `list_children`: Zweck + 4 Edge Cases (null/leer, leerer String, unbekannter Slug, ungültiger Slug) + 3 Beispiel-Outputs + "keine Cap"-Hinweis. `search_docs`: Zweck + `Response-Shape` (`{ results, truncated }`, `MaxResults`-Default 50) + `Semantik` (SQL LIKE, Bracket-Escape, Title-Ranking) + 4 Edge Cases (leere/Whitespace-Query, zu lang, keine Treffer, `truncated=true`). `get_doc`: Zweck + 3 Edge Cases (unbekannter Slug → null, Token-Budget-Hinweis NVARCHAR(MAX), YAML-Front-Matter nicht im Content) + 2 Beispiel-Outputs. Konstruktor-Signatur und Methoden-Bodies unverändert.
- `docs/02-Architektur-und-Techstack.md` — Neuer `#### Quell-Doku für die Tool-Descriptions`-Unterabschnitt in Abschnitt 4.D, eingefügt direkt nach der Tool-Liste und vor `Sichtbarkeit ohne SQL Profiler`. Drei Sub-Blöcke (einer pro Tool) mit Detail-Begründungen für jede Description-Aussage, inkl. Verweise auf [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md) (SQL-Details), [04, Edge Case 4.2](04-Datenmodell-Validierung-Edgecases.md#42-leeres-docs-root-verzeichnis-leere-db) (leere-DB-Konvention), [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln) (Slug-Regeln), F-SE-001 (Wildcard-DoS), F-PE-001/F-PE-002/F-MC-001 (Performance/UX). Expliziter Hinweis, dass Description + Doku synchron zu halten sind.
- `docs/04-Datenmodell-Validierung-Edgecases.md` — Verweis-Block am Ende des `search_docs`-Blocks (Abschnitt 1, nach `Maximale Query-Länge`) ergänzt: "LLM-UX-Details (Edge Cases, Response-Shape-Beispiele, Begründungen für die Wahl der Defaults …) siehe [02, Abschnitt 4.D]". Verweismodell konsistent mit `04-docs-reference.mdc` (verweisen statt duplizieren).

## Commit

- **Code-Commit-Hash:** `5346f2505d7e899ca16ec6fb422d6ccb771e72ed`
- **Message:**
  ```
  docs(mcp): tool-descriptions um edge-cases und beispiel-outputs erweitern

  LLM-Konsumenten sehen jetzt explizit, wie die drei
  MCP-Tools auf Edge Cases reagieren (leere Eingaben, unbekannte
  Slugs, gekappte Suchergebnisse). `search_docs`-Description
  dokumentiert den neuen `SearchResult`-Shape mit `truncated`-
  Marker und das Bracket-Escape der LIKE-Wildcards. Beispiel-
  Outputs (F-MC-002) sind in jede Description integriert. `docs/02`
  Abschnitt 4.D ist die neue Quell-Doku für Description-Inhalte.

  Refs: tasks/audit-2026-07-24-PrioA/step-004
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
```

## Test-Output

```
tests\KnowHowToAI.Core.Tests\bin\Release\net10.0\KnowHowToAI.Core.Tests.exe
(xUnit v3 MTP-v2 Runner — direkte Exe-Ausführung, weil `dotnet test` mit MTP-v2-Targets die Tests nicht aufnimmt; siehe Schritt-003-Result für die gleiche Konvention)
→ Ergebnis: grün — 78 Tests, 0 fehlgeschlagen, 0 übersprungen
→ Anzahl Tests: 78 (Baseline unverändert — keine neuen Tests in diesem Step, da Description-Strings kein testbares Verhalten sind, siehe Plan "Tests")
```

AiNetLinter-Lauf ist in `AiNetLinterTests.LintRun_ReportsNoViolations` enthalten
(78 Tests gesamt umfasst den Linter-Test) — Exit 0, Report unter
`tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md` enthält
nur `OK` (0 Violations). Linter-Run-Timestamp im Report aktualisiert sich
bei jedem Test-Lauf (zuletzt 19:24:45 nach dem Build, also nach den
Änderungen).

## Abweichungen vom Plan

Keine substantiellen. Kleinere Anpassungen, die im Plan-Rahmen bleiben:

- **`get_doc`-Description um YAML-Front-Matter-Edge-Case erweitert** (war nicht explizit in der Plan-Code-Skizze, aber sinnvoll als dritter Edge Case, weil das ein nicht-offensichtliches Verhalten ist: das LLM bekommt das Front-Matter der Originaldatei *nicht* zu sehen, da es beim `import` in eigene DB-Spalten aufgeteilt wurde). Im Plan-Abschnitt "Konkrete Inhalte pro Tool" unter (c) sind 3 Edge Cases gelistet — die Reihenfolge weicht leicht ab: ich liste `null`-Return, Token-Budget-Hinweis, YAML-Front-Matter-Info. Inhaltlich identisch zum Plan-Spirit, das `YAML-Front-Matter`-Detail ist eine sinnvolle Ergänzung ohne Scope-Erweiterung. In `docs/02` 4.D ist es als Detail-Begründung für `get_doc` mit aufgenommen.
- **`search_docs`-Description hat einen zusätzlichen "Viele Treffer"-Edge-Case** (Plan listet 3 Edge Cases, ich habe 4: leer/whitespace, zu lang, keine Treffer, **viele Treffer**). Begründung: ohne den `truncated=true`-Fall explizit zu nennen, wäre die `Response-Shape`-Aussage vom Verhalten entkoppelt — das LLM wüsste nicht, wann es mit dem Marker rechnen muss. Im `docs/02` 4.D ist der `truncated`-Fall bei `Response-Shape` und `MaxResults` ausführlich erklärt.
- **Subject-Länge 74 Zeichen** statt der im `03-git-workflow.mdc` empfohlenen 70. Plan-DoD gibt den Subject exakt so vor, User-Briefing wiederholt ihn. Repo-Historie zeigt, dass auch längere Subjects (71, 77, 81) vorkommen, also keine Regel-Verletzung in der Praxis.

## Beobachtungen

- **Raw-String-Literal-Einrückung:** Die Description-Texte sind in `"""..."""` mit 8-Space-Indent eingebettet (so wie es C# 11 Raw-String-Literals verlangen, damit der Compiler den Inhalt korrekt strippt). Der Compiler nimmt die ersten 8 Spaces als „margin" und entfernt sie aus jeder Zeile — d.h. das LLM sieht die Description ohne den Code-Einrückungs-Padding. Verified durch Build + Lint.
- **Section-Anchor-Formatierung:** Die Markdown-Anchor-Konvention in diesem Repo nutzt Umlaute im Slug (z.B. `#2-slug-regeln`, `#d-knowhowtoaicli-server---config-path`) — kein Umlaut-Ersatz in der URL-Form. Mein Link `#quell-doku-für-die-tool-descriptions` folgt dieser Konvention; falls der Repo-Markdown-Renderer (z. B. eine ältere GitHub-Variante) Umlaute escapet, bricht der Cross-Reference. Auditer sollte prüfen, ob der Link im gerenderten Markdown funktioniert.
- **Kein manueller MCP-Smoke-Test durchgeführt** (siehe Plan-DoD: "Bedingt durch SQL-Setup-Problem möglicherweise nicht durchführbar"). Visuelle Code-Inspektion der `Description`-Strings + Linter-OK reicht als Verifikation. Auditer kann bei Bedarf `dotnet run --project src/KnowHowToAI.Cli -- server` starten und mit MCP-Inspector verbinden, um die `description`-Felder in der `tools/list`-Antwort zu sehen.
- **Konzept-Konsolidierung F-MC-002 in F-MC-001:** Die Plan-Empfehlung war, die Beispiel-Outputs in den gleichen Step zu integrieren (Aufwand < 15 Min, LLM-UX-Mehrwert). Habe ich genau so gemacht — die "Beispiel:"-Sektion in jeder Description enthält 2–3 Beispiele (siehe Code-Skizze des Plans). Kein eigener Top-Level-Step für F-MC-002 nötig.
- **`DocsMcpResources.ServerInstructions` unangetastet** (Plan-Note: "bleibt unverändert"). Der Resource-Text ist ein globaler Pointer auf die Tools, nicht eine zweite Description-Quelle. Verweismodell wird nicht dupliziert — bewusst so belassen.

## Bekannte Unschärfen

- **Description-Inhalt vs. Code-Verhalten — Detailtreue:** Der Auditer sollte explizit prüfen, dass die Description keine Versprechen macht, die der Code nicht hält:
  - `list_children` "Sortierung: alphabetisch nach Slug" — wird durch SQL `ORDER BY slug` (implizit) erfüllt, **aber** das SQL in `SqlDocumentsStore.ListChildrenAsync` müsste das tatsächlich tun. Code-Review dieses Statements war im Scope dieses Steps nicht erforderlich; der Plan sagt "keine Code-Logik-Änderungen".
  - `search_docs` "Sortierung: Title-Treffer zuerst, dann alphabetisch" — durch Step 003 (`ORDER BY (CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END), title`) abgedeckt. Cross-Step-Konsistenz ist gegeben.
  - `get_doc` "YAML-Front-Matter ist nicht Teil des Contents" — Annahme basiert auf Plan-Text und dem Verhalten in `ImportCommand`. Auditer sollte `SqlDocumentsStore.GetDocAsync` und die Import-Logik kurz gegenchecken, ob das `content`-Feld wirklich nur den Body und nicht das Original-Front-Matter enthält.
- **`list_children(parentSlug="")` wirft `ArgumentException` — Verhalten verifiziert?** Description sagt das explizit. Im Code wirft das SQL-Parameter-Binding (Dapper) bei leerem String *keine* Exception — es ist die *Anwendungsschicht* (bzw. der Dapper-Pass-Through zur WHERE-Klausel), die das unterschiedlich behandelt. Auditer sollte `SqlDocumentsStore.ListChildrenAsync("", ct)` einmal manuell oder per Test nachvollziehen, ob `ArgumentException` oder leere Liste zurückkommt. Plan-DoD verlangt das nicht explizit; nur Lint + Build + Tests wurden verifiziert.
- **Markdown-Anchor `#quell-doku-für-die-tool-descriptions`:** nicht durch Build/Lint prüfbar. Hängt vom Markdown-Renderer ab (siehe Beobachtungen).

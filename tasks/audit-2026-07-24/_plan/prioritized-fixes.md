# Priorisierter Fix-Plan

> **Methodik:** Reihenfolge nach "wenn ich Ralf wäre und 4 Stunden Zeit hätte, was
> zuerst?". Tiebreak-Kriterien: (a) Security > Performance > Architektur > LLM-UX >
> Konvention. (b) Bei Gleichstand: weniger Dependencies zwischen Fixes = früher.
> (c) Bei weiterem Gleichstand: weniger Dateien anfasst = früher.

## Status-Legende

- ✅ = umgesetzt + committed (siehe Commit-Hash in Klammern)
- 🚧 = in Bearbeitung
- ⏭️ = bewusst übersprungen
- ⬜ = offen

## Sofort (≤ 30 Min, hoher Impact)

Diese Fixes sind klein genug für einen einzelnen Commit und haben entweder
Sicherheits- oder Performance-Impact, der pro Tool-Aufruf oder pro Log-Zeile spürbar ist.

### Fix #1 — ✅ F-PE-001: Doppelte JSON-Serialisierung in `LogResponseSize` eliminieren (`d262095`)

**Schweregrad:** High · **Aufwand:** ~20 Min · **Commit-Granularität:** 1 Commit
**Impact:** Alle Tool-Aufrufe werden schneller; bei großen `get_doc`-Antworten
sparen wir 100% der Serialisierungs-Zeit (siehe `_findings/F-PE-001-double-json-serialize.md`).

**Konkrete Schritte:**
1. `Measure<T>(T response)` Helper nach `src/KnowHowToAI.Core/Logging/ResponseSize.cs` extrahieren
2. Drei `LogResponseSize`-Aufrufe in `DocsMcpTools.cs` auf `ResponseSize.Measure(result)` umstellen
3. Lokalen `LogResponseSize<T>`-Helper + `using System.Text.Json;` löschen
4. `docs/02` Zeile 120 (F-DK-001) anpassen
5. Tests in `tests/KnowHowToAI.Core.Tests/ResponseSizeTests.cs` (6 Cases)

**Tatsächliche Umsetzung (Commit `d262095`):** Wie geplant + Regel-Disput mit
`02-testing.mdc` (Switch-Expression mit 4 Cases ist "eigene Logik", muss nach Core)
per Refactor aufgelöst. Helper lebt jetzt in Core, 6 Tests dazu, 49 → 55 grün.

**Geschätzte Latenz-Reduktion pro Tool-Call:**
- 1 KB: vernachlässigbar
- 100 KB: ~1-2 ms
- 1 MB: ~15-30 ms

---

### Fix #2 — F-CQ-001 + F-CQ-002: `sealed` für `Document` und `ValidationResult` / `Document` als Record

**Schweregrad:** High (F-CQ-001) · **Aufwand:** ~10 Min · **Commit-Granularität:** 1 Commit
**Impact:** AiNetLinter-Konformität wiederhergestellt, `Document` als Record
liefert kostenlos `Equals`/`GetHashCode`/`ToString`/`with`.

**Konkrete Schritte:**
1. `Document` zu `sealed record Document(Slug, ParentSlug, Title, Content, Tags, Synonyms)` umbauen
2. `ValidationResult` zu `sealed record ValidationResult(IReadOnlyList<ValidationError> Errors, IReadOnlyList<ValidationError> Warnings)` mit `IsValid => Errors.Count == 0`
3. `rules.json` checken, ob die Klassen absichtlich ausgenommen waren — wenn ja,
   dokumentieren
4. Build + Tests grün

**Risiko:** Niedrig. Tests nutzen `new Document { ... }`-Syntax, die auch für
Records funktioniert.

---

### Fix #4 — F-PE-003: `ORDER BY slug` in `ListChildrenAsync`

**Schweregrad:** Medium · **Aufwand:** ~5 Min · **Commit-Granularität:** 1 Commit
**Impact:** Deterministische Treffer-Reihenfolge. LLM bekommt bei wiederholten
Aufrufen konsistente Ergebnisse.

**Konkrete Schritte:**
1. `ListChildrenAsync` SQL: `ORDER BY slug` anhängen
2. Test in `SqlDocumentsStore`-Test-Suite (siehe F-TS-001 — aktuell nicht
   existent, das ist eine Quick-Lösung)

---

### Fix #2 — ✅ F-CQ-001/002: `Document` und `ValidationResult` als sealed records (`27570cd`)

**Schweregrad:** High (F-CQ-001) + Medium (F-CQ-002, im selben Commit gelöst)
**Aufwand:** ~10 Min · **Commit-Granularität:** 1 Commit
**Impact:** Konsistenz mit den anderen Domain-Records (`DocumentSummary`,
`DocumentDetail`, `ValidationError`, `SqlScript`, `DocumentRow`,
`FrontMatterData`). Records liefern implizites `sealed`, `Equals`/`GetHashCode`
per Property (für Wert-Type-Props), `ToString()` mit Property-Liste,
`with`-Expression.

**Tatsächliche Umsetzung (Commit `27570cd`):** `Document` und `ValidationResult`
als positional records. `required`-Keyword entfernt (Begründung: C# 14 erlaubt
required nur als Property-Modifier, positional-ctor-Params sind ohnehin required).
Alle 5 Aufrufer auf positional ctor umgestellt. Verifier hatte zwei
Nicht-FAIL-Findings: (a) Record-Equality für `IReadOnlyList<T>` ist per Referenz,
(b) `Tags`/`Synonyms` können null sein. Beides im App-Kontext irrelevant: kein
Aufrufer vergleicht Document-Instanzen via Equals/==, und die zwei Aufrufer von
Document liefern konsistent non-null. Status Quo akzeptiert. 55 Tests grün.

### Fix #3 — F-CQ-003: Defensive `JsonSerializer.Deserialize`

**Schweregrad:** Medium · **Aufwand:** ~10 Min · **Commit-Granularität:** 1 Commit
**Impact:** Wenn DB-Daten verbogen sind, gibt `export` eine leere Liste zurück
statt `JsonException`.

**Konkrete Schritte:**
1. `DeserializeJsonArrayOrEmpty(string? json)` Helper in `SqlDocumentsStore`
2. `!`-Suppressor entfernen
3. Logger-Aufruf mit Warning (benötigt F-AR-002)

---

## Kurzfristig (1-2 Stunden, hoher Impact)

Diese Fixes brauchen ~1-2 Stunden und sind über mehrere Dateien verteilt. Sie
lösen *Klassen* von Problemen, nicht Einzelfälle.

### Fix #6 — F-SE-001: LIKE-Wildcard-Escaping + Längen-Cap

**Schweregrad:** High · **Aufwand:** ~45 Min · **Commit-Granularität:** 1 Commit
**Impact:** DoS-Vektor geschlossen. LLM kann nicht mehr via `%`/`_`/`[`/`\`
amplifizieren. Längen-Cap schützt vor Memory-Issues.

**Tatsächliche Umsetzung (Commit `27570cd`):** `Document` und `ValidationResult`
als positional records. `required`-Keyword entfernt (Begründung: C# 14 erlaubt
required nur als Property-Modifier, positional-ctor-Params sind ohnehin required).
Alle 5 Aufrufer auf positional ctor umgestellt. Verifier hatte zwei
Nicht-FAIL-Findings: (a) Record-Equality für `IReadOnlyList<T>` ist per Referenz,
(b) `Tags`/`Synonyms` können null sein. Beides im App-Kontext irrelevant: kein
Aufrufer vergleicht Document-Instanzen via Equals/==, und die zwei Aufrufer von
Document liefern konsistent non-null. Status Quo akzeptiert. 55 Tests grün.

**Konkrete Schritte (verbleibend, F-CQ-003):**
1. `KnowHowToAiOptions.Search.MaxQueryLength` (Default 200) hinzufügen
2. `SearchDocsAsync` validiert + escaped
3. `BuildLikePattern` schreibt auf Escaped-Form
4. Tests (über Reflection oder `InternalsVisibleTo`)
5. `appsettings.json` ergänzen
6. `docs/02`/`docs/03` aktualisieren
7. `docs/04` Zeile 48-49 aktualisieren (LIKE-Semantik präziser)

**Detail:** [`_findings/F-SE-001-like-wildcard-injection.md`](../_findings/F-SE-001-like-wildcard-injection.md)

---

### Fix #7 — F-AR-002: `ILogger<T>`-Injection in Core-Services

**Schweregrad:** High · **Aufwand:** ~1,5 Stunden · **Commit-Granularität:** 1-2 Commits
**Impact:** Beobachtbarkeit. Jeder Service kann pro Aufruf start/ende/duration loggen.
SQL-Operationen werden transparent. Audit-Trail ermöglicht.

**Konkrete Schritte:**
1. `Microsoft.Extensions.Logging.Abstractions` zu `Core.csproj`
2. `SqlDocumentsStore` umstellen
3. `DocsValidator` umstellen
4. `ImportService` und `ExportService` umstellen
5. Tests mit `NullLogger<T>.Instance` anpassen
6. `Program.cs` (Composition Root) reicht `Log.Logger` als `ILogger<T>` durch

**Detail:** [`_findings/F-AR-002-core-services-without-logger.md`](../_findings/F-AR-002-core-services-without-logger.md)

---

### Fix #8 — F-AR-001: DI-Inkonsistenz mit Composition-Root-Pattern beheben

**Schweregrad:** High · **Aufwand:** ~30 Min · **Commit-Granularität:** 1 Commit
**Impact:** Service-Konstruktion ist konsistent. Refactor-Sicherheit für
Decorator-Pattern, AOP, etc.

**Konkrete Schritte:**
1. `BuildCoreServices(KnowHowToAiOptions options)` Factory-Funktion in `Program.cs`
2. `RunValidate`/`RunImport`/`RunExport`/`RunServer` über die Factory
3. Tests unverändert (Services werden weiterhin per `new` in Tests konstruiert)

**Risiko:** Niedrig. Funktional keine Änderung.

---

### Fix #9 — F-CD-001: String-Enum-Validation in `Logging`-Options

**Schweregrad:** High · **Aufwand:** ~20 Min · **Commit-Granularität:** 1 Commit
**Impact:** Bei Tippfehler in `appsettings.json` bekommt der User eine
verständliche Fehlermeldung mit erlaubten Werten.

**Konkrete Schritte:**
1. `ParseLogLevel`/`ParseRollingInterval` Helper mit `Enum.TryParse` + eigener Message
2. Tests für Valid + Invalid
3. Doku-Hinweis in `docs/03`

---

### Fix #10 — F-MC-001: Tool-Description-Qualität (mit Fix #2 verbunden)

**Schweregrad:** High · **Aufwand:** ~30 Min + Doku-Update
**Commit-Granularität:** 1 Commit (nach Fix #2, weil beide die Description anfassen)
**Impact:** LLM kann die Tools *optimal* benutzen. Edge-Cases werden vor dem
LLM-Aufruf klar, Fehlinterpretationen werden vermieden.

**Konkrete Schritte:**
1. `list_children`-Description: Edge-Cases (null vs. "", nicht-existente Slugs, Sortierung)
2. `search_docs`-Description: LIKE-Semantik, Wildcard-Escape (kommt mit Fix #6),
   Ranking, Cap
3. `get_doc`-Description: null-Return, Token-Budget
4. Optional: Beispiel-Outputs (siehe F-MC-002, kann zusammen kommen)
5. `docs/02` Abschnitt 4.D als Referenz festhalten

**Detail:** [`_findings/F-MC-001-tool-description-quality.md`](../_findings/F-MC-001-tool-description-quality.md) (im Plan erwähnt, nicht als separate Detail-Datei erstellt)

---

## Mittelfristig (mehrere Stunden, hohes Impact)

Diese Fixes sind *eigenständige Projekte* und brauchen Planung + mehrere
Commits. Sie sind nicht-blockierend für v1, aber wichtig für die langfristige
Gesundheit des Projekts.

### Fix #11 — F-DP-001: Preview-Dependencies klären

**Schweregrad:** High (Dependency-Choice) · **Aufwand:** ~10 Min (Downgrade) oder
~30 Min (bewusst behalten + dokumentieren)
**Impact:** Reduziert das Risiko, dass Breaking Changes aus 2.0.0 / 3.0.0 das
Tool ohne Vorwarnung kaputtmachen.

**Konkrete Schritte:**
1. **Option A (Downgrade):** `ModelContextProtocol` auf `1.4.1` (Stable),
   `System.CommandLine` auf `2.0.10` (Stable). Build + Tests grün.
2. **Option B (Beibehalten):** Bewusste Wahl dokumentieren in `docs/02`
   Tech-Stack-Tabelle. Wiederevaluations-Trigger festhalten: nach 2.0.0 Stable.

**Empfehlung:** Option A für `System.CommandLine` (schnell, kein Funktionsverlust).
Option B für `ModelContextProtocol`, *wenn* 2.0-Stable innerhalb 4 Wochen kommt;
sonst auch Downgrade.

---

### Fix #12 — F-PE-002: `TOP`-Cap für `SearchDocsAsync` + Title-Ranking

**Schweregrad:** High · **Aufwand:** ~30 Min
**Impact:** Verhindert Token-Budget-Sprengung bei breiten Suchen. Title-Ranking
verbessert LLM-UX.

**Konkrete Schritte:**
1. `KnowHowToAiOptions.Search.MaxResults` (Default 50)
2. SQL: `SELECT TOP (@MaxResults) ... ORDER BY (CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END), title`
3. Tests
4. `docs/02` aktualisieren

**Detail:** [`_findings/F-MC-001-tool-description-quality.md`](../_findings/F-MC-001-tool-description-quality.md) (cross-ref)

---

### Fix #13 — F-TS-001: SQL-Integrationstest-Infrastruktur (Backlog-Item)

**Schweregrad:** High (per Doku akzeptiert, aber langfristig wichtig)
**Aufwand:** ~4 Stunden (SQLite-Setup) + laufende Test-Pflege
**Impact:** Tests für `SqlDocumentsStore`. Edge-Cases (F-SE-001, F-CQ-003,
F-SE-004) werden testbar.

**Konkrete Schritte:**
1. SQLite-In-Memory oder Testcontainers für SQL-Server
2. `SqlDocumentsStore`-Tests gegen die In-Memory-DB
3. Besondere Vorsicht: `LIKE`-Semantik unterscheidet sich zwischen SQLite und
   SQL Server (Index-Nutzung, Escape-Verhalten)
4. Alternative: Bestehende `SqlIdentifierValidator`-Tests als Vorbild

**Detail:** Siehe Dim 4.

---

### Fix #14 — F-DK-001: Doku für `LogResponseSize` (mit Fix #1)

Schon im Plan als Teil von Fix #1. Wenn Fix #1 umgesetzt ist, ist F-DK-001
automatisch obsolet (Doku muss aktualisiert werden).

---

### Fix #15 — F-DK-005 bis F-DK-008: Doku-Lücken schließen (Rest nach Prio-B-Extraktion)

**Schweregrad:** Medium · **Aufwand:** ~15 Min total
**Impact:** Vollständigere Doku. Konsistenter mit Code.

**Hinweis:** F-DK-002, F-DK-003, F-DK-004 sind in Prio B extrahiert (siehe `tasks/audit-2026-07-24-PrioB/Konzept.md`).

**Konkrete Schritte:**
1. F-DK-005: Preview-Dependencies (wird ggf. mit F-DP-001 in Brocken B kombiniert)
2. F-DK-006: `TrustServerCertificate=True` erklären
3. F-DK-007: `Microsoft.Data.SqlClient 7.0` Breaking Changes
4. F-DK-008: `authoring-guide` Slug-Beispiele (per Audit "kein Handlungsbedarf")

**Commit:** Ein einzelner Doku-Commit.

---

### Fix #16 — F-SE-004 + F-AR-005: `SqlIdentifierValidator` lowercase-only + `Constants.cs`

**Schweregrad:** Medium · **Aufwand:** ~30 Min
**Impact:** Plattform-Inkonsistenz behoben. Konsistente Identifier-Regeln
analog zu `SlugRules`.

**Konkrete Schritte:**
1. `SqlIdentifierValidator` Regex auf `^[a-z_][a-z0-9_]{0,99}$` umstellen
2. Reserved-Words-Liste (ca. 50 häufigste) als HashSet hinzufügen
3. Tests anpassen
4. Falls `Constants.cs` entsteht: `FrontMatterParser.delimiter` dorthin verschieben
5. Doku in `docs/03` aktualisieren

---

## Zusammenfassung

| Phase | Anzahl Fixes | Gesamtaufwand | Reihenfolge |
| --- | --- | --- | --- |
| Sofort (≤ 30 Min) | 5 | ~1,5 h | #1 → #2 → #3 → #4 → #5 |
| Kurzfristig (1-2 h) | 5 | ~4 h | #6 → #7 → #8 → #9 → #10 |
| Mittelfristig (mehrere h) | 6 | ~7 h | #11 → #12 → #13 → #14 → #15 → #16 |

**Gesamt-Schätzung:** ~12,5 Stunden reines Implementieren (ohne Reviews, ohne
Re-Tests, ohne Doku-Politur). In der Praxis eher 2-3 Arbeitstage, mit den
notwendigen Reviews + Diskussionen.

**Empfohlene Commit-Reihenfolge:**

```
1. F-CQ-001/002 (sealed/record) — architektonisch unabhängig
2. F-PE-003 (ORDER BY) — Datenbank-Behavior
3. F-CQ-003 (defensive Deserialize) — Robustheit
4. F-PE-001 (LogResponseSize) — MCP-Loop
5. F-MC-001 (Tool-Descriptions) — LLM-UX, baut auf Fix #4 auf
6. F-CD-001 (Enum-Validation) — Konfig
7. F-SE-001 (LIKE-Wildcard) — Security
8. F-AR-002 (ILogger-Injection) — Architektur
9. F-AR-001 (Composition Root) — Architektur, baut auf #8
10. F-DP-001 (Preview-Dependencies) — Dependency-Update
11. F-PE-002 (TOP-Cap) — Performance
12. F-TS-001 (SQL-Tests) — Test-Infrastruktur
13. Doku-Commit (F-DK-005 bis F-DK-008; F-DK-001 obsolet; F-DK-002/003/004 in Prio B)
```

**Optional, separate Commits:**
- F-AR-004 (SchemaMigrator-Transaktion) — eigenständig
- F-AR-005 (Constants.cs) — kann entstehen, wenn F-SE-004 umgesetzt wird
- F-CD-002/003/004 (Config-Sicherheit) — größeres Refactor, eigener Plan
- F-AR-003 (LogResponseSize in falscher Schicht) — obsolet nach Fix #4

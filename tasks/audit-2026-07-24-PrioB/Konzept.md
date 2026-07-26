# Audit Prio B — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** `tasks/audit-2026-07-24-PrioA/Konzept.md` (umgesetzt)
> **Methodik:** Aus dem Gesamt-Audit (85 Findings nach Prio A) wurden die 7 Findings extrahiert, die unter „Tool-UX & Doku-Polish" zusammengefasst sind. Bewertung primär aus LLM-Sicht (was braucht ein LLM, das diesen MCP-Server benutzt?), sekundär aus Maintainer-Sicht (interne Doku-Lücken). Alle übrigen Findings (78) wurden bewusst weggelassen — Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | LLM-UX? | Status |
|---|---|---|---|---|---|
| [F-MC-002](#f-mc-002--keine-tool-beispiele-in-den-descriptions) | Keine Tool-Beispiele in den Descriptions | Medium | ~20 Min | ja | **erledigt** |
| [F-MC-003](#f-mc-003--serverinstructions-zentral-undokumentiert) | `ServerInstructions` zentral undokumentiert (= F-DK-002) | Medium | ~5 Min | ja | **erledigt** |
| [F-MC-004](#f-mc-004--null-semantik-für-get_doc-undokumentiert) | `null`-Semantik für `get_doc` undokumentiert | Medium | ~5 Min | ja | **erledigt** (war schon im Prio-A-Commit enthalten) |
| [F-MC-005](#f-mc-005--authoring-guide-length-warning-fehlt) | `authoring-guide` Length-Warning fehlt | Medium | ~5 Min | ja | **erledigt** |
| [F-MC-006](#f-mc-006--tool-naming-konvention-undokumentiert) | Tool-Naming-Konvention undokumentiert | Low | ~2 Min | ja (für Maintainer) | **erledigt** |
| [F-DK-003](#f-dk-003--service-konstruktion-in-programcs-undokumentiert) | Service-Konstruktion in `Program.cs` undokumentiert | Medium | ~10 Min | nein (Maintainer) | **erledigt** |
| [F-DK-004](#f-dk-004--schemamigrator-transaktions-verlust-undokumentiert) | `SchemaMigrator` Transaktions-Verlust undokumentiert | Medium | ~5 Min Doku / ~15 Min Code | nein (Maintainer) | **erledigt** (Variante A: Doku) |

**Gesamt-Aufwand:** ~1 Stunde (37 Min Doku + 20 Min Code + 5 Min für F-MC-002 mit Tests). Aufteilbar in 2-3 Commits.

**Leitidee:** LLM, das diesen MCP-Server benutzt, soll nicht raten müssen. Konkrete Beispiele, klare Edge-Cases, dokumentierte Semantik. Plus ein paar interne Doku-Lücken, die bei nächster Gelegenheit sowieso weh tun würden.

---

## F-MC-002 — Keine Tool-Beispiele in den Descriptions

> **Schweregrad:** Medium · **Dimension:** MCP-Tool-API
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` (alle drei `[Description]`-Attribute)

### Problem

Die aktuellen Descriptions sind *Definitionen*, nicht *Anleitungen*. LLMs verarbeiten Beispiele oft besser als abstrakte Beschreibungen.

**Beispiel-Pattern (für LLM-UX):**
```json
// list_children(parentSlug="it")
[
  {"slug": "it/netzwerk", "title": "Netzwerk"},
  {"slug": "it/security", "title": "Security"}
]
```

Wenn das in der Description steht, hat das LLM ein konkretes Format-Verständnis und muss nicht raten.

### Fix-Empfehlung

Pro Tool ein konkretes JSON-Beispiel in der Description. Die Beispiele sollten *valide* sein (syntaktisch und semantisch), sonst verwirrt das LLM mehr als es hilft.

Beispiele:
- `list_children(parentSlug="it")` → Array von `DocumentSummary`
- `search_docs(query="netzwerk konfiguration")` → Array von `DocumentSummary` (max. N Treffer, sortiert nach Title-Treffern)
- `get_doc(slug="it/netzwerk/vlan")` → `DocumentDetail` mit `title` + `content`, oder `null` wenn nicht gefunden

### Aufwand

- ~20 Min
- 1 Commit (kann mit F-MC-001 aus Prio A kombiniert werden, falls noch nicht committed)

### Risiko

Niedrig. Reine Text-Erweiterung in `[Description(...)]`-Attributen. Wenn die Beispiele *valide* sind (Format, Feldnamen, Semantik), gibt es kein Risiko. Wenn nicht, verwirrt es das LLM.

**Achtung:** Wenn sich später `DocumentSummary`/`DocumentDetail` ändert (Field-Renaming, neue Property), müssen die Beispiele mitgezogen werden. Code-Review-Checkliste ergänzen.

---

## F-MC-003 — `ServerInstructions` zentral undokumentiert

> **Schweregrad:** Medium · **Dimension:** MCP-Tool-API + Doku
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpResources.cs:11-14`
> **Zusammenhang:** Identisch mit F-DK-002 in Dim 5 (Doku-Drift). Hier aus API-Perspektive, dort aus Doku-Perspektive.

### Problem

`ServerInstructions` ist die *Eingangstür* für jedes verbundene LLM. Bei Änderungen (z.B. neuer Workflow-Schritt, geänderte Tool-Liste) gibt es keinen Test, keinen Review-Prozess, keine Dokumentation, die die LLM-UX-Wirksamkeit prüft.

Der konkrete Wortlaut aus `DocsMcpResources.cs:11-14`:
> "KnowHowToAI: durchsuchbare Wissensdatenbank. Lesen: list_children/search_docs/get_doc. Neue oder geänderte Doku als .md-Datei im docs-root anlegen (Format siehe Resource docs://authoring-guide), danach 'validate' und 'import' per CLI ausführen."

Diese Phrase ist *die* Eingangstür. Wenn jemand den Text "verbessert" (z.B. "weniger kryptisch"), könnte die LLM-Wirksamkeit sinken.

### Fix-Empfehlung

1. In `docs/02-Architektur-und-Techstack.md` oder einem neuen `docs/06-LLM-UX.md` den exakten Wortlaut zitieren
2. Wirkungs-Achsen dokumentieren: Länge, Vokabular, Reihenfolge der Tools
3. Bei Änderungen: Changelog-Eintrag in `docs/05-Roadmap.md` oder neuem `docs/06-LLM-UX.md#changelog`

### Aufwand

- ~5 Min (nur Doku)
- 1 Commit

### Risiko

Niedrig. Reine Doku. Wert entsteht, wenn jemand später die `ServerInstructions` ändern will und nachschlagen kann, warum sie so formuliert sind.

---

## F-MC-004 — `null`-Semantik für `get_doc` undokumentiert

> **Schweregrad:** Medium · **Dimension:** MCP-Tool-API
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:34` (Description)

### Problem

`GetDocAsync` returnt `DocumentDetail?` (nullable). Die Description sagt das nicht explizit. LLMs neigen dazu, `null` als Fehler zu interpretieren und in eine Fehlerbehandlungs-Schleife zu gehen.

**Beispiel-Schleife (häufig in LLM-Logs beobachtet):**
```
LLM: get_doc(slug="foo")
Tool: null
LLM: "foo existiert nicht. Ich versuche foo-bar."
LLM: get_doc(slug="foo-bar")
Tool: null
LLM: "..."
```

Mit klarer Description würde das LLM frühzeitig aufhören.

### Fix-Empfehlung

In die Description explizit einbauen:
```csharp
[Description("""
    Lädt Titel und Inhalt eines einzelnen Dokuments.

    Returnt null, wenn der Slug nicht existiert (kein Fehler, einfach nicht da).
    Das LLM soll dann einen anderen Slug probieren oder die Anfrage abbrechen.
    """)]
```

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-MC-002 kombiniert werden)

### Risiko

Niedrig. Reine Text-Änderung in `[Description(...)]`.

---

## F-MC-005 — `authoring-guide` Length-Warning fehlt

> **Schweregrad:** Medium · **Dimension:** MCP-Tool-API
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpResources.cs:46-50` (Resource)

### Problem

Der `authoring-guide` (Resource) lehrt LLMs, neue Doku zu schreiben. Aber: er erwähnt nicht, dass zu lange Doku problematisch ist (`MaxContentLengthWarning` in `appsettings.json` + Validator). Ein LLM, das einen 50-KB-Block als ein Doc schreibt, bekommt beim nächsten `validate` eine Warning, aber das LLM weiß nicht warum.

### Fix-Empfehlung

Im `authoring-guide` einen kurzen Hinweis ergänzen:
> "Einzeldokumente sollten idealerweise unter 8.000 Zeichen bleiben (Schwelle konfigurierbar in `appsettings.json`). Längere Inhalte sind möglich, aber das LLM bekommt sie in `get_doc` als *ganzen* Content — Token-Budget!"

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-MC-004 kombiniert werden)

### Risiko

Niedrig. Reine Resource-Text-Erweiterung.

---

## F-MC-006 — Tool-Naming-Konvention undokumentiert

> **Schweregrad:** Low · **Dimension:** MCP-Tool-API + Doku
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` (Tool-Namen) + `docs/02` (Doku)

### Problem

Die drei Tools heißen `list_children`, `search_docs`, `get_doc` — snake_case, alle mit Verb im Imperativ. Das ist MCP-Standard, aber nirgends im Repo dokumentiert.

**LLM-Sicht:** Eher Maintainer-Thema. Ein LLM erkennt die Konvention implizit. Aber für neue Tools wäre eine dokumentierte Konvention hilfreich.

### Fix-Empfehlung

Kurzer Absatz in `docs/02`:
> "MCP-Tool-Namen: snake_case, Verb im Imperativ. Beispiele: `list_children`, `search_docs`, `get_doc`. Beim Hinzufügen eines neuen Tools: Name mit Doku abgleichen."

### Aufwand

- ~2 Min
- 1 Commit (kann mit F-MC-005 kombiniert werden)

### Risiko

Niedrig. Reine Doku.

---

## F-DK-003 — Service-Konstruktion in `Program.cs` undokumentiert

> **Schweregrad:** Medium · **Dimension:** Doku
> **Datei:** `docs/03-Projektstruktur-und-Konfiguration.md` Zeile 7-46

### Problem

`docs/03` zeigt das Solution-Layout als Block-Schema und beschreibt die Core/Cli-Trennung. Aber: nirgendwo wird erwähnt, *wie* die Cli-Commands ihre Services bekommen.

Aktueller Stand in `Program.cs`:
- `RunValidate` → `new DocsValidator(...)`
- `RunImport` → `new SqlDocumentsStore(...)` + `new ImportService(...)`
- `RunExport` → `new SqlDocumentsStore(...)` + `new ExportService(...)`
- `RunServer` → `AddSingleton<SqlDocumentsStore>` + `WithToolsFromAssembly()`

Die Inkonsistenz (siehe F-AR-001) ist in der Doku weder als bewusst noch als "noch zu refaktorisieren" markiert. Wer das Repo liest, könnte denken: "ist das so gewollt?" und unsicher sein, ob eine Vereinheitlichung gewünscht ist.

### Fix-Empfehlung

Kurzer Abschnitt in `docs/03` (oder Verweis auf F-AR-001 in einem zukünftigen Brocken):
> "Aktuell werden `ImportService`/`ExportService` direkt per `new` in `Program.cs` konstruiert, nicht per DI. Dies ist historisch gewachsen (jeder Cli-Command braucht nur einen Service) und konsistent mit dem schlanken Setup in v1. Vereinheitlichung über ein Composition-Root-Pattern (siehe Audit-F-AR-001) ist angedacht, aber nicht Teil von v1."

### Aufwand

- ~10 Min
- 1 Commit (Doku-Commit)

### Risiko

Niedrig. Reine Doku.

---

## F-DK-004 — `SchemaMigrator` Transaktions-Verlust undokumentiert

> **Schweregrad:** Medium · **Dimension:** Doku (+ optional Code)
> **Datei:** `docs/04-Datenmodell-Validierung-Edgecases.md` Zeile 9-13

### Problem

`docs/04` Zeile 9 sagt:
> "Skripte sind selbst idempotent und laufen bei jedem `import` erneut"

Implizit: kein Bedarf für Migration-Journal. Aber: was passiert, wenn das 2. Skript fehlschlägt, nachdem das 1. erfolgreich war? Aktueller Code (`SchemaMigrator.cs:27-31`):
```csharp
foreach (var script in DiscoverScripts(documentsTableName))
{
    logInformation($"Führe SQL-Skript aus: {script.Name}");
    await connection.ExecuteAsync(new CommandDefinition(script.Sql, ...));
}
```

Jeder `ExecuteAsync` ist auto-committed (kein expliziter `BeginTransaction`). Wenn Skript 1 committed und Skript 2 fehlschlägt, ist die DB in einem halb-migrierten Zustand.

Für das *aktuelle* Skript-Set (nur `0001_create_documents_table.sql`, idempotent) ist das irrelevant. Sobald ein zweites Skript hinzukommt (z.B. ein Index-Skript), wird das Risiko real.

### Fix-Empfehlung

**Variante A — Nur Doku (~5 Min):**
In `docs/04` Zeile 9-13 (Abschnitt "SQL-Skripte") explizit dokumentieren:
> "Der `SchemaMigrator` führt Skripte sequenziell aus, ohne explizite Transaktion. Bei Mehr-Skript-Setups ist fehlende Atomarität ein Risiko — entweder Transaktion um die Skript-Liste legen oder explizit dokumentieren, dass jedes Skript unabhängig idempotent und vor Fehlern sicher sein muss."

**Variante B — Code + Doku (~20 Min):**
`SchemaMigrator.MigrateAsync` in eine Transaktion wickeln:
```csharp
await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
try
{
    foreach (var script in DiscoverScripts(documentsTableName))
    {
        // ...
        await connection.ExecuteAsync(new CommandDefinition(script.Sql, ..., transaction: transaction));
    }
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

**Empfehlung: Variante A jetzt, Variante B wenn Skript #2 ansteht.** Code-Änderung ist klein, aber: kein Skript-Set-Up >1 vorhanden, also kein aktueller Leidensdruck. Wenn der zweite Index-Skript kommt, ist Variante B ein 15-Min-Commit.

### Aufwand

- Variante A: ~5 Min, 1 Doku-Commit
- Variante B: ~20 Min, 1 Code-Commit + Doku-Anpassung

### Risiko

- Variante A: keine
- Variante B: niedrig. Die Idempotenz der Skripte bleibt erhalten, und die Transaktion schützt nur gegen den Halb-Migrations-Zustand. Tests in `SchemaMigratorTests.cs` (F-TS-002) müssten erweitert werden.

---

## Warum diese 7 und nicht andere?

### Aufgenommen

**LLM-UX (5):**
1. **F-MC-002** — Beispiele sind der direkteste Hebel für LLM-Format-Verständnis
2. **F-MC-003** — Eingangstür zum Server; wer sie ändert, muss die Konsequenzen kennen
3. **F-MC-004** — Verhindert LLM-Endlosschleifen bei `null`-Returns
4. **F-MC-005** — Verhindert "warum Warning?" beim LLM beim Schreiben
5. **F-MC-006** — Tool-Naming-Konvention, kostet 2 Min, hilft bei Erweiterung

**Maintainer-Doku (2):**
6. **F-DK-003** — Architektur-Frage, die jeder Repo-Leser stellt; jetzt billig beantworten
7. **F-DK-004** — Risiko wird real, sobald Skript #2 kommt; jetzt dokumentieren ist billig

### Bewusst weggelassen (Kurzbegründung)

- **F-MC-007 (Cancellation exposed):** Per Audit "akzeptiert, 0 Aufwand". Knowledge-Aspekt gehört zu F-MC-001 (Prio A) und kann dort mit aufgenommen werden.
- **F-DK-005 (Preview-Dependencies undokumentiert):** Wird in Brocken B (Architecture & Dependencies) sowieso angefasst (F-DP-001). Doppelt-Arbeit vermeiden.
- **F-DK-006 (TrustServerCertificate undokumentiert):** Lokales Dev-Setup-Detail, niedriger Impact, Config-nah.
- **F-DK-007 (SqlClient 7.0 Breaking Changes):** Per Audit "irrelevant für lokalen Use-Case".
- **F-DK-008 (authoring-guide Slug-Regeln):** Per Audit "kein Handlungsbedarf".

Alle übrigen Findings (78) gehören thematisch in andere Brocken (B: Architecture/Dependencies, C: Sicherheits-Hardening, D: Test-Coverage, E: Performance-Polish, F: Config-Deploy, G: Code-Quality-Rest).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-MC-006** (~2 Min) + **F-MC-005** (~5 Min) + **F-MC-004** (~5 Min) + **F-MC-002** (~20 Min) — alle 4 Tool-Description-Pakete in 1-2 Commits. Bauen aufeinander auf (Reihenfolge: kürzeste zuerst, dann umfangreichere).
2. **F-MC-003 / F-DK-002** (~5 Min) — Doku-Commit für `ServerInstructions`.
3. **F-DK-003** (~10 Min) — Doku-Commit für `Program.cs`-Service-Konstruktion.
4. **F-DK-004** (~5 Min Doku / ~20 Min Code) — Doku zuerst, Code optional wenn Skript #2 kommt.

**Gesamt-Aufwand in dieser Reihenfolge:** ~67 Min, 3-4 Commits.

**Commit-Clustering-Vorschlag:**
- Commit 1: Tool-Description-Paket (F-MC-002, F-MC-004, F-MC-005, F-MC-006) — ~32 Min
- Commit 2: Doku-Cluster (F-MC-003/F-DK-002 + F-DK-003) — ~15 Min
- Commit 3: F-DK-004 (Doku) — ~5 Min
- Optional Commit 4: F-DK-004 (Code, Transaktion) — ~20 Min, später

## Querverweise zu anderen Brocken

- **F-MC-001 in PrioA** — alle Tool-Description-Fixes (F-MC-002/004/005/006) bauen auf der Edge-Case-Description aus Prio A auf. Wenn Prio A noch nicht umgesetzt ist, am besten erst Prio A.
- **F-AR-001 in Brocken B** — `F-DK-003` verweist auf F-AR-001. Wenn Brocken B umgesetzt ist, ist die Doku ggf. obsolet, weil die Inkonsistenz behoben ist.
- **F-DP-001 in Brocken B** — `F-DK-005` ist out-of-scope hier, wird in Brocken B mit F-DP-001 zusammen dokumentiert.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).

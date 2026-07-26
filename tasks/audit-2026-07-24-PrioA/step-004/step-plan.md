---
status: open
type: step-plan
task: audit-2026-07-24-PrioA
step: 004
title: "F-MC-001 + F-MC-002 — Tool-Description-Qualität und Beispiel-Outputs für LLM-Konsumenten"
estimated_risk: low
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T18:00:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#fix-4--f-mc-001-tool-description-qualität"
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#nice-to-have-empfehlungen"
---

# Step 004: F-MC-001 + F-MC-002 — Tool-Description-Qualität und Beispiel-Outputs für LLM-Konsumenten

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `Konzept.md` Sektion „Fix 4 — F-MC-001: Tool-Description-
  Qualität" + Nice-to-Have F-MC-002 (Beispiel-Outputs in MCP-Tool-
  Description)
- **Phase / Priorität:** Kurzfristig (LLM-UX, High) — laut
  Konzept-Tiebreak-Logik nach Security + Performance
- **Abhängigkeiten:** **baut auf Step 002 und Step 003 auf** — die
  Tool-Description dokumentiert das Verhalten aus
  `BuildLikePattern`-Escape (Step 002) und `SearchResult`-Shape
  (Step 003). Wenn diese Schritte nicht abgeschlossen sind, ist
  die hier dokumentierte Beschreibung *ungültig* (Beispiel: `truncated`-
  Marker existiert nicht ohne Step 003).
- **Nice-to-Have-Konsolidierung:** F-MC-002 (Beispiel-Outputs)
  wird laut Konzept-Empfehlung in diesen Step integriert (Aufwand
  < 15 Min, LLM-UX-Mehrwert). Kein eigener Top-Level-Step.

## Intention

Die drei `[Description(...)]`-Strings in `DocsMcpTools` sind heute
einzeilige Kurzhinweise, die dem LLM zentrale Edge-Cases verschweigen
(„Was passiert bei leerem Query?", „Wie erkenne ich, ob die Suche
gekappt wurde?", „Was ist die Sortierung?"). Nach diesem Step bekommt
jedes Tool eine **mehrteilige, strukturierte** Beschreibung mit drei
klaren Abschnitten (Zweck, Edge Cases, Beispiel-Outputs) — und damit
einen Werkzeugkasten, mit dem das LLM die Tools optimal nutzen kann,
ohne durch Probieren herausfinden zu müssen, wie Edge Cases behandelt
werden. Die exakten deutschen Formulierungen legt der Coder fest —
dieser Plan gibt die *inhaltliche Mindest-Spezifikation* und die
Reihenfolge der Abschnitte vor.

`docs/02` Abschnitt 4.D wird zur **Quell-Doku** der
Description-Texte: jede Information, die in einer `[Description(...)]`
steht, hat dort eine ausführlichere Erklärung mit Begründung. Damit
laufen Description und Architektur-Doku nicht auseinander.

## Konkrete Änderungen

### Datei 1: `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs`

- **Was:** Die drei `[Description(...)]`-Strings umschreiben. Struktur
  pro Tool (3 Sektionen, getrennt durch Leerzeilen + Markdown-Heading-
  ähnliche Sektionsmarker):

  ```
  <Zweck: 1-2 Sätze>

  Edge Cases:
  - <Edge-Case 1>
  - <Edge-Case 2>
  - ...

  Beispiel:
  - <Beispiel-Input → erwarteter Output>
  - <weiteres Beispiel>
  ```

  Konkrete Inhalte pro Tool:

  **a) `list_children`:**
  ```
  Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn
  parentSlug weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

  Edge Cases:
  - parentSlug = null oder weggelassen: listet Root-Dokumente
  - parentSlug = "" (leerer String): wirft ArgumentException, nicht das gleiche wie null
  - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
  - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server akzeptiert, liefert leere Liste

  Beispiel:
  - list_children() → DocumentSummary[] der Root-Dokumente
  - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"
  - list_children(parentSlug="gibt-es-nicht") → []

  Es gibt keine Cap; bei sehr breiten Verzeichnissen ggf. >100 Treffer.
  ```

  **b) `search_docs`:**
  ```
  Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff
  (Substring-Match). Liefert die Treffer als SearchResult.

  Response-Shape:
  - { results: DocumentSummary[], truncated: bool }
  - results: Slug + Title der gefundenen Dokumente
  - truncated: true, wenn es mehr Treffer gibt als MaxResults (Default 50,
    konfigurierbar via appsettings.json → KnowHowToAi.Search.MaxResults).
    In dem Fall: Suche verfeinern (präziserer Query) statt alle Treffer
    zu erwarten.

  Semantik:
  - SQL LIKE '%query%' gegen title, content, tags, synonyms
  - Wildcard-Zeichen (% _ [) im Query werden literal behandelt
  - Sortierung: zuerst Title-Treffer, dann alphabetisch nach title

  Edge Cases:
  - query = null/leer/Whitespace: leere results, truncated=false
  - query länger als MaxQueryLength (Default 200, konfigurierbar via
    appsettings.json → KnowHowToAi.Search.MaxQueryLength): Tool-Error
  - Keine Treffer: leere results, truncated=false
  ```

  **c) `get_doc`:**
  ```
  Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs.

  Edge Cases:
  - slug existiert nicht: liefert null (kein Tool-Error)
  - null-Return vom Server: das LLM hat den falschen Slug, bitte list_children
    oder search_docs erneut aufrufen
  - Inhalt ist NVARCHAR(MAX) ohne Trunkierung: bei sehr großen Dokumenten
    kann der Content das Token-Budget sprengen — Aufteilung in mehrere
    Slugs erwägen

  Beispiel:
  - get_doc(slug="it/netzwerk/routing") → DocumentDetail mit title und content
  - get_doc(slug="unbekannt") → null
  ```

- **Warum:** Konzept-Vorgabe explizit so. Reihenfolge: Zweck, dann
  Response-Shape (nur bei `search_docs`, weil die anderen einen
  trivialen Shape haben), dann Semantik / Sortierung, dann Edge
  Cases, dann Beispiel. Die „Beispiel"-Sektion ist die F-MC-002-
  Konsolidierung (Konzept empfiehlt das explizit, „Aufwand < 15 Min,
  LLM-UX-Mehrwert da").
- **Implementation-Hinweis:** Die `[Description(...)]`-Strings sind
  C# 11 Raw-String-Literals (`"""..."""`), damit die mehrzeiligen
  Texte lesbar bleiben. Die exakte deutsche Formulierung legt der
  Coder fest — der Plan gibt die inhaltliche Mindest-Spezifikation
  vor. **Keine englischen Brocken im Description-Text** (Konvention
  im Projekt, siehe `DocsMcpResources.ServerInstructions`).

### Datei 2: `docs/02-Architektur-und-Techstack.md` (Abschnitt 4.D)

- **Was:** Den `search_docs`/`list_children`/`get_doc`-Tool-Block
  (Z. 107-119) zu einer **Quell-Doku für die `[Description(...)]`-
  Texte** ausbauen. Konkret: nach der Tabelle mit den drei Tools
  einen neuen Unterabschnitt „Quell-Doku für die Tool-Descriptions"
  mit:
  1. Verweis: „Die `[Description(...)]`-Strings in `DocsMcpTools.cs`
     sind aus diesem Abschnitt gespeist — bei Änderungen an einer
     Tool-Beschreibung hier und im Code synchron halten."
  2. Eine **detailliertere** Erklärung der Semantik, die über die
     Description hinausgeht:
     - Edge-Case „leerer Query" — Begründung: docs/04 Edge Case 4.2
       (leere DB / kein Fehler) gilt analog für leere Query.
     - Edge-Case „truncated-Marker" — Begründung: Token-Budget-
       Schutz + LLM-UX (Querschnittsregel).
     - Edge-Case „Wildcard-Escape" — Begründung: F-SE-001 (DoS-Schutz,
       nicht-gewollte Wildcard-Bedeutung).
     - Sortierung deterministisch (Title-Ranking, dann alphabetisch)
       — Begründung: F-PE-002 (Heuristik statt komplexer Ranking).
     - Keine Cap bei `list_children` — Begründung: keine
       Token-Budget-Sorge, da LLM Slug-basiert navigiert; Cap wäre
       ein falscher Anreiz.
- **Warum:** Konzept-Vorgabe. Die `docs/02` Doku wird zur
  Single-Source-of-Truth für die Description-Inhalte; die Description
  selbst ist die knappe LLM-Sicht, die Doku die ausführliche
  Architektur-Begründung. Verhindert Description-/Doku-Drift.

### Datei 3: `docs/04-Datenmodell-Validierung-Edgecases.md` (Abschnitt 1, search_docs-Query)

- **Was:** Verweis am Ende des search_docs-Blocks auf den neuen
  `docs/02` Abschnitt 4.D ergänzen:
  ```
  - **LLM-UX-Details** (Edge Cases, Response-Shape-Beispiele,
    Begründungen für die Wahl der Defaults): siehe
    [02, Abschnitt 4.D](02-Architektur-und-Techstack.md).
  ```
- **Warum:** Konsistenz mit dem Verweismodell aus `04-docs-reference.mdc`
  (verweisen statt duplizieren). Der Edge-Case-Teil von F-MC-001 ist
  primär LLM-UX und gehört architektonisch nach `docs/02`; die
  *technische* SQL-Spezifikation (TOP, COUNT(*) OVER(), Title-Ranking)
  bleibt in `docs/04` aus Step 003.

## Tests

Keine automatisierten Tests. Begründung:
- `[Description(...)]`-Strings sind reine LLM-UX-Hinweise, kein
  testbares Verhalten. Die `Description`-Property wird vom
  MCP-SDK an den Client durchgereicht, nicht von eigenem Code
  konsumiert.
- Wenn der Coder oder Auditer die Inhalts-Korrektheit prüfen will:
  manueller Smoke-Test: `dotnet run --project src/KnowHowToAI.Cli --
  server` starten, mit einem MCP-Client (z. B. `mcp-cli` oder
  MCP-Inspector) verbinden, `tools/list` aufrufen, die
  `description`-Felder inspizieren. *Bedingt durch SQL-Setup-Problem
  ist dieser Smoke möglicherweise nicht durchführbar* — in dem Fall
  ist visuelle Code-Inspektion ausreichend.

## Definition of Done

- [ ] `DocsMcpTools.cs` enthält die drei neuen `[Description(...)]`-
      Strings mit den Sektionen Zweck / Edge Cases / Beispiel
      (für `search_docs` zusätzlich Response-Shape + Semantik)
- [ ] Beschreibung von `search_docs` enthält explizit:
      - `SearchResult { results, truncated }`-Shape
      - `truncated: true` → Suche verfeinern
      - Wildcard-Literal-Verhalten (`%`, `_`, `[` literal)
      - Title-Ranking („Title-Treffer zuerst, dann alphabetisch")
      - Längen-Cap-Verweis auf `MaxQueryLength` und `MaxResults`
        (inkl. Default-Werte und `appsettings.json`-Pfad)
- [ ] Beschreibung von `list_children` enthält explizit:
      - null vs. leerer String Unterschied
      - Sortierung alphabetisch nach Slug
      - „keine Cap"-Hinweis
- [ ] Beschreibung von `get_doc` enthält explizit:
      - `null`-Return bei unbekanntem Slug
      - Token-Budget-Hinweis (NVARCHAR(MAX) ohne Trunkierung)
- [ ] `docs/02` Abschnitt 4.D ist um den neuen
      „Quell-Doku für die Tool-Descriptions"-Unterabschnitt erweitert
- [ ] `docs/04` Abschnitt 1 enthält den Verweis auf `docs/02` 4.D
- [ ] `dotnet build -c Release` — 0 Warnings, 0 Errors
- [ ] `dotnet test` — 74 grün (unverändert, keine neuen Tests in
      diesem Step)
- [ ] AiNetLinter 0 neue Verstöße
- [ ] Commit mit Subject
      `docs(mcp): tool-descriptions um edge-cases, response-shape und beispiele erweitern`,
      Body: „LLM-Konsumenten sehen jetzt explizit, wie die drei
      MCP-Tools auf Edge Cases reagieren (leere Eingaben, unbekannte
      Slugs, gekappte Suchergebnisse). `search_docs`-Description
      dokumentiert den neuen `SearchResult`-Shape mit `truncated`-
      Marker und das Bracket-Escape der LIKE-Wildcards. Beispiel-
      Outputs (F-MC-002) sind in jede Description integriert. `docs/02`
      Abschnitt 4.D ist die neue Quell-Doku für Description-Inhalte."
      Trailer: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- [ ] `step-004/step-result.md` geschrieben mit Commit-Hash
- [ ] `status` in `step-plan.md` von `in_progress` auf `done (pending audit)`
      gesetzt

## Rules-Refs

- `.agents/rules/01-code-style.mdc` — keine Kommentare (gilt für
  die Description-Texte auch — sie sind Doku, kein Code-Kommentar;
  gute Lesbarkeit via Strukturierung)
- `.agents/rules/03-git-workflow.mdc` — Conventional Commit, deutsch;
  Subject `docs(mcp):` weil primär Doku-Änderung mit Verhaltens-Impact
  für LLM-Konsumenten
- `.agents/rules/05-documentation.mdc` — Doku im selben Commit
- `.agents/rules/04-docs-reference.mdc` — verweisen statt
  duplizieren (Description ↔ docs/02 4.D ↔ docs/04 Abschnitt 1)

## Bekannte Ausnahmen

- **Keine automatisierten Tests** für die Description-Inhalte.
  Begründung: kein testbares Verhalten, nur LLM-UX-Text. Visuelle
  Code-Inspektion durch den Auditer ist ausreichend.
- **Manueller Smoke-Test via MCP-Client** ist *bedingt* durch das
  SQL-Setup-Problem (docs/03 Abschnitt 2). Falls nicht durchführbar:
  dokumentieren in `step-004/step-result.md` und im `task-summary.md`,
  Audit-Verdict auf Basis visueller Inspektion.

## Code-Skizze

```csharp
[McpServerTool(Name = "list_children"), Description("""
    Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn
    parentSlug weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

    Edge Cases:
    - parentSlug = null oder weggelassen: listet Root-Dokumente
    - parentSlug = "" (leerer String): wirft ArgumentException, nicht das gleiche wie null
    - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
    - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server
      akzeptiert, liefert leere Liste

    Beispiel:
    - list_children() → DocumentSummary[] der Root-Dokumente
    - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"
    - list_children(parentSlug="gibt-es-nicht") → []

    Es gibt keine Cap; bei sehr breiten Verzeichnissen ggf. >100 Treffer.
    """)]
public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
{
    // ... existing body unchanged ...
}
```

## Notes

- **Reihenfolge im Loop:** Step 004 kommt *nach* Step 002 und Step 003,
  weil die hier dokumentierten Verhalten (Wildcard-Escape, SearchResult-
  Shape, Truncation-Marker) erst durch diese Schritte entstehen. Würde
  Step 004 vorher laufen, wäre die Description Lügen-Doku.
- **F-MC-002-Konsolidierung:** die Beispiel-Outputs sind in jeden
  Description-String integriert (siehe Code-Skizze und Konzept). Das
  spart eine separate Commit-Nummer (Konzept-Empfehlung: „rein damit,
  Aufwand-Nachteil minimal").
- **Reihenfolge der Sektionen** in der Description (Zweck → Response-
  Shape → Semantik → Edge Cases → Beispiel) ist eine
  Konvention-Entscheidung. Begründung: das LLM scannt
  typischerweise von oben nach unten, die wichtigste Info (Zweck +
  was passiert bei Edge Cases) kommt zuerst. Beispiel kommt zuletzt,
  weil es die wenigste Aktion vom LLM verlangt.
- **Bezug `DocsMcpResources.ServerInstructions`:** der separate
  Resource-Text (siehe `01-Konzept-und-Workflow.md`) bleibt
  *unverändert* — er ist ein globaler Pointer auf die Tools, nicht
  eine zweite Description-Quelle. Verweismodell wird nicht
  dupliziert.
- **Auditer-Check:** neben visuellem Code-Review sollte der Auditer
  explizit prüfen, dass (a) die in `docs/02` 4.D formulierten
  Begründungen in den Description-Strings nicht widersprüchlich
  verkürzt sind, (b) keine Versprechen in der Description stehen,
  die der Code nicht hält (z. B. „sortiert nach Datum" wenn
  tatsächlich alphabetisch).
- **Optional — `get_doc`-Beispiel um `null`-Return erweitern:**
  habe ich bereits in der `get_doc`-Beschreibung oben. Falls der
  Coder es vorzieht, kann er das `null`-Beispiel auch als Code-
  Kommentar in `GetDocAsync` setzen — der Konzept sieht das nicht
  vor. Empfehlung: nein, weil `[Description(...)]` schon die
  maßgebliche LLM-Schnittstelle ist.

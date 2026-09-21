# Idee: Agentischer Eval-Loop für den MCP-Server (Hermes-CLI als Testtreiber)

**Status:** Idee, kein Entschluss. Entstanden aus der Diskussion um die
Aussagekraft des Exploration-Harness (`src/KnowHowToAI.Exploration`). Verwandt
mit `999_token-basierte-groessenmetriken.md` (Größenmetriken) und dem
Harness-README (In-Process-Dogfooding).

> **Nachtrag 2026-09-17 (Entschluss):** Umsetzung als Ausprägung 3 erfolgt —
> `scripts/eval-run.ps1`, Prompts unter `.agents/prompts/evals/`, Wegwerf-Läufe
> unter `tasks/eval-<name>/`. Der Exploration-Harness wurde im selben Zug
> **vollständig entfernt** (Projekt, Linter-Ausschluss, InternalsVisibleTo);
> Bytegrößen-Metriken lassen sich künftig bei Bedarf aus den
> JSONL-Transkripten der Eval-Läufe ableiten. Die unten stehende Textpassage
> „bleibt unersetzt" ist damit überholt.

## Ausgangslage

Die Testlandschaft deckt zwei Ebenen verlässlich ab — und lässt genau die
mittlere aus, um die es beim späteren Produktbetrieb geht:

- **`StdioProtocolTests`** (Integration): rohes JSON-RPC über echte stdio-Verbindung
  — Initialize, Parse-Fehler, stdout-Reinheit, Unknown-Tool.
- **Exploration-Harness** (In-Process, typisiert): Envelope-Verträge, Fehlercodes,
  End-to-End-Flows, kompakte UTF-8-Bytegrößen als Token-Näherung. Szenarien sind
  fixe C#-Skripte mit compiler-getippten Handler-Calls.

Nicht getestet wird die **Agenten-Sicht**: Ein späterer Agent sieht nur das
SDK-generierte `inputSchema`, muss daraus JSON-Argumente bauen und den
`CallToolResult`-Text lesen. Diese Schichten (Schema-Ergonomie,
JSON-Argument-Binding, agentische Entscheidung über mehrere Turns) überspringen
typisierte Calls komplett — der Harness misst also die Werkzeuge, nicht die
Nutzbarkeit der Werkzeuge.

Branchenstand (recherchiert, Sep 2026):

- **Anthropic, „Writing effective tools for agents — with agents"** (Sep 2025,
  anthropic.com/engineering/writing-tools-for-agents): echter lokaler MCP-Server,
  ein Agent-Loop pro Eval-Task, Tasks als Paar aus Prompt + verifizierbarem
  Outcome, Metriken (Erfolgsquote, Call-Anzahl, Tokens, Tool-Fehler), danach
  Rohe-Transkripte lesen und Tool-Code/Descriptions automatisch verbessern.
- **MCPMark** (arXiv 2509.24002): jede Task = **Instruction + definierter
  Initialzustand + programmatisches Verifikationsskript**; ausdrückliche Falle:
  nicht die Aufrufsequenz bewerten, sondern das Ergebnis.
- **Anthropic, „Demystifying evals"**: deterministische Grader (Unit-Tests/
  Skripte) bevorzugen, LLM-Rubrics nur wo nötig.

Gemeinsamer Nenner aller Muster: der MCP-Server läuft **echt mit** (stdio), das
Szenario wird nicht von Hand getippt, sondern von einem live entscheidenden
Agenten gefahren, und bewertet wird programmatisch das Outcome.

## Idee

Drei Ausprägungen, von denen zwei verworfen wurden:

1. **CLI-Agent-View im Harness (zweite Schnittstelle zum MCP-Server).**
   Verworfen. Eine CLI, die über denselben Invocation-Pfad läuft wie
   `tools/call`, würde die Schichten zwar abdecken — aber sie erfindet einen
   **dritten Contract** (neben MCP-Schema und Envelope), der driftet, und
   liefert keinen Agent-Loop. Gemessen: ein kompletter Harness-Prozess
   (Host-Start, Migration, DB-Roundtrip) kostet **0,27 s**, reine
   Host-Konstruktion 0,10 s — Performance wäre kein Argument gewesen, die
   Ablehnung ist rein architektonisch.
2. **Eigener LLM-Loop im Harness** (API-Key + While-Loop um Tool-Calls).
   Verworfen: doppelt gebaut — ein fertiger Agent-Loop existiert bereits als
   Hermes-CLI-Prozess, inklusive Terminal-, Datei- und Journal-Zugriff.
3. **Agentischer Eval-Loop mit Hermes-CLI als Treiber (bevorzugt).**
   Der Orchestrator (Hermes-Desktop-Session, **ohne** MCP-Tools) fährt einen
   Phasenloop:
   1. Eval-Task auswählen (`tasks/999_ideen`-Nachfolger oder `tasks/evals/`):
      MCPMark-Dreieck — Instruction, Initialzustand (welcher DB-Seed),
      Verifikation (was programmatisch geprüft wird, z. B. per SQL oder
      Read-Tool-Call).
   2. DB-Reset/Seed als **eigener Schleifenschritt** (nicht Aufgabe des
      Eval-Agenten, sonst bereichert ein früher Run den Initialzustand des
      nächsten).
   3. `hermes chat -q "<Task>" --max-turns N` starten. Dieser Prozess liest
      beim eigenen Startup die MCP-Config, **spannt den KnowHowToAI-Server als
      Kindprozess** (Start/Stop implizit, kein Skript), entdeckt die Tools
      frisch und löst die Task agentisch. Er schreibt **nur** ins Journal
      (z. B. `temp/eval-journal-<task>.md`) — kein Code, keine Commits.
   4. Orchestrator liest das Journal (Befunde: was lief schief, wo war das
      Schema missverständlich, welche Fehlerbilder traten auf).
   5. Bestehender **Writer** (einer, Pathspec-Commits, wie im etablierten
      Orchestrator-Modell) fixt Code/Descriptions; Orchestrator verifiziert
      wie gewohnt (git diff, Build, Tests).
   6. Loop von vorne — jeder Eval-Run ist ein neuer Hermes-Prozess mit
      neuem Server-Kindprozess und damit **immer frischem Schema-Discovery**
      des gerade gebauten Binaries.

### Hermes-Fakten, auf denen das beruht

- MCP-Verbindungsaufbau nur beim Session-Start (Server als Subprozess, Tools
  einmalig entdeckt, `mcp_<server>_<tool>`-Präfix).
- Verbindungsabbruch: Auto-Reconnect (5 Versuche, exponentiell bis 60 s) —
  aber **Schemas werden nicht neu entdeckt**; eine Session über einen Rebuild
  hinweg arbeitet mit veraltetem Toolset. Kein Hot-Reload der Config.
- Deshalb: nie eine Session über die Fix-/Build-Phase hinweg halten — das
  Phasenmodell oben macht das Problem strukturell unmöglich.
- Einmaliges Setup: `hermes mcp add knowhowtoai --command dotnet --args run
  --project …/KnowHowToAI.Server` (oder Pfad zur gebauten exe). Der Pfad ist
  über Builds konstant → kein Config-Edit pro Iteration. Die Desktop-Session
  des Orchestrators braucht die Tools nicht und muss nicht neu gestartet
  werden.
- Optional: dediziertes Profil (`hermes profile create eval`, Aufruf mit
  `-p eval`), um die Tools aus normalen Sessions fernzuhalten.

## Nutzen

- Testet genau die Schicht, die im späteren Betrieb das Risiko trägt
  (Schema-Ergonomie, JSON-Binding, mehrstufige agentische Entscheidung), und
  folgt dabei dem von Anthropic publizierten und von MCPMark standardisierten
  Muster — kein eigenes Verfahren.
- Bauaufwand minimal: Eval-Task-Dateien, Journal-Konvention, einmal
  `hermes mcp add`. Keine neue Infrastruktur, keine API-Key-Verwaltung im
  Harness, keine zweite Schnittstelle am Server.
- Aufgabentrennung fällt nebenbei ab: Eval-Agent (liest/ruft auf, schreibt nur
  Journal) ≠ Writer (fixt, hat keine MCP-Tools) ≠ Orchestrator (verifiziert).
  Das bestehende Sequenzialitätsmodell bleibt intakt — es kommt eine Aufgabe
  *neben* den Writer hinzu, kein zweiter Writer.
- Der Exploration-Harness bleibt unersetzt: Er ist weiter das Messinstrument
  für Bytegrößen/Contract-Befunde und der schnellste Contract-Explorer beim
  Schreiben der Eval-Tasks.

## Offene Fragen / Risiken

- **Kosten/Laufzeit:** Jeder Eval-Run ist ein voller Agenten-Loop (Modell-
  Aufrufe). Startgröße 5–10 Tasks; Budget pro Run und Turn-Limit (`--max-turns`)
  festlegen.
- **Read-only-Regel des Eval-Agenten ist soft:** Der Task-Prompt beschränkt das
  Schreibziel auf den Journal-Pfad, aber nicht hart erzwingbar. Akzeptabel,
  solange der Orchestrator `git status` wie gewohnt prüft.
- **Scratch-DB-Konkurrenz:** Eval-Läufe und IntegrationTests leeren dieselbe
  DB (`SqlTestDatabase`) — Läufe strikt sequenziell halten.
- **Journal-Format** für maschinelles Auslesen definieren (Befund-Liste mit
  Severity + betroffenem Tool/Contract), damit der Orchestrator→Writer-Übergang
  stabil ist.
- **Verhältnis zu bestehenden Tests klären:** Eval-Tasks ergänzen, sie
  ersetzen weder `StdioProtocolTests` noch den Harness; Einordnung im
  TestRichtlinien-Dokument erst bei Entschluss.
- **Determinismus:** Agenten-Runs sind nichtdeterministisch; Einzelbefunde
  sind Hinweise, keine Regression-Gates. Ein CI-Gate (falls je gewünscht)
  würde ein gefrorenes Task-Set mit programmatischen Gradern brauchen —
  bewusst nicht Teil dieser Idee.

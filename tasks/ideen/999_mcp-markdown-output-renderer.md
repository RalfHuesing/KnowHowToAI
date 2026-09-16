# Idee: Markdown-Renderer für MCP-Tool-Ausgaben

**Status:** Idee, kein Entschluss. Hier zunächst nur festgehalten, um Token-Effizienz
der MCP-Antworten separat zu diskutieren und ggf. als eigenen Roadmap-Slice zu bewerten.

## Ausgangslage

Jedes MCP-Tool gibt heute genau ein JSON-Objekt zurück (gemeinsamer Antwort-Envelope,
Konzept 05-MCP-API.md, Abschnitt „Gemeinsamer Antwort-Envelope"). Konsument ist
primär ein Agent/LLM; das JSON wird vom MCP SDK in einen Text-Content-Block
serialisiert. Strukturierte Maschinenkonsumenten (z. B. spätere REST-API) existieren
noch nicht, sind aber möglich.

## Die Idee

Zwischen Envelope und MCP-Ausgabe einen separaten Markdown-Renderer schalten:
Agenten lesen dann Markdown (mit Frontmatter für Metadaten) statt JSON.

- Der Envelope (JSON) bleibt unverändert die kanonische Domänenrepräsentation
  und Vertragsbasis (Konzept, Vertragstests, spätere REST-API).
- Der Renderer wäre reine Präsentationsschicht in `Server.Mcp`:
  - **Markdown mit Frontmatter** für Read-/Listen-Tools (search, diff, history,
    get_node, …) — IDs bleiben exakt round-trip-tauglich in den Metadaten.
  - **JSON-Passthrough** für Fehler-Envelopes und kleine
    Transaktions-Bestätigungen (begin/commit/discard) — eindeutig und kompakt.

## Bisherige Einschätzung (geschätzt, nicht gemessen)

- Einzelnode mit Content (get_node): nur ca. 8 % Zeichen-Ersparnis — der
  Content selbst dominiert; JSON-Key-/Escape-Overhead ist gering.
- Listen-Tools: ca. 50–70 % Ersparnis, weil JSON die Feldnamen pro Eintrag
  wiederholt (Beispiel: 20 Suchtreffer ≈ 1.240 → ~370 Tokens).
- Realistische Gesamtersparnis im typischen Agent-Workflow (viele Searches,
  wenige vollständige Node-Lesevorgänge): ca. 30–50 % der Read-Token.

## Offene Fragen / Risiken

- Frontmatter-Kollision: Node-Content kann selbst `---` am Zeilenanfang
  enthalten → Delimiter-Strategie nötig.
- Round-Trip-Garantie für IDs muss für beide Formate in Vertragstests fixiert
  werden; Drift zwischen JSON- und Markdown-Darstellung verhindern.
- Tool-Descriptions müssen das Ausgabeformat pro Tool eindeutig benennen.
- Entscheidung brauchte echte Messung an M5-Acceptance-Payloads, nicht
  Schätzungen.

# Idee: Token-Budget-Regressionstests

**Status:** Idee, kein Entschluss. Quer zu `mcp-markdown-output-renderer.md`,
`mcp-handle-alias-ids.md` und `token-basierte-groessenmetriken.md` — macht
deren Nutzen messbar statt diskutabel.

## Ausgangslage

Token-Effizienz ist Konzept-Prinzip (Abschnitt 50), aber nirgends gemessen.
Die laufenden Ideen (Markdown-Renderer, Handle-Alias-IDs, Token-Metriken)
wurden bisher mit Schätzungen und Beispielrechnungen bewertet. Ohne Messung
bleibt jede Entscheidung davon Geschmackssache, und unbemerkte
Verschlechterungen (neue Felder, größere Payloads, neue Tools) summieren
sich stumm.

## Die Idee

Ein Fixture-Workflows als Test: ein typischer Agent-Ablauf
(z. B. `begin_transaction → list_children → search → get_node →
update_content → validate → commit`) wird gegen die
Integrations-Testumgebung gefahren, und die **kumulierte Größe aller
Tool-Kommunikation** (Requests + Responses) wird gezählt und gegen ein
Budget asserted.

- Zähler: Zeichen oder geschätzte Tokens (einheitlich, schätzbasiert —
  siehe `token-basierte-groessenmetriken.md`; wichtig ist Konsistenz
  zwischen Runs, nicht Absolute-Wahrheit).
- Budget pro Workflow als Test-Assertion → Token-Effizienz wird zur
  Engineerings-Metrik mit Regressionsschutz: Jede Vertrags- oder
  Payload-Änderung, die die Kommunikation verteuert, macht den Test rot.
- Varianten des Workflows (kleine/große Nodes, Listen mit langer Seite)
  erzeugen eine kleine Matrix; pro Zeile ein Budget.

## Nutzen

1. **Bewertungswerkzeug für offene Ideen:** Renderer, Handles und Metriken
   lassen sich durch einfaches Umschalten und Messen der Testläufe
   entscheiden — echte Zahlen statt Schätzungen (die offene Frage aller
   drei bisherigen Ideendateien).
2. **Regressionsschutz:** Verteuerungen werden sichtbar, bevor sie sich
   stapeln.
3. **Dokumentationswert:** Die Budget-Matrix macht die Effizienz-Ziele des
   Konzepts konktret prüfbar (Abschnitt 50 bekommt Zahelen).

## Offene Fragen / Risiken

- **Fixture-Realismus:** synthetic Testdaten müssen repräsentativ sein
  (deutscher Prosa-Text, Markdown-Struktur, realistische Node-Größen) —
  sonst misst man die falsche Welt.
- **Schwelle-Kalibrierung:** Erstes Budget aus einer Baseline-Messung
  ableiten (Baseline einmal messen, dann anbinden), nicht willkürlich.
- **Testebene:** Integrations-Tests (Server + MCP-Vertrag) statt Unit —
  TestRichtlinien einordnen lassen; Läufe sollten schnell bleiben.
- Kein Blocker für die anderen Ideen, sondern das Bewertungsinstrument
  dafür — sinnvollerweise *vor* der Renderer-/Handle-Entscheidung
  aufsetzen.

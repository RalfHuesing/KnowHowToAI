# Idee: Handle-Alias-IDs an der MCP-Kante (GUID bleibt kanonisch)

**Status:** Idee, kein Entschluss. Ergänzt / überschneidet sich mit
`mcp-markdown-output-renderer.md` — beide leben als Präsentationsschicht
an der MCP-Kante.

## Ausgangslage

Alle MCP-Tools geben GUIDs nach außen zurück (`transactionId`, `baseSnapshotId`,
`workingSnapshotId`, `nodeId`, `snapshotId`, `contentRevisionId`). Konzept
05-MCP-API.md schreibt GUID-Parsebarkeit verbindlich vor (Abschnitt 35:
nicht GUID-parsebarer `transactionId` → `TransactionNotFound`).

Konsument ist primär ein Agent/LLM. Eine GUID (36 Zeichen) kostet in
BPE-Tokenisierung grob 10–15 Tokens, und `transactionId` steht in *jedem*
Schreib- und Read-Aufruf (Konzept Abschnitt 62) sowie in jeder Antwort —
bei ~30 Calls pro Session also ~60 Vorkommen ≈ 700 Tokens allein für diese ID.

## Die Idee

GUIDs bleiben die kanonische Identität (DB, Domäne, Content, Dependencies,
Exporte). An der MCP-Kante liegt eine Alias-Map mit internem Zähler:

- Ausgabe: `GUID → Handle`, z. B. `"56de7237-…fd54b"` → `"a"`, mit
  Base62-Zählweise (`a`, `b`, …, `z`, `A`, `B`, …, `9`, `aa`, `ab`, …).
- Eingabe: Handle → Lookup zurück auf die GUID. Die GUID verlässt
  den Server-Prozess (bzw. die Session) nicht.
- Maskierung: Handles tragen ein Präfix wie `"h:a"` — das ist nicht nur
  Kosmetik, sondern Namensraumtrennung: `"h:…"` → Lookup, sonst
  GUID-Parse → Durchreichen. Eingehend werden beide Formate akzeptiert.

## Bewertete Ausprägungen

- **Scope der Map: pro MCP-Session empfohlen.** Zähler startet pro Session
  bei 0, Handles bleiben damit kurz (`a`, `b`, …). Ein Handle ist außerhalb
  seiner Session nicht auflösbar — das entschärft das Erratbarkeits-Risiko
  sequenzieller IDs weitgehend.
- **Lebensdauer:** Session-Ende verwirft die Map. Abgeschlossene/historisierte
  Transaktionen sind danach nur per GUID erreichbar; Tool-Descriptions müssen
  das benennen.
- **Betroffene IDs:** `transactionId` (klar) und ggf. `snapshotId`
  (Read-Selektor). `nodeId` / `contentRevisionId` bewusst *nicht* handeln:
  sie stecken in Dependencies innerhalb von Content und in Exporten — Agenten
  zitieren sie aus Texten, nicht nur aus Tool-Antworten; ein Session-Handle
  hilft dort nicht.

## Token-Bilanz (grob)

`"h:a"` ≈ 2–3 Tokens statt ~10–15 → ~10 Tokens Ersparnis pro Vorkommen,
realistisch ~500–700 Tokens pro Agent-Session. Mittelgroßer Gewinn,
kein Gamechanger.

## Offene Fragen / Risiken

- **Konzept-Änderung nötig:** Abschnitt 35 (GUID-Parse-Regel) und die
  GUID-Beispiele im gemeinsamen Antwort-Envelope. Round-Trip-Garantie bleibt
  unangetastet (Format der Ausgaben = Format der Eingaben, erfüllt auch für
  Handles).
- **Erratbarkeit:** Handles sind sequenziell und damit erratbar; bei lokalem
  STDIO mit einem User unkritisch. Falls `transactionId` je als Capability-Handle
  auf fremde Transaktionen verstanden wird, muss der Session-Scope das abdecken.
- **Cross-Session-Fälle:** Agent kopiert GUID aus Export/Doku → Durchreichen
  von GUIDs eingehend abdecken (siehe Maskierung).
- Umsetzungs-Umfang wäre überschaubar: Handle-Mapper + Session-Registry +
  Vertragstests + Konzept-Edit (Abschnitt 35). Entscheidung brauchte die
  Bewertung im Zusammenspiel mit der Markdown-Renderer-Idee.

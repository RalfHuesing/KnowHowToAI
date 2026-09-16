# Idee: Token-basierte Größenmetriken statt/neben Bytes

**Status:** Idee, kein Entschluss. Verwandt mit `mcp-markdown-output-renderer.md`
und `mcp-handle-alias-ids.md` — alles Aspekte von Token-Budget-Management an
der MCP-Kante.

## Ausgangslage

Die Größe eines Nodes wird in UTF-8-Bytes gemessen: `ContentSizeWarningBytes`
(Standard 4096), `QualityWarningEvaluator` zählt `Encoding.UTF8.GetByteCount`
auf normalisiertem Content und warnt mit `NodeTooLarge`
(`actualBytes`/`thresholdBytes` in den Details).

Das Kernprinzip existiert im Konzept bereits als Abschnitt 50 („Retrieval muss
token-effizient sein") — aber die Maßeinheit der Validierung sind Bytes, während
die faktische Konsumeinheit Tokens sind (Agenten/LLM lesen den Content).
Bytes sind ein Proxy mit ~2× Varianz: 4096 Bytes ≈ 800–1.500 Tokens, je nach
Sprache, Code-Anteil und Markdown-Dichte.

## Die Idee

Größe konzeptionell in Tokens denken und messen — mit zwei Ausprägungen:

1. **Größen-Metadaten für Navigation (der größere Hebel).** Der Konzept-Flow
   (Abschnitt 50) ist `list_children → Metadaten prüfen → Nodes auswählen →
   get_node`. Dafür fehlt dem Agenten heute die Information, wie groß ein Node
   ist: Er kann beim Navigieren nicht budgetieren. Ein `estimatedTokens`-Feld
   in den Navigation-Metadaten (Konzept Abschnitt 51 erweitern) lässt den
   Agenten *vor* dem Abruf entscheiden — z. B. ein 15k-Token-Kapitel nicht
   blind laden.
2. **Zerteilungs-Warnung auf Token-Ziel kalibrieren.** Die Byte-Schwelle wird
   als abgeleitete Größe aus einem Token-Ziel definiert (z. B. Ziel ≤ 2.000
   Tokens × Faktor ≈ 3,5 → ~7.000 Bytes), statt zwei Wahrheiten zu pflegen.
   Das wahre fachliche Kriterium bleibt semantische Kohärenz; Größe ist nur
   Indikator, und für diesen Zweck ist eine Schätzung ±30 % völlig ausreichend.

## Bewertete Ausprägungen

- **Schätzer statt Tokenizer:** Ein einfacher Schätzer (Zeichen ÷ Faktor,
  ggf. skriptabhängig verfeinert) kostet nichts, braucht keine Dependency und
  hält `Core` sauber. Echtes Token-Zählen bräuchte ein
  `IContentTokenizer`-Interface in Domain mit Implementierung in
  Server/Infrastruktur — Modell-abhängig (cl100k/o200k/Gemini weichen
  ±10–20 % ab) und damit unklar, was "die" Wahrheit ist.
- **Bytes bleiben die deterministische technische Schwelle:** Byte-Zählung
  ist versionsstabil; Token-Schätzungen driften mit Tokenizer-/Schätzer-Versionen
  → Vertragstests und `details`-Werte verschieben sich. Agenten round-trippen
  Werte — wechselnde Schätzzahlen sind konfus.
- **Kosten:** Tokenisieren bei jedem `validate_transaction` über alle Contents
  ist machbar, der Schätzer ist gratis.

## Token-Bilanz / Nutzen

Nicht primär Antwort-Token-Ersparnis, sondern Entscheidungsqualität: Der Agent
budgetiert Kontext beim Navigieren statt beim Ausprobieren. Im Zusammenspiel mit
Renderer- und Handle-Idee ein Baustein von „Token-Effizienz als durchgängiges
Prinzip" (Konzept Abschnitt 50).

## Offene Fragen / Risiken

- **Konzept-Änderung:** Abschnitt 51 (Navigation-Metadaten) um
  Größenmetrik erweitern; Abschnitt 50 ggf. um die Maßeinheit ergänzen.
  Envelope-/Warning-Vertrag: bleibt `NodeTooLarge` in Bytes oder kommt ein
  zweiter Warncode/double-Treshold in Tokens?
- **Schätzfaktor kalibrieren** an realen Contents (deutsch, Markdown, Code);
  ggf. pro Skript unterschiedlich.
- **Widerspruch zu Abschnitt 35/Vertragstests vermeiden:** Details-Feldnamen
  und Warn-Codes sind im M6.2-Vertrag fixiert — Änderungen nur als eigener
  Slice mit Vertragstest-Anpassung.

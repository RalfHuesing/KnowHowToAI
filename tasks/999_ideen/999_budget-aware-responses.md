# Idee: Budget-aware Responses (`maxTokens` als universeller Selektor)

**Status:** Idee, kein Entschluss. Verwandt mit
`999_section-adressierter-content-abruf.md` (Section-Abruf ist die feinere,
dies die generische Variante).

## Ausgangslage

Paging existiert für Listen, Search und Diff (`limit`/Cursor, geklemmt auf
`RetrievalPolicy`, Konzept 05-MCP-API.md). Aber der *Inhalt* eines einzelnen
Nodes ist unbegrenzt: `get_content` liefert, was da ist — im Extremfall einen
mehr-Megabyte-Blob in einen Tool-Response. Eine Server-seitige
Antwort-Obergrenze gibt es nicht.

## Die Idee

Alle Read-Tools akzeptieren optional ein Budget-Feld (z. B. `maxTokens` oder
`maxCharacters`): Der Server liefert genau so viel, wie ins Budget passt,
plus Cursor/Continue-Hinweis für den Rest.

- Fallback ohne Feld: konfigurierte Standard-Deckelung
  („niemals ein 5-MB-Blob in einem Response", egal was der Agent anfragt).
- Kürzung erfolgt an sinnvollen Grenzen (Heading-/Abschnittsgrenze,
  Zeilengrenze), nicht mid-markdown; Antwort benennt, wie viel geliefert
  wurde und wie es weitergeht (Fortsatz-Anker statt blinder Cursor).
- Passt strukturell zum bestehenden Paging-Vertrag: fehlend/≤ 0 → Standard,
  oberhalb Maximum geklemmt, Cursor opak.

## Nutzen

- Garantierte Obergrenze pro Tool-Response — Token-Effizienz wird vom
  Agenten-Verhalten entkoppelt (träge Agenten profitieren gratis).
- Vollständige Kontrollierbarkeit des Kontext-Budgets auf Server-Seite;
  Komplement zu `999_token-basierte-groessenmetriken.md` (Metadaten vor dem
  Abruf) und `999_section-adressierter-content-abruf.md` (gezielter Abruf).

## Offene Fragen / Risiken

- **Einheit:** Tokens (schätzbasiert, siehe
  `999_token-basierte-groessenmetriken.md`) oder Zeichen (deterministisch)?
  Empfehlung aus jener Diskussion: deterministische Einheit, ggf. aus
  Token-Ziel abgeleitet.
- **Vertragsänderung:** Neues optionales Feld an allen Read-Tools + Regeln
  für Kürzung/Fortsetzung im Envelope (muss eindeutig erkennbar sein, dass
  gekürzt wurde — nicht still).
- **Zusammenspiel mit Zerteilung:** Deckelung darf die
  Zerteilungs-Warnung nicht obsolet machen, sondern ergänzt sie.
- Verwandte bereits beschlossene Grundlage: Konzept Abschnitt 50 („Retrieval
  muss token-effizient sein") — dies wäre dessen konsequente
  Mechanisierung.

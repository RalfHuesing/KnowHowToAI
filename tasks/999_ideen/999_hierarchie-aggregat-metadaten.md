# Idee: Hierarchie-Aggregat-Metadaten für Navigation

**Status:** Idee, kein Entschluss. Erweitert Konzept Abschnitt 51
(Navigation-Metadaten); verwandt mit `999_token-basierte-groessenmetriken.md`
(`estimatedTokens`-Feld als Teilmenge dieser Idee).

## Ausgangslage

Konzept Abschnitt 51 fordert pro Node `Title` plus kurze zielgruppenunabhängige
`Description`/`Purpose`. Der Retrieval-Flow (Abschnitt 50) ist
`list_children → Metadaten prüfen → Nodes auswählen → get_node`.

Bei wachsendem Wissensbestand werden einzelne Hierarchie-Ebenen sehr
breite Fan-outs bekommen (zum Vergleich: Wikipedia-Kategorien mit
zehntausenden Einträgen). Paging löst das mechanisch — aber ein Agent, der
50 Seiten durchblättert, um den relevanten Zweig zu finden, verbrennt genau
die Tokens, die Abschnitt 50 sparen will. Für die Entscheidung „welchen
Zweig öffne ich?" reicht eine Beschreibung allein nicht mehr.

## Die Idee

Pro Hierarchie-Ebene vorbechnende Aggregat-Metadaten, mit denen der Agent
Fan-outs einschätzen kann, *ohne* sie zu durchblättern:

- `childCount` (direkte Kinder),
- `subtreeSize` bzw. Größenklasse des Teilbaums,
- optional Summe/Größenklassen der Contents im Teilbaum
  (Konsistenz mit `999_token-basierte-groessenmetriken.md`),
- optional charakteristische Stichworte/Top-Begriffe des Teilbaums (als
  spätere Ausbaustufe).

Pflege: Aggregate werden bei Commit aktualisiert (inkrementell entlang der
Pfad-Kette), nicht bei jedem Read neu berechnet.

## Nutzen

Agent navigiert Entscheidung für Entscheidung statt Seite für Seite;
`list_children` bleibt klein, die Auswahl wird eine Frage von zwei bis drei
Calls statt dreißig. Direkte Konsequenz aus Abschnitt 50 auf
Hierarchie-Ebene.

## Offene Fragen / Risiken

- **Konsistenz:** Aggregate müssen transaktionskorrekt mitgepflegt werden
  (Commit aktualisiert die Pfad-Kette; Discard unverändert). Alternative:
  lazily berechnete Cache-Werte mit Invalidierung — einfacher, aber
  potenziell veraltet.
- **Vertragsänderung:** Neue Felder in `list_children`-Payload (M6.2-Vertrag
  + Konzept 51).
- **Schwelle der Nützlichkeit:** Bei kleinem Wissensbestand zunächst
  Rauschen; Felder sollten sparsam gesetzt/nur bei Bedarf geliefert werden
  (WhenWritingNull-Muster existiert bereits).
- Stichwort-/Top-Begriffe-Erweiterung würde eine Index-Infrastruktur
  voraussetzen — bewusst als spätere Ausbaustufe markiert.

# Idee: Section-adressierter Content-Abruf

**Status:** Idee, kein Entschluss. Verwandt mit `budget-aware-responses.md` und
`token-basierte-groessenmetriken.md` (dieselbe Zielrichtung: Agent liest nur,
was er braucht).

## Ausgangslage

`get_content` liefert heute den vollständigen Node-Content. Search liefert
zwar schlank mit Snippets (Konzept Abschnitte 218 ff., Metadata-First), aber
der eigentliche Abruf ist binär: ganzer Node oder nichts.

Unsere Zerteilungs-Warnung (`NodeTooLarge`) empfiehlt fachliche Aufteilung —
aber auch bei disziplinierter Zerteilung wird es Nodes geben, die für einen
einzelnen Agent-Blick zu groß sind (der Wissensbestand wird groß; einzelne
Node-Inhalte wachsen entsprechend). Ein 15k-Token-Node wird dann oft nur
wegen eines Abschnitts komplett gelesen.

## Die Idee

Abruf pro Abschnitt statt nur pro Node: Markdown-Headings werden zu **stabilen
Ankern**, und `get_content` akzeptiert einen optionalen Abschnitts-Selektor,
z. B. `get_content(nodeId, section: "historie")`.

- Anchors sind deterministisch aus der Heading-Struktur ableitbar (Markdown
  hat sie ohnehin; Normalisierung muss sie stabilisieren: Heading-Text →
  Slug, Duplikate numerieren).
- Nicht geliefert: den Rest des Nodes — nur den Abschnitt samt optionaler
  Kinder-Headings (Tiefe konfigurierbar).
- Abschnitts-Selektor zusätzlich als Read-Selektor-Feld neben
  `transactionId`/`snapshotId`/`includeDeleted` denkbar (Konzept Abschnitt
  211 ff., gemeinsame Selektor-Felder).

## Nebeneffekt mit Eigenwert: Zitierfähigkeit

Agent-Antworten können Fundstellen als `Pfad#abschnitt` referenzieren —
prüfbare Verweise in das Wissensarchiv statt nur NodeId. Das wertet
Provenienz und Review auf (Konzept Modul 02).

## Offene Fragen / Risiken

- **Anker-Stabilität:** Umbenennen/Restrukturieren eines Headings ändert den
  Anchor — muss die Round-Trip-/Stabilitätsgarantie mitgedacht bekommen
  (Anker-Redirects oder Alias auf Abschnittsebene?).
- **Vertragsänderung:** Neues optionales Feld/Parameter am Read-Selektor =
  M6.2-Vertragserweiterung; Antwort-Payload muss den tatsächlich gelieferten
  Abschnitt benennen (Agent weiß sonst nicht, was er bekommen hat).
- **Fachliche Zerteilung bleibt zuerst:** Abschnitts-Abruf darf nicht als
  Freibrief für riesige Nodes dienen — Zusammenspiel mit der
  `NodeTooLarge`-Schwelle klären.
- Umsetzungs-Umfang: Normalisierungs-Anchor-Erzeugung + Selektor + Tests;
  überschaubar, aber Konzept-Änderung in 04.

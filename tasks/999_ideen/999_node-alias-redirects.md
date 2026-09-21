# Idee: Aliase/Redirects für Nodes

**Status:** Idee, kein Entschluss. Betrifft Search (Konzept 04, Abschnitte
218 ff.) und ggf. Selektoren.

## Ausgangslage

Search rankt heute Treffer über `Title`, `Description` und `Content`
(HitField-Ränge 1–3). Wenn der Wissensbestand groß und vielfältig wird,
scheitert das an Benennungs-Vielfalt: Ein Node „Garbage Collection in .NET"
wird vom Agenten, der nach „.NET GC" sucht, nicht gefunden — und der Agent
verbrennt danach Tokens auf Such-Umwegen (weitere Suchen, Durchblättern,
Fehlkäufe ganzer Contents).

Zum Vergleich: Wikipedia funktioniert zu erheblichem Teil über Redirects
und Aliase; in einem großen, agentisch wachsenden Archiv entstehen
Synonyme, Abkürzungen und frühere Titel unweigerlich (auch durch
Umbenennungen im Laufe der Zeit).

## Die Idee

Eine Alias-Tabelle pro Node: Synonyme, Abkürzungen, frühere Titel,
alternativ gängige Schreibweisen.

- Search bezieht Aliase in den Ranking-Pfaden mit ein (Treffer in einem
  Alias rangiert sinnvoll, z. B. wie Title/Description, `HitField = "Alias"`).
- Umbenennung eines Nodes hinterlässt den alten Titel als Alias → alte
  Referenzen (auch aus Agent-Kontexten) bleiben auflösbar.
- Alias-Verwaltung über die bestehenden Transaktions-Tools
  (setzbar wie Content, versioniert, rollbackbar).

## Nutzen

Erhöht die Retrieval-Qualität direkt dort, wo der Agent ansetzt: bei der
ersten Suche. Spart die teuerste Art von Token-Verschwendung — die durch
Fehlsuche und daraus folgende Umwege. Günstige Infrastruktur (Tabelle +
Ranking-Erweiterung), hoher Effekt bei großem Bestand.

## Offene Fragen / Risiken

- **Ranking-Integration:** Neuer `HitField`-Wert und Rang-Einordnung müssen
  in die verbindliche Rang-Liste (Abschnitt 224 ff.) eingebaut werden —
  Vertrags- und Konzept-Änderung.
- **Mehrsprachigkeit:** Aliase pro Sprache/Audience? Zielgruppenmodell (Modul 02)
  könnte relevant werden — klären, ob Aliase zielgruppenunabhängig sind
  (empfohlen: ja, sie sind Namens-Varianten, kein Wissen).
- **Missbrauch/Qualität:** Wer pflegt Aliase? Agent-getrieben bei Bedarf
  (Werkzeug „Alias hinzufügen") vs. kuratiert; Gefahr von Alias-Flut →
  Obergrenze/Dedup-Regeln.
- **Fuzzy-Matching-Grenze:** Aliase lösen exakte Benennungs-Varianten, nicht
  Tippfehler-Fuzzy-Search — bewusst abgrenzen (keine Fuzzy-Engine als Scope).

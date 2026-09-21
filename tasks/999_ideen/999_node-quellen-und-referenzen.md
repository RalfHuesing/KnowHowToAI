# Idee: Quellen, Referenzen und Querverweise für Nodes (Provenance & Wissensgraph)

**Status:** Idee, kein Entschluss. Betrifft Datenmodell (Modul 01/Snapshots), Node-Metadaten (Konzept 51), MCP-Tools und Wissensnavigation.

## Ausgangslage

Nodes werden sowohl von menschlichen Entwicklern als auch von autonomen Agenten angelegt und gepflegt. Die Inhalte bestehen aus strukturiertem Fließtext (`ContentMd`).

Bei wachsendem Wissensbestand entstehen zwei grundlegende Herausforderungen:

1. **Herkunft unklar (Provenance & Verifizierbarkeit):**
   - Nach Wochen oder Monaten weiß niemand mehr verlässlich, *woher* eine Aussage stammt (aus welchem Quellcode, welcher Mail, welchem Dokument oder welcher Spezifikation).
   - Weder Mensch noch Agent können schnell nachprüfen, ob eine dokumentierte Tatsache noch zutrifft oder auf veralteten Annahmen basiert.
   - Ein lesender Agent kann nicht unterscheiden, ob ein Inhalt ein reales, nachprüfbares Faktum beschreibt oder eine historische Vermutung war.

2. **Hierarchische Silos vs. vernetztes Wissen (Querspringen):**
   - Die Wissensbasis ist hierarchisch strukturiert (strikter Baum: `ParentNodeId`). Das ist ideal für Organisation, Breadcrumbs und Zielgruppenvererbung.
   - Reales Fachwissen ist jedoch ein **Graph**: Ein Feature-Node unter `Features/Billing` hat starken inhaltlichen Bezug zu `Security/Authentication` und `Architecture/MessageBus`.
   - Ohne Querverweise muss ein Agent mühsam durch den Baum navigieren oder blind per Volltextsuche nach verwandten Konzepten fahnden – was viele Tokens verbrennt.

## Die Kernidee

Jeder Node erhält eine schlanke, versionierte Liste von **Referenzen**. Dabei unterscheiden wir zwei Ausprägungen:

### A. Externe Referenzen (Quellen / Provenienz)
Einzeilige Beschreibung der realen Primärquelle zur Verifikation:
- **Quellcode:** `Code: KnowHowToAI.Core.Services.NodeService.GetNode` → Real nachprüfbarer Fakt: Existiert die Klasse/Funktion noch? Hat sich die Signatur geändert?
- **Kommunikation / Absprachen:** `Mail: "Architekturentscheidung Mandantenfähigkeit", Von: ralf@..., Datum: 2026-03-15`
- **Dateisystem / Dokumente:** `File: \\fileserver\architektur\konzept-v2.docx` oder Dateipfad im Repo `docs/architecture/adr-004.md`
- **Externe Quellen / Web:** `Web: https://learn.microsoft.com/dotnet/...`

### B. Interne Referenzen (Querverweise / Related Nodes)
Direkte Verknüpfung zu anderen Nodes innerhalb der Wissensbasis (`TargetNodeId`):
- Ermöglicht dem Agenten oder Menschen sofortiges **Querspringen** („Siehe auch: Node B, E, G“), ohne den aktuellen Pfad im Baum verlassen und neu suchen zu müssen.
- Der Agent versteht den semantischen Kontext: *„Node X verweist auf A; A verweist auf K → K könnte für das aktuelle Problem relevant sein.“*

## Stabilität gegen Refactoring

- Interne Querverweise referenzieren die unveränderliche, stabile `TargetNodeId` (Guid/Identifier).
- **Vorteil:** Wenn der Ziel-Node umbenannt wird (`Title`-Änderung) oder im Baum an eine andere Stelle verschoben wird (`ParentNodeId`-Änderung), bleibt der Querverweis **vollständig intakt**.
- Bei Löschung eines Ziel-Nodes im Working Snapshot kann die Referenz entweder als Broken/Tombstone markiert oder transaktional bereinigt werden.

## Schutz vor Verwässerung / „Am Ende hängt alles zusammen“ (Anti-Bloat)

Wenn Nodes wahllos mit Dutzenden anderen Nodes verknüpft werden, geht das Signal-Rausch-Verhältnis verloren und der Kontext wird für LLMs unbrauchbar („Context Stuffing“).

Daher gelten verbindliche Schutzmechanismen:
1. **Strikte Obergrenze pro Node:** Z. B. maximal 10–20 interne Querverweise pro Node (bzw. Top-N).
2. **Kuratierte Relevanz:** Nur direkte, unverzichtbare Abhängigkeiten oder Kern-Nachbarkonzepte referenzieren – keine beiläufigen Assoziationen.
3. **Optionaler Relationstyp (später oder Step 1):** z. B. `relies_on`, `related_to`, `alternative_to`.

## Was gehört in Step 1 (Schlank & pragmatisch)

Um das System nicht künstlich aufzublähen, wird Step 1 minimal gehalten:

1. **Relationale Tabelle im Snapshot-Modell (`KnowHowToAI_NodeReferences`):**
   - `ReferenceId` (Identifier)
   - `SnapshotId`, `NodeId` (Quell-Node)
   - `TargetNodeId` (NULL bei externen Referenzen; gesetzt bei internen Querverweisen)
   - `SourceText` (Freitext für externe Quellen; bei internen Referenzen optionaler Kommentar oder leer, da Titel des Ziel-Nodes dynamisch aufgelöst wird)
   - `ReferenceType` (`Code`, `File`, `Mail`, `Web`, `InternalNode`, etc.)
   - `CreatedAt`, `CreatedBy` (Zeitstempel und Ersteller: Mensch oder Agent)
2. **Snapshot-Integration:**
   - Referenzen sind wie Nodes snapshot-gebunden und versioniert.
   - Transaktionale Verwaltung (`add_node_reference`, `remove_node_reference`).
3. **Progressive Disclosure (Token-Effizienz):**
   - In Node-Metadaten (`list_children`, `get_node_metadata`) lediglich Zähler: `externalReferenceCount`, `internalReferenceCount`.
   - Volle Referenzliste wird erst bei gezieltem Abruf (`get_node_references` oder Flag in `get_node`) geliefert.
4. **Web-Frontend:**
   - Externe Quellen: Tabellarische Liste (Text, Typ, Datum).
   - Interne Querverweise: Klickbare Badges / Links direkt zum Ziel-Node (unter Nutzung des Ziel-Node-Titels).

## Was bewusst NICHT in Step 1 gehört (Spätere Ausbaustufen)

- **Keine automatische Verifikations-Engine im Server:** Das Validieren von Code oder Pfaden übernimmt bei Bedarf ein Agent über MCP.
- **Keine komplexe Graph-Datenbank / Cypher-Query-Engine:** Die relationale Tabelle genügt für direkte Querverweise (1st Degree). Graph-Traversierung über mehrere Hops kann ein Agent bei Bedarf iterativ durchführen.
- **Keine automatische Verlinkung im Fließtext:** Keine Inline-Wiki-Links im Markdown, um die Trennung zwischen Struktur (relational) und Inhalt (reines Markdown) strikt einzuhalten.

## Nutzen

- **Agentisches Fact-Checking:** Schnelle Validierung externer Fakten via Tools (`AiNetLinter`, Filesystem).
- **Abkürzung bei Agenten-Navigation:** Direkter Sprung über Domänengrenzen im Baum hinweg spart Suchaufrufe und Token.
- **Auditierbarkeit:** Vollständige Nachvollziehbarkeit sowohl der Herkunft (extern) als auch der Abhängigkeiten (intern).

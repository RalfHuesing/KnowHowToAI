# Idee: Quellen und Referenzen für Nodes (Provenance & Verifizierbarkeit)

**Status:** Idee, kein Entschluss. Betrifft Datenmodell (Modul 01/Snapshots), Node-Metadaten (Konzept 51) und MCP-Tools.

## Ausgangslage

Nodes werden sowohl von menschlichen Entwicklern als auch von autonomen Agenten angelegt und gepflegt. Die Inhalte bestehen aus strukturiertem Fließtext (`ContentMd`).

Nach Wochen oder Monaten entsteht bei wachsendem Wissensbestand ein gravierendes Problem:
- **Herkunft unklar (Provenance):** Niemand weiß mehr verlässlich, *woher* eine Aussage stammt (aus welchem Quellcode, welcher Mail, welchem Dokument oder welcher Spezifikation).
- **Verifizierbarkeit fehlt:** Weder Mensch noch Agent können schnell nachprüfen, ob eine dokumentierte Tatsache noch zutrifft oder auf veralteten Annahmen basiert.
- **Halluzinations-Risiko bei Agenten:** Ein lesender Agent kann nicht unterscheiden, ob ein Inhalt ein reales, nachprüfbares Faktum beschreibt oder eine historische Vermutung war.

## Die Kernidee

Jeder Node erhält eine schlanke, versionierte Liste von Referenzen / Quellenangaben (1 Zeile pro Referenz).

Beispiele für Quellen:
- **Quellcode:** `Code: KnowHowToAI.Core.Services.NodeService.GetNode` (oder DLL / Namespace / Klasse / Funktion) → Real nachprüfbarer Fakt: Existiert die Klasse/Funktion noch? Hat sich die Signatur geändert?
- **Kommunikation / Absprachen:** `Mail: "Architekturentscheidung Mandantenfähigkeit", Von: ralf@..., Datum: 2026-03-15`
- **Dateisystem / Dokumente:** `File: \\fileserver\architektur\konzept-v2.docx` oder Dateipfad im Repo `docs/architecture/adr-004.md`
- **Externe Quellen / Web:** `Web: https://learn.microsoft.com/dotnet/...`

Ein Agent oder Mensch kann dadurch gezielt die Quelle ansteuern und den Node-Inhalt validieren.

## Was gehört in Step 1 (Schlank & pragmatisch)

Um das System nicht künstlich aufzublähen, wird Step 1 minimal gehalten:

1. **Relationale Tabelle im Snapshot-Modell (`KnowHowToAI_NodeReferences`):**
   - `ReferenceId` (Identifier)
   - `SnapshotId`, `NodeId` (Zuordnung zum Node innerhalb des Snapshots)
   - `SourceText` (Einzeiliger Freitext der Quelle, z. B. Pfad, Signatur, Mail-Betreff)
   - `ReferenceType` (optionaler String/Marker zur groben Kategorisierung, z. B. `Code`, `File`, `Mail`, `Web`, `Other`)
   - `CreatedAt`, `CreatedBy` (Zeitstempel und Ersteller: Mensch oder Agent)
2. **Snapshot-Integration:**
   - Referenzen sind wie Nodes snapshot-gebunden und versioniert.
   - Pflege erfolgt transaktionssicher (z. B. `add_node_reference`, `remove_node_reference` im Working Snapshot).
3. **Progressive Disclosure (Token-Effizienz):**
   - In Node-Metadaten (`list_children`, `get_node_metadata`) lediglich `referenceCount: int`.
   - Volle Referenzliste wird über `get_node_references` (oder optionales Flag in `get_node`) geliefert – kein unnötiger Token-Verbrauch beim reinen Durchblättern.
4. **Web-Frontend:**
   - Einfache tabellarische / listenartige Anzeige am Node (z. B. unterhalb der Metadaten oder in einer Seitenleiste).

## Was bewusst NICHT in Step 1 gehört (Spätere Ausbaustufen)

- **Keine typ-spezifischen Sub-Tabellen:** Keine separaten Schemata für Code (mit DLL, Namespace, Parametern), Mails oder URLs. Ein robuster Freitext genügt völlig.
- **Keine automatische Validierungs-Engine im Server:** Der Server prüft nicht selbst, ob DLLs oder Dateien existieren. Die Validierung ist eine Aufgabe für Agenten (z. B. via `AiNetLinter` oder Filesystem-Tools) oder Menschen.
- **Keine binäre Dateiablage:** Keine Dokumentenanhänge oder Dateispeicher in der DB; reine Zeiger/Referenzen.

## Nutzen

- **Agentisches Fact-Checking:** Ein Agent kann gezielt beauftragt werden: *"Prüfe alle Nodes mit Code-Referenzen gegen den aktuellen Stand der Solution"*. Ein Linter- oder AST-Check deckt umbenannte/gelöschte Funktionen sofort auf.
- **Auditierbarkeit:** Entscheidungen und Wissensbausteine bleiben dauerhaft mit ihrem Ursprung verknüpft.
- **Hoher Hebel bei minimalem Aufwand:** Eine einzige Tabelle + einfache MCP-Tools bieten vollständige Nachvollziehbarkeit ohne strukturellen Overhead.

## Offene Fragen & Diskussionspunkte

1. **Node-Level vs. Role-Level:**
   - *Empfehlung Step 1:* Auf Node-Ebene anbinden. Das Thema/Wissen des Nodes hat die Quelle.
   - *Alternative:* Anbindung an Rollen-Content. Würde aber bedeuten, dass bei mehreren Rollen (z. B. Developer, Architect) dieselbe Quelle mehrfach gepflegt werden müsste.
2. **Format-Konventionen:**
   - Reicht reiner Freitext, oder empfiehlt sich ein leichtes Präfix-Schema (z. B. `code:...`, `file:...`, `url:...`) für UI-Icons und Filterung?
3. **Verlinkung im Markdown:**
   - Sollen Referenzen im Fließtext verankert werden (z. B. Footnote-Marker `[^1]`), oder reicht die tabellarische Auflistung am Node? (Empfehlung: Tabellarisch am Node, um Markdown sauber zu halten).

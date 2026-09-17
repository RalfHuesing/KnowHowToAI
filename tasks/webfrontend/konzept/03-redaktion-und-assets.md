# Redaktion und Assets

## Rich-Text-Editor

Der erste schreibende Frontend-Schnitt verwendet einen etablierten Rich-Text-/WYSIWYG-Editor. Ein vorheriger Wegwerf-Memo-Editor ist nicht vorgesehen.

- Markdown bleibt kanonisches Ein-/Ausgabe- und Speicherformat.
- Der Editor muss Markdown verlustarm roundtrippen; HTML-first mit nachträglicher verlustbehafteter Konvertierung reicht nicht.
- WYSIWYG und optionaler Markdown-Quellmodus.
- Formatierungen, Links, Listen, Tabellen, Code, Zitate und Bilder.
- Heading-Funktionen sind deaktiviert.
- Verbotene Markdown-/HTML-Headings werden unmittelbar markiert und serverseitig weiterhin abgelehnt.
- Editorinhalt wird nicht zum Träger von Systemmetadaten.
- Auswahl erfolgt als technischer Spike mit realistischen KnowHowTo-Inhalten und Roundtrip-Tests.

## Bilder und Assets

### Nutzung

- Upload, Drag-and-drop und Einfügen aus der Zwischenablage.
- Keine Data-URLs und keine unkontrollierten lokalen Dateipfade im Markdown.
- Markdown referenziert stabile Asset-Identitäten oder kontrollierte, serverseitig auflösbare URLs.
- Browser, REST, MCP, HTML-Vorschau und PDF verwenden dieselbe Asset-Auflösung.

### Zielmodell

Ein Asset besitzt mindestens:

```text
AssetId
AssetRevisionId oder ContentHash
MimeType
OriginalFileName
SizeBytes
CreatedAt
CreatedBy
```

- Assets sind unveränderlich oder revisioniert.
- Identische Binärinhalte werden per Hash dedupliziert.
- Snapshots referenzieren Assets; sie kopieren nicht bei jeder Transaction die Binärdaten.
- Historische Snapshots und Releases behalten eine reproduzierbare Assetreferenz.
- Dateityp, Größe, Bildabmessungen und tatsächlicher Inhalt werden serverseitig validiert.
- Das konkrete Binärspeichermedium bleibt eine offene Entscheidung; SQL-Metadaten sind unabhängig davon.

## Redaktionelle Hinweise

Ein Text wie `TODO Besser formulieren` darf nicht unkontrolliert in Endkundenexporte gelangen. Fachinhalt und Redaktionsmetadaten bleiben getrennt.

Empfohlenes Zielmodell:

```text
EditorialNote
- NoteId
- NodeId
- optional RoleId
- Text
- Type: Todo | Question | Review | AgentTask
- State: Open | InProgress | Resolved | Dismissed
- CreatedAt / CreatedBy
- optional ResolvedAt / ResolvedBy
```

Anforderungen:

- Hinweise erscheinen im Editor direkt beim betroffenen Node oder Rollen-Content.
- Hinweise sind such- und filterbar.
- Agenten können offene Hinweise gezielt abfragen.
- Fachliche Änderungen erfolgen in einer Transaction.
- Auflösung eines Hinweises und zugehörige Inhaltsänderung bleiben nachvollziehbar verbunden.
- Publikationen enthalten standardmäßig keine Editorial Notes.

Offen: Editorial Notes als Teil versionierter Snapshots oder als separat historisierte Workflow-Metadaten. Sie werden in keinem Fall als `ContentMd` modelliert.

## Agentenauftraege

- `AgentTask` ist zunächst eine redaktionelle Kennzeichnung, kein autonomer Ausführungsmechanismus.
- Externe Agenten lesen Aufgaben über MCP und liefern Änderungen in normalen Transactions.
- Spätere integrierte Agenten erhalten ausgewählten Node, Rolle, Quellen und Auftrag explizit.
- Vorschlag, Diff, Findings und Commit bleiben sichtbar und kontrollierbar.
- Semantic Kernel oder andere Orchestrierung liegt hinter einer eigenen Application-Grenze.

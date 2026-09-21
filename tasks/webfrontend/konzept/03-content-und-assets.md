# Content-Bearbeitung und Assets

## Rich-Text-Editor

Der erste schreibende Frontend-Schnitt verwendet ausschließlich Milkdown `@milkdown/crepe`. Tiptap mit `@tiptap/markdown` ist trotz technisch bestandener Prüfung ausgeschlossen, weil die offizielle Markdown-Erweiterung eine Beta-/Early-Release-API an der kanonischen Speichergrenze ist. Ein vorheriger Wegwerf-Memo-Editor und eine erneute Editor-Auswahl sind nicht vorgesehen.

- Markdown bleibt kanonisches Ein-/Ausgabe- und Speicherformat.
- Der Editor muss Markdown verlustarm roundtrippen; HTML-first mit nachträglicher verlustbehafteter Konvertierung reicht nicht.
- WYSIWYG und ein kontrollierter Markdown-Quellmodus verwenden denselben kanonischen Markdown- und Validierungsvertrag; der Quellmodus wurde im M5.0-Gate aufgenommen.
- Formatierungen, Links, Listen, Tabellen, Code und Zitate; Bilder werden erst in M8 über kontrollierte interne Assets aktiviert.
- Die sichtbare Crepe-Toolbar enthält ausschließlich Bold, Italic, Strikethrough, Inline-Code und Link. Latex, ImageBlock, Headings und Bild-Upload sind deaktiviert.
- Verbotene Markdown-/HTML-Headings werden unmittelbar markiert und serverseitig weiterhin abgelehnt.
- Editorinhalt wird nicht zum Träger von Systemmetadaten.
- Markdown wird über Milkdowns vorhandenen Import und `getMarkdown()` ausgetauscht. Die interne ProseMirror-Struktur ist ausschließlich flüchtiger Editorzustand und wird weder gespeichert noch transportiert.
- Die Blazor-Grenze besteht aus genau einem featurelokalen, dynamisch importierten `ContentEditor.razor.js`: `mount` erhält Markdown sowie Change-/Focus-Callbacks; `readMarkdown`, `focus` und `dispose` sind die einzigen weiteren Aufrufe. Vor erneutem `mount` bei Nodewechsel oder Reconnect wird `dispose` ausgeführt.
- Die Blazor-Grenze exportiert ausschließlich `mount`, `readMarkdown`, `focus` und `dispose`. Der M5.2-T1-Adapter darf intern die Crepe-Instanz pro Element halten, Callback-Fehler abfangen und Dispose idempotent machen; diese eigene Lifecycle-/Zustandslogik wird in `Frontend/tests` mit Vitest ohne Browser geprüft und über `scripts/test-fast.ps1` in das FastTest-Gate integriert.
- Der verbindliche Golden Master umfasst Absätze, fett/kursiv/durchgestrichen, erlaubte Links, geordnete/ungeordnete/verschachtelte Listen, Tabellen, Inline-Code, Fenced Code mit Sprachkennung, Blockquotes und Unicode. Jeder zulässige Master muss fünf aufeinanderfolgende `Markdown -> Editor -> Markdown`-Zyklen Markdig-semantisch äquivalent überstehen.
- Bekannte Sicherheitsfälle bleiben unverändert prüfbar: Raw HTML und Markdown-/HTML-Headings werden nicht still entfernt, sondern an der maßgeblichen Servergrenze abgelehnt; externe Bilder lösen keinen Request aus; Paste-Reduktionen erzeugen einen sichtbaren `role=status`-Hinweis; eine Serverablehnung überschreibt nie den ungespeicherten Editorwert.
- Die spätere M8-Integration darf über den vorhandenen Hook ausschließlich eine bereits serverseitig erzeugte interne Assetreferenz einsetzen. Sie aktiviert keine externen Bild-URLs und keinen direkten Browserupload aus Milkdown heraus.

### Zielgruppen-Content-Modi

- Aufgelöster Fallback ist read-only. Eigener Content wird nur über eine explizite Aktion angelegt oder ersetzt; ein Fallback darf nur bewusst als Ausgangstext übernommen werden.
- Explizit leerer `ContentMd` ist gültiger eigener Content und unterdrückt Fallback nach Bestätigung. Löschen der eigenen Zuordnung ist eine getrennte Mutation und reaktiviert Fallback.
- `Independent` und `Derived` sind explizite Modi. Derived speichert mindestens eine über Node-Suche und Zielgruppe gewählte aktive explizite Source mit ihrer aktuellen Revision; Quellen dürfen nicht manuell als GUID eingegeben werden. Ein Wechsel zu Independent entfernt Quellen nur bestätigt.

## Sichere Markdown-, Link- und Paste-Policy

Die Policy gilt einheitlich für MCP- und Web-Schreibvorgänge, Editor, Browserdarstellung, Markdown-Export und PDF. Sichere Darstellung ersetzt die serverseitige Validierung nicht.

### Raw HTML

- Raw HTML außerhalb von Inline- und Fenced-Code ist kein zulässiger `ContentMd` und wird serverseitig mit `RawHtmlNotAllowed` als harter Fehler abgelehnt. Es wird weder still entfernt noch lediglich im Browser versteckt.
- Front Matter bleibt als Systemmetadatenkanal verboten und wird serverseitig mit `FrontMatterNotAllowed` abgelehnt.
- HTML innerhalb eines Codebereichs bleibt normaler, nicht ausgeführter Beispieltext.
- Browser- und PDF-Renderer führen Raw HTML auch als zusätzliche Abwehr nicht aus. Content wird nie ungeprüft als `MarkupString`, DOM-HTML oder Template-HTML übernommen.
- Bei einer Ablehnung bleibt der vollständige ungespeicherte Editorinhalt erhalten und der konkrete Befund sichtbar.

### Links und Bilder

- Zulässig sind `https`-, `mailto`-, Fragment-, root-relative und normale pfadrelative Links. Netzwerkpfade mit `//` sowie `http`, `javascript`, `data`, `file`, UNC-Pfade und alle nicht ausdrücklich erlaubten Schemas werden serverseitig mit `LinkTargetNotAllowed` abgelehnt.
- Externe Markdown- und HTML-Bildquellen werden mit `ExternalImageNotAllowed` abgelehnt und weder gespeichert noch geladen. Bis M8 gibt es keinen Content-Bildpfad; ab M8 sind ausschließlich die kontrollierten internen Assetreferenzen zulässig.
- Renderer dürfen keine externen oder lokalen Ressourcen nachladen. Externe Links erhalten beim Rendern eine sichere Browserbehandlung ohne Zugriff des Zielkontexts auf die Ursprungsseite.

### Einfügen aus der Zwischenablage

- Plain Text bleibt Plain Text. HTML aus Browsern oder Office wird ausschließlich in den erlaubten Markdownumfang überführt: Absätze, Hervorhebungen, Links, geordnete und ungeordnete Listen, Tabellen, Inline-/Fenced-Code und Blockquotes.
- Styles, Klassen, Skripte, unbekannte Elemente, eingebettete Dateien und Bilder werden verworfen. Unsichere Links werden unter Erhalt ihres sichtbaren Texts entfernt. Eingefügte Headings werden zu normalen Absätzen ohne Headingsemantik.
- Jede inhaltliche Reduktion erzeugt unmittelbar einen sichtbaren, zusammengefassten Hinweis. Sie gilt als ungespeicherte Editoränderung und umgeht nie die serverseitige Validierung.

## Freier Content einschließlich TODOs

`TODO`, `TODO: Besser formulieren` oder vergleichbare Formulierungen sind normaler `ContentMd` wie jeder andere Text.

- Keine besondere Eingabemaske, Annotation, Entität oder Statusverwaltung.
- Keine reservierte TODO-Syntax.
- Keine besondere Validierung, Warnung, Hervorhebung oder Vollständigkeitsprüfung.
- Kein automatischer Ausschluss aus Suche, UI, Markdown-Export oder PDF.
- Gelangt ein TODO in eine Kundenpublikation, ist das kein Systemfehler.
- Ein Agent findet TODOs über die normale Textsuche, liest die betroffenen Nodes und überarbeitet den Content in einer normalen Transaction.
- Die Nachvollziehbarkeit ergibt sich aus Transaction, Snapshot und Diff; es existiert kein zusätzlicher Aufgabenworkflow.

## Bilder und Assets

Priorität: niedrig. Verwaltete Bilder und das Assetmodell werden nach dem einfachen PDF-Export umgesetzt.

### Nutzung

- Upload, Drag-and-drop und Einfügen aus der Zwischenablage.
- Keine Data-URLs und keine unkontrollierten lokalen Dateipfade im Markdown.
- Markdown referenziert stabile Asset-Identitäten oder kontrollierte, serverseitig auflösbare URLs.
- Browser, MCP, Markdown-Export und PDF verwenden dieselbe Asset-Auflösung; spätere Adapter binden dieselbe Auflösung ein.

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

## Spätere integrierte Agenten

- Externe Agenten suchen und bearbeiten Content weiterhin über MCP.
- Eine spätere integrierte Agentenfunktion erhält Suchtext, ausgewählte Nodes, Zielgruppe und Arbeitsauftrag explizit.
- Vorschlag, Diff, Findings und Commit bleiben sichtbar und kontrollierbar.
- Semantic Kernel oder andere Orchestrierung liegt hinter einer eigenen Application-Grenze.
- Daraus entsteht kein besonderes TODO-Datenmodell.

# Entscheidungen und offene Fragen

## Entscheidungsregister

| ID | Status | Entscheidung | Detail |
|---|---|---|---|
| K-001 | Gesetzt | Vollumfängliche menschliche Weboberfläche; Agenten bleiben gleichwertige Clients | [Vision](01-vision-und-produktprinzipien.md#vision) |
| K-002 | Gesetzt | Blazor Web App mit Interactive Server als erster UI-Ansatz | [Architektur](05-architektur-api-und-mcp.md#zielbild) |
| K-003 | Gesetzt | Sachlich-seriöses, modernes Business-UI | [Visueller Stil](02-bedienkonzept-und-ui.md#visueller-stil) |
| K-004 | Gesetzt | Ein ASP.NET-Core-Projekt und eine Deployment-Einheit für UI, REST, Assets und MCP | [Projektstruktur](05-architektur-api-und-mcp.md#projekt--und-namespace-struktur) |
| K-005 | Gesetzt | REST/JSON plus OpenAPI für n8n und allgemeine Integrationen | [HTTP-Endpunkte](05-architektur-api-und-mcp.md#http-endpunkte-und-n8n) |
| K-006 | Gesetzt | MCP-Zieltransport Streamable HTTP; STDIO per eigenem Hard Cut entfernen | [MCP-Transport](05-architektur-api-und-mcp.md#mcp-transport) |
| K-007 | Gesetzt | Erster Stand explizit ohne Authentifizierung und Autorisierung | [Auth-Abgrenzung](06-betrieb-sicherheit-und-risiken.md#explizite-auth-abgrenzung) |
| K-008 | Gesetzt | Etablierte Standardkomponenten vor Eigenentwicklung | [Komponentenstrategie](02-bedienkonzept-und-ui.md#komponentenstrategie) |
| K-009 | Gesetzt | Rich Text/WYSIWYG mit Markdown als kanonischem Format und Bildern | [Rich-Text-Editor](03-content-und-assets.md#rich-text-editor) |
| K-010 | Gesetzt | Globale Hierarchie und dieselbe Wissensbasis für UI, REST und MCP | [Kernleitplanken](01-vision-und-produktprinzipien.md#bestehende-kernleitplanken) |
| K-011 | Gesetzt | TODO ist normaler `ContentMd` ohne Sondermodell, Sondervalidierung oder Exportfilter | [Freier Content](03-content-und-assets.md#freier-content-einschließlich-todos) |
| K-012 | Gesetzt | PDF ist ein niedrig priorisierter Teilbaumexport mit einem Template, Pandoc und WeasyPrint | [Publikation und PDF](04-publikation-und-pdf.md) |
| A-001 | Empfohlen | Eine EXE und ein HTTP(S)-Port; Trennung ausschließlich über Routen | [Ein Prozess und ein Port](05-architektur-api-und-mcp.md#ein-prozess-und-ein-port) |
| A-002 | Empfohlen | Stateless MCP HTTP ohne fachlichen Sessionzustand | [MCP-Transport](05-architektur-api-und-mcp.md#mcp-transport) |
| A-003 | Empfohlen | Pro Browserarbeitskontext genau eine aktive Transaction; bewusster Wechsel erlaubt | [Transaction-Arbeitsbereich](02-bedienkonzept-und-ui.md#transaction-arbeitsbereich) |

## Offene Entscheidungen

| ID | Priorität | Frage | Startempfehlung |
|---|---|---|---|
| O-001 | Hoch | UI-Komponentenpaket | Einheitliches, aktiv gepflegtes Blazor-Paket nach Spike auswählen |
| O-002 | Hoch | Rich-Text-Komponente | Markdown-nativ, verlustarmer Roundtrip, Upload-Hooks, Headings deaktivierbar |
| O-003 | Hoch | Baumkomponente | Lazy Loading, Virtualisierung, Drag-and-drop, Tastaturbedienung |
| O-004 | Hoch | Asset-Speicher | Immutable/dedupliziert; SQL-Metadaten, Binärspeicher nach Spike |
| O-005 | Mittel | Presentation Views | Kanonischen Baum behalten; Views auf denselben `NodeId`s später ergänzen |
| O-006 | Niedrig | Integrierte KI | Erst nach stabilen manuellen Workflows |

Entscheidungen werden hier aktualisiert, bevor abhängige Roadmap-Aufgaben umgesetzt werden.

## Nicht im ersten Frontend

- Authentifizierung, Autorisierung, ACL und Mandantenmodell; eigenes späteres Vorhaben.
- Direkter Endkundenzugang.
- Integrierter Chat oder autonome Agentenläufe.
- Semantic Search und Embeddings.
- Gleichzeitiges kollaboratives Live-Editing.
- Automatisches Merge/Rebase bei Snapshot-Konflikten.
- Frei platzierbarer Graph als Ersatz für den kanonischen Baum.
- Allgemeines CMS mit beliebigen Seitentypen.

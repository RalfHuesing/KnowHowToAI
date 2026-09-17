# Webfrontend und Wissensplattform

Stand: 2026-09-17

Status: Fortlaufendes Zielkonzept; keine Beschreibung des implementierten Ist-Zustands.

## Zweck und Pflege

Dieser Ordner beschreibt Zielbild, Entscheidungen und Umsetzungsreihenfolge für das KnowHowTo-AI-Webfrontend.

- Verbindlicher System-Ist-Stand bleibt [`docs/`](../../docs/README.md).
- Konzeptaussagen werden nicht als bereits implementiertes Verhalten in `docs/` übernommen.
- Relevante Gesprächsergebnisse werden hier fortgeschrieben und automatisch atomar committed.
- Jede umgesetzte Roadmap-Aufgabe aktualisiert im selben Commit Code, Tests, `docs/`, Konzeptstatus und Checkbox.
- Detailaussagen stehen in genau einem Konzeptdokument; Index und Roadmap verlinken darauf.

## Dokumente

| Dokument | Inhalt |
|---|---|
| [Vision und Produktprinzipien](konzept/01-vision-und-produktprinzipien.md) | Zweck, Nutzen, Zielgruppen, bestehende Kernleitplanken, Erfolgskriterien |
| [Bedienkonzept und UI](konzept/02-bedienkonzept-und-ui.md) | visueller Stil, Layout, Dashboard, Wissensbaum, Editor, Transactions, Historie, Rollen |
| [Content und Assets](konzept/03-content-und-assets.md) | Rich Text, freier Markdown-Content einschließlich TODOs, Bilder und Asset-Modell |
| [Publikation und PDF](konzept/04-publikation-und-pdf.md) | einfacher Teilbaumexport mit einem Template, Pandoc und WeasyPrint |
| [Architektur, API und MCP](konzept/05-architektur-api-und-mcp.md) | gemeinsamer Host, interne Blazor-Aufrufe, MCP HTTP, optionale spätere REST-API, Projektstruktur, Ports und DI-Grenzen |
| [Betrieb, Sicherheit und Risiken](konzept/06-betrieb-sicherheit-und-risiken.md) | Intranetbetrieb, ausdrücklich keine Auth im ersten Schritt, Betriebsgrenzen, Risiken |
| [Entscheidungen und offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md) | Entscheidungsregister, offene Auswahlentscheidungen und Nicht-Ziele |
| [Roadmap](Roadmap.md) | Milestones, Abhängigkeiten, abhakbare Arbeitspakete und Abnahmekriterien |

## Lese-Matrix

| Aufgabe | Lesen |
|---|---|
| Scope, Produktziel, Priorisierung | dieses Dokument, [Vision](konzept/01-vision-und-produktprinzipien.md), [Roadmap](Roadmap.md) |
| UI, Layout, Navigation, Editor | [Bedienkonzept](konzept/02-bedienkonzept-und-ui.md), [Content und Assets](konzept/03-content-und-assets.md) |
| Bilder oder freie TODO-Texte | [Content und Assets](konzept/03-content-und-assets.md) |
| Einfacher PDF-Teilbaumexport | [Publikation und PDF](konzept/04-publikation-und-pdf.md) |
| Blazor, MCP HTTP, spätere REST-/n8n-Integration, Projektstruktur | [Architektur, API und MCP](konzept/05-architektur-api-und-mcp.md) |
| Deployment, Netzwerk, Auth-Abgrenzung | [Betrieb, Sicherheit und Risiken](konzept/06-betrieb-sicherheit-und-risiken.md) |
| Implementierung eines Milestones | [Roadmap](Roadmap.md), dort referenzierte Konzeptabschnitte und die Lese-Matrix in [`docs/`](../../docs/README.md) |

## Gesetzte Leitentscheidungen

| ID | Entscheidung |
|---|---|
| K-001 | Vollumfängliche Weboberfläche für Menschen; Agenten bleiben gleichwertige Clients |
| K-002 | Blazor Web App mit Interactive Server als erster UI-Ansatz |
| K-003 | Sachlich-seriöses, modernes Erscheinungsbild einer aktuellen Business-Webanwendung |
| K-004 | Ein ASP.NET-Core-Host für Blazor, Assets und MCP HTTP; um weitere Endpunktgruppen erweiterbar |
| K-005 | Keine allgemeine REST-/OpenAPI-Integration im ersten Schritt; n8n bleibt eine vorbereitete spätere Option |
| K-006 | MCP-Zieltransport ist stateless Streamable HTTP; STDIO wird in eigenem Hard-Cut-Schnitt entfernt |
| K-007 | Erstimplementierung explizit ohne Authentifizierung und Autorisierung; Security ist ein separates Vorhaben |
| K-008 | Etablierte Standardkomponenten vor Eigenentwicklung |
| K-009 | Rich-Text-/WYSIWYG-Bearbeitung mit Markdown als kanonischem Speicherformat und Bildunterstützung |
| K-010 | Eine globale Hierarchie und dieselbe versionierte Wissensbasis für UI, MCP und spätere Adapter |
| K-011 | TODOs sind normaler Content ohne Sondermodell, Sondervalidierung oder Exportfilter |
| K-012 | PDF ist ein niedrig priorisierter Teilbaumexport mit genau einem Template, Pandoc und WeasyPrint |

Details und noch offene Entscheidungen: [Entscheidungsregister](konzept/07-entscheidungen-und-offene-fragen.md).

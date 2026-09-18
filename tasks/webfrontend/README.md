# Webfrontend und Wissensplattform

Stand: 2026-09-18

Status: Fortlaufendes Zielkonzept; keine Beschreibung des implementierten Ist-Zustands.

## Zweck und Pflege

Dieser Ordner beschreibt Zielbild, Entscheidungen und Umsetzungsreihenfolge für das KnowHowTo-AI-Webfrontend.

- Verbindlicher System-Ist-Stand bleibt [`docs/`](../../docs/README.md).
- Konzeptaussagen werden nicht als bereits implementiertes Verhalten in `docs/` übernommen.
- Relevante Gesprächsergebnisse werden hier fortgeschrieben und automatisch atomar committed.
- Das Konzept wird nur bis zum jeweils aktuellen Detailplanungshorizont ausführungsreif geschärft. Aktuell umfasst dieser Horizont M0 bis einschließlich M2; die Freigabe entsteht erst, wenn die dafür offenen Entscheidungen geschlossen und die Tasks abschließend geprüft sind.
- Ab M3 beginnt jedes Milestone mit einem manuellen Planungs- und Konzept-Gate `Mx.0`. Spätere Tasks sind bis zum Abschluss dieses Gates nur ein Richtungsentwurf und nicht zur Agentenausführung freigegeben.
- Jede umgesetzte Roadmap-Aufgabe aktualisiert im selben Commit Code, Tests, `docs/`, Konzeptstatus und Checkbox.
- Detailaussagen stehen in genau einem Konzeptdokument; Index und Roadmap verlinken darauf.

## Dokumente

| Dokument | Inhalt |
|---|---|
| [Vision und Produktprinzipien](konzept/01-vision-und-produktprinzipien.md) | Zweck, Nutzen, Zielgruppen, bestehende Kernleitplanken, Erfolgskriterien |
| [Bedienkonzept und UI](konzept/02-bedienkonzept-und-ui.md) | visueller Stil, Layout, Dashboard, Wissensbaum, Editor, Transactions, Historie, Rollen |
| [Content und Assets](konzept/03-content-und-assets.md) | Rich Text, freier Markdown-Content einschließlich TODOs, Bilder und Asset-Modell |
| [Publikation und PDF](konzept/04-publikation-und-pdf.md) | einfacher Teilbaumexport mit einem Template, Pandoc und WeasyPrint |
| [Architektur, API und MCP](konzept/05-architektur-api-und-mcp.md) | gemeinsamer Host, interne Blazor-Aufrufe, MCP HTTP, optionale spätere REST-API, Ports und Schichtengrenzen |
| [Betrieb, Sicherheit und Risiken](konzept/06-betrieb-sicherheit-und-risiken.md) | Intranetbetrieb, ausdrücklich keine Auth im ersten Schritt, Betriebsgrenzen, Risiken |
| [Offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md) | ausschließlich noch nicht entschiedene Auswahl- und Zukunftsfragen |
| [Projektstruktur und Codekonventionen](konzept/08-projektstruktur-und-codekonventionen.md) | verbindliche Projekte, Ordner, Namespaces, Features, Klassen, Routen und Teststruktur |
| [Roadmap](Roadmap.md) | Index, Ausführungsregeln und getrennte Milestone-Dateien mit abhakbaren Agent-Tasks |
| [Entscheidungsworkshop](Entscheidungsworkshop.md) | aktueller Gesprächsstand, Empfehlungen, Recherche, Reihenfolge und Wiedereinstieg; nicht selbst verbindlich |

## Lese-Matrix

| Aufgabe | Lesen |
|---|---|
| Scope, Produktziel, Priorisierung | dieses Dokument, [Vision](konzept/01-vision-und-produktprinzipien.md), [Roadmap](Roadmap.md) |
| UI, Layout, Navigation, Editor | [Bedienkonzept](konzept/02-bedienkonzept-und-ui.md), [Content und Assets](konzept/03-content-und-assets.md) |
| Bilder oder freie TODO-Texte | [Content und Assets](konzept/03-content-und-assets.md) |
| Einfacher PDF-Teilbaumexport | [Publikation und PDF](konzept/04-publikation-und-pdf.md) |
| Blazor, MCP HTTP, spätere REST-/n8n-Integration, Hosts und Schichtengrenzen | [Architektur, API und MCP](konzept/05-architektur-api-und-mcp.md) |
| Deployment, Netzwerk, Auth-Abgrenzung | [Betrieb, Sicherheit und Risiken](konzept/06-betrieb-sicherheit-und-risiken.md) |
| Offene Entscheidung mit dem Benutzer klären | [Offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md), [Entscheidungsworkshop](Entscheidungsworkshop.md) |
| Ordner, Namespace, neue Klasse, Razor-Komponente oder Testprojekt | [Projektstruktur und Codekonventionen](konzept/08-projektstruktur-und-codekonventionen.md) |
| Implementierung eines Agent-Tasks | [Roadmap](Roadmap.md), zugehörige Milestone-Datei, [Projektstruktur](konzept/08-projektstruktur-und-codekonventionen.md), dort referenzierte Konzeptabschnitte und die Lese-Matrix in [`docs/`](../../docs/README.md) |

## Gesetzte Leitentscheidungen

| ID | Entscheidung |
|---|---|
| K-001 | Vollumfängliche Weboberfläche für Menschen; Agenten bleiben gleichwertige Clients |
| K-002 | Blazor Web App mit Interactive Server als erster UI-Ansatz |
| K-003 | Sachlich-seriöses, modernes Erscheinungsbild einer aktuellen Business-Webanwendung |
| K-004 | Ein ASP.NET-Core-Host für Blazor, Assets und MCP HTTP; um weitere Endpunktgruppen erweiterbar |
| K-005 | Keine allgemeine REST-/OpenAPI-Integration im ersten Schritt; n8n bleibt eine vorbereitete spätere Option |
| K-006 | Streamable HTTP ist danach der einzige MCP-Transport und ersetzt STDIO vollständig; kein Doppel- oder Fallbackbetrieb |
| K-007 | Erstimplementierung explizit ohne Authentifizierung und Autorisierung; Security ist ein separates Vorhaben |
| K-008 | Pragmatische Komponentenstrategie: einfache UI mit Blazor, HTML und überschaubarem CSS selbst umsetzen; spezialisierte Open-Source-Komponenten nur für nachweislich komplexe Controls |
| K-009 | Rich-Text-/WYSIWYG-Bearbeitung mit Markdown als kanonischem Speicherformat und Bildunterstützung |
| K-010 | Eine globale Hierarchie und dieselbe versionierte Wissensbasis für UI, MCP und spätere Adapter |
| K-011 | TODOs sind normaler Content ohne Sondermodell, Sondervalidierung oder Exportfilter |
| K-012 | PDF ist ein niedrig priorisierter Teilbaumexport mit genau einem Template, Pandoc und WeasyPrint |
| K-013 | Bilder und Assetverwaltung sind niedrig priorisiert und folgen erst nach dem einfachen PDF-Export |
| K-014 | Web, MCP und PDF bleiben in `KnowHowToAI.Server`; die verbindliche Feature-, Namespace- und Teststruktur steht in einem eigenen Strukturkonzept |
| K-015 | Keine kostenpflichtigen Komponenten; jede direkte und transitive Abhängigkeit muss kostenlos nutzbar und mit der MIT-Distribution vereinbar sein, Lizenzpflichten werden eingehalten |
| K-016 | Rollierende Detailplanung: M0–M2 werden jetzt ausführungsreif geschärft; M3 und jedes folgende Milestone starten mit einem manuellen `Mx.0`-Planungs- und Konzept-Gate |
| K-017 | WCAG 2.2 AA ist Entwicklungsmaßstab für menschliche Kernworkflows ohne formale Zertifizierung; Agenten prüfen automatisiert headless, die manuelle Tastaturcheckliste führt ein Mensch aus |
| K-018 | Content erlaubt kein ausführbares Raw HTML und keine fremden Bildquellen; Links, Paste, Browserdarstellung und PDF folgen einer zentralen sicheren Markdown-Policy |

Noch offene Punkte: [Offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md).

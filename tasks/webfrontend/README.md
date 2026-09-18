# Webfrontend und Wissensplattform

Stand: 2026-09-18

Status: Fortlaufendes Zielkonzept; keine Beschreibung des implementierten Ist-Zustands.

## Zweck und Pflege

Dieser Ordner beschreibt Zielbild, Entscheidungen und Umsetzungsreihenfolge für das KnowHowTo-AI-Webfrontend.

- Verbindlicher System-Ist-Stand bleibt [`docs/`](../../docs/README.md).
- Konzeptaussagen werden nicht als bereits implementiertes Verhalten in `docs/` übernommen.
- Relevante Gesprächsergebnisse werden hier fortgeschrieben und automatisch atomar committed.
- Das Konzept wird nur bis zum jeweils aktuellen Detailplanungshorizont ausführungsreif geschärft. Aktuell umfasst dieser Horizont M0 bis einschließlich M2; dieser Bereich ist seit 2026-09-18 entschieden, abschließend geprüft und zur sequenziellen Agentenausführung freigegeben.
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
| K-019 | Im PoC gibt es keinen verbindlichen externen MCP-Zielclient; M1 wird automatisiert über den offiziellen SDK-Client abgenommen, Hermes bleibt ein optionaler Eval-Smoke |
| K-020 | M2 verwendet den Namen „KnowHowToAI“ als reine Textwortmarke ohne Logo-Dummy, ein neutrales Blau und Systemschriften; die UI ist zunächst ausschließlich deutsch ohne Lokalisierungsinfrastruktur |
| K-021 | Allgemeine UI-Basis ist ausschließlich natives Blazor, semantisches HTML und eigenes CSS; Fluent UI, MudBlazor oder eine andere allgemeine Komponentenbibliothek werden in M1–M8 nicht erneut bewertet oder eingeführt |
| K-022 | Der Knowledge Tree wird nativ umgesetzt: serverseitiges Cursor-Paging mit exakt 100 Einträgen pro Seite, höchstens zehn gleichzeitig gehaltene Seiten und kein vorgeladener Gesamtbaum; Radzen ist mangels öffentlicher Tree-Virtualisierungs-/Cursor-Paging-API ausgeschlossen |
| K-023 | Rich-Text-Editor ist Milkdown `@milkdown/crepe` `7.22.1`; Tiptap ist trotz bestandener Technikprüfung wegen der Beta-Markdown-Erweiterung ausgeschlossen |
| K-024 | Razor-Komponententests verwenden bUnit `2.11.3` mit xUnit v3 `3.2.2`; Browser-E2E verwendet Microsoft.Playwright .NET `1.62.0` ausschließlich headless mit `Channel = "chrome"` gegen die installierte aktuelle Google-Chrome-Stable-Version |
| K-025 | Vitest und eine separate JavaScript-Testtoolchain werden nicht vorsorglich eingeführt; sie werden erst bei eigener zustandsbehafteter JavaScript-/TypeScript-Logik mit Verzweigungen, Transformationen oder Retry-/Lifecyclelogik erforderlich |
| K-026 | Die ignorierten Verzeichnisse unter `temp/webfrontend-spikes/` bleiben uncommittete Referenz-Fixtures; sie sind weder Produktionscode noch kopierbare Implementierungsvorlagen oder dauerhafte Testprojekte |

Noch offene Punkte: [Offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md).

# Entscheidungsworkshop: Arbeitsstand und Fortsetzung

Stand: 2026-09-18

Status: aktiv; aktueller Planungshorizont M0–M2. Von den offenen Punkten werden jetzt nur die dafür notwendigen Entscheidungen bearbeitet.

## Verbindlichkeit

- Die verbindliche Liste steht in [Offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md).
- Dieses Dokument ist das Arbeits- und Übergabeprotokoll: Empfehlungen, Begründungen, Recherche und Gesprächsreihenfolge.
- Empfehlungen sind keine Entscheidungen. Erst eine ausdrückliche Benutzerantwort schließt einen Punkt.
- Nach jeder Entscheidung: Ergebnis in das fachlich zuständige Konzept übernehmen, Eintrag aus `07-entscheidungen-und-offene-fragen.md` entfernen, betroffene Roadmap-Voraussetzungen aktualisieren und atomar committen.
- Technische Spike-Punkte werden nicht durch Bauchgefühl geschlossen; der zuständige M0-/M8-Task liefert Prototyp, Messung, Lizenzprüfung, Entscheidung und verworfene Alternativen.

## Wiedereinstieg

1. `git status` prüfen.
2. Dieses Dokument und [Offene Fragen](konzept/07-entscheidungen-und-offene-fragen.md) lesen.
3. Mit O-028 fortfahren: Copyrightinhaber der fehlenden MIT-Lizenzdatei.
4. Pro Gesprächsschritt genau eine zusammenhängende Benutzerentscheidung für M0–M2 behandeln.
5. Empfehlung, Konsequenzen und betroffene Tasks nennen; keine unnötige Technikfrage an den Benutzer delegieren.
6. Antwort sofort dokumentieren und committen, bevor der nächste Block beginnt.

Erste noch unbeantwortete Frage:

> Unter welchem vollständigen Namen oder Firmennamen soll der Copyright-Hinweis der MIT-`LICENSE` geführt werden?

## Entscheidungsreihenfolge für M0–M2

| Block | IDs | Ziel |
|---|---|---|
| A – Auswahlrahmen | O-028, O-013, O-021, O-020 | Lizenzdatei, Zielbrowser, Barrierefreiheit und sichere Contentdarstellung vor Komponenten-Spikes festlegen |
| B – Technische Spikes | O-001, O-003, O-002, O-015 | UI-Paket, Tree, Editor und Testwerkzeuge evidenzbasiert auswählen |
| C – Host und MCP | O-022, O-019 | produktive Datenbankidentität und reale HTTP-MCP-Clients festlegen |
| D – Theme und Sprache | O-009, O-014 | Branding und UI-Sprache für M2 definieren |

Die übrigen offenen Fragen werden in den manuellen `Mx.0`-Gates ab M3 bearbeitet und nicht jetzt vorsorglich entschieden.

## Technische Vorbewertung vom 2026-09-17

### UI-Komponentenbasis – O-001

Startreihenfolge für M0.3-T1:

1. **Native Blazor-/HTML-/CSS-Basis** zuerst prototypisieren.
   - Basisshell, Formulare und Gestaltung ohne allgemeines UI-Paket abdecken.
   - Kleines, transparentes CSS und Frameworkfunktionen vor zusätzlicher Abhängigkeit verwenden.
2. **Microsoft Fluent UI Blazor** und **MudBlazor** nur als OSS-Vergleich heranziehen, wenn der Prototyp einen konkreten Mehrwert für wiederkehrende komplexere Controls zeigt.
   - Lizenz einschließlich transitiver Abhängigkeiten und NOTICE-Pflichten prüfen.
   - Paket-/Bundle-Gewicht, JS-Interop, Buildaufwand, Theme-Einschränkungen und Interactive-Server-Verhalten gegen die native Lösung messen.
3. **Kommerzielle, kostenpflichtige oder nutzungsbeschränkte Suites** sind durch O-018 ausgeschlossen.

Nicht vor dem Spike festlegen. „Keine allgemeine Komponentenbibliothek“ ist ein valides Ergebnis. Abnahme muss Layout, Dialoge, Formulare, Tabellen, Benachrichtigungen, Theme, Tastatur, WCAG-Ziel, Testbarkeit, Interactive Server und Self-Hosting statischer Ressourcen umfassen.

Quellen:

- [Microsoft Fluent UI Blazor](https://github.com/microsoft/fluentui-blazor)
- [MudBlazor Releases](https://github.com/MudBlazor/MudBlazor/releases)
- [MudBlazor .NET-10-Target](https://github.com/MudBlazor/MudBlazor/blob/dev/CONTRIBUTING.md)

### Knowledge Tree – O-003

Der Tree ist das größte Komponentenrisiko. Er muss gleichzeitig bieten:

- serverseitiges Root-/Children-Paging mit opaken Cursors;
- echtes Nachladen pro Expand, keine Vorabübertragung des Gesamtbaums;
- stabile Node-Identitäten und kontrollierten Expand-/Selection-State;
- Drag-and-drop mit Parent, Before und After als eindeutiger Zielposition;
- Tastaturalternative für Move/Reorder;
- tiefe und breite Strukturen, Fokusführung und WCAG-Ziel;
- Blazor Interactive Server ohne interne Web-API-Pflicht.

Aktuelle Bewertung:

- Die technische Vorrecherche zu **Syncfusion TreeView** und **Telerik TreeView** dient nur noch als Anforderungsspiegel. Beide kommerziellen Produktlinien sind durch O-018 als Abhängigkeit ausgeschlossen; auch kostenlose Sonderprogramme oder Community-Lizenzen ändern den grundsätzlichen Auswahlrahmen nicht.
- Fokussierte permissiv lizenzierte OSS-Komponenten und eine eigene schlanke Tree-Darstellung bleiben Kandidaten. Gewählt wird nur eine OSS-Komponente, die Kernanforderungen ohne umfangreiche Anpassung erfüllt und gegenüber einer Eigenlösung materiellen Mehrwert belegt.
- Der Spike muss insbesondere echtes serverseitiges Paging beweisen; reine Rendering-Virtualisierung eines zuvor vollständig geladenen Baums genügt für 100.000 Nodes nicht.

Quellen:

- [Syncfusion Load-on-demand](https://blazor.syncfusion.com/documentation/treeview/data-binding)
- [Syncfusion Virtualisierung](https://blazor.syncfusion.com/documentation/treeview/virtualization)
- [Syncfusion Drag-and-drop](https://blazor.syncfusion.com/documentation/treeview/drag-and-drop)
- [Syncfusion Accessibility](https://blazor.syncfusion.com/documentation/treeview/accessibility)
- [Telerik Load-on-demand](https://www.telerik.com/blazor-ui/documentation/components/treeview/data-binding/load-on-demand)
- [Telerik Drag-and-drop](https://www.telerik.com/blazor-ui/documentation/components/treeview/drag-drop)
- [Telerik Accessibility](https://www.telerik.com/blazor-ui/documentation/components/treeview/accessibility/wai-aria-support)

### Markdown-Rich-Text-Editor – O-002

Startreihenfolge für M0.3-T3:

1. **Milkdown** zuerst prüfen.
   - Markdown-first, WYSIWYG, ProseMirror/remark, MIT, aktiv gepflegt.
   - Upload-Plugin unterstützt Auswahl, Drop/Paste und einen eigenen Uploader; Data-URL-Default muss ersetzt werden.
   - Erfordert lokal gebündelte JavaScript-/TypeScript-Artefakte und eine schmale JS-Isolation für Blazor.
2. **Tiptap** nur als Vergleich.
   - Sehr aktive Editorbasis.
   - Bidirektionale Markdown-Erweiterung ist weiterhin Beta und dokumentiert Verlust-/Grenzfälle; deshalb derzeit kein Startfavorit.
3. **Toast UI Editor** nur als Wartungs-/Fallbackvergleich.
   - Markdown- und WYSIWYG-Modus sowie Image-Hook vorhanden.
   - Vor Auswahl aktuelle Wartungsaktivität und Roundtripqualität neu belegen.

Zwingende Spike-Golden-Master:

- Absätze, Hervorhebungen, Links, geordnete/ungeordnete Listen;
- Tabellen, Inline-Code, fenced Code Blocks, Blockquotes und Unicode;
- mehrfaches Öffnen/Speichern ohne semantische Drift;
- deaktivierte Heading-Erzeugung und erkennbare serverseitige Ablehnung;
- Paste aus Browser, Office und reinem Text gemäß O-020;
- Bilder/Upload-Hooks vorbereiten, aber erst in M8 aktivieren;
- große Inhalte innerhalb der fachlichen Limits;
- ungespeicherter Zustand, Fokus, Dispose/Reconnect und Nodewechsel.

Quellen:

- [Milkdown](https://github.com/Milkdown/milkdown)
- [Milkdown Upload-Plugin](https://milkdown.dev/docs/api/plugin-upload)
- [Tiptap Markdown](https://tiptap.dev/docs/editor/markdown)
- [Toast UI Editor](https://github.com/nhn/tui.editor)

### Testwerkzeuge – O-015

Klare Startempfehlung:

- **bUnit mit xUnit v3** für Razor-Komponenten, Rendering, Events, DI und kontrolliertes JS-Interop.
- **Microsoft Playwright .NET mit xUnit v3** für reale Browserabläufe, SignalR/Reconnect, JS-lastige Fremdkomponenten, Downloads und Accessibility-Smokes.
- Browser-E2E bleibt klein; fachliche Varianten gehören in Core-/Komponententests.
- Playwright verwendet web-first Assertions und Rollen-/Label-/Test-ID-Locators; keine festen Wartezeiten.

Quellen:

- [Microsoft: Blazor testen](https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0)
- [bUnit xUnit-v3-Projekt](https://bunit.dev/docs/getting-started/create-test-project.html)
- [Playwright .NET](https://playwright.dev/dotnet/docs/library)
- [Playwright Browser](https://playwright.dev/dotnet/docs/browsers)

### HTTP-MCP-Zielclients – O-019

Vorbefund:

- Cursor dokumentiert Streamable HTTP.
- Hermes dokumentiert URL-basierte HTTP-/Streamable-HTTP-Server.
- Für Claude/Codex und weitere tatsächlich genutzte Clients ist die konkrete Produktvariante und Version zu benennen und praktisch abzunehmen; Produktfamilienname allein genügt nicht.
- Mindestabnahme je Client: Verbindung, Initialize/Discovery, Toolliste, langer Read, strukturierter Fehler, Begin/Mutation/Validate/Commit sowie Reconnect/Timeout.

Quellen:

- [Cursor MCP](https://docs.cursor.com/context/model-context-protocol)
- [Hermes MCP-Konfigurationsreferenz](https://github.com/NousResearch/hermes-agent/blob/main/website/docs/reference/mcp-config-reference.md)

## Getroffene Benutzerentscheidungen

| ID | Entscheidung | Umsetzungsauswirkung |
|---|---|---|
| O-018 | Keine kostenpflichtigen Komponenten. Direkte und transitive Abhängigkeiten müssen kostenlos nutzbar und mit der MIT-Distribution vereinbar sein. Einfache UI/CSS wird pragmatisch selbst umgesetzt; spezialisierte OSS-Komponenten nur bei belegtem Mehrwert. | Kommerzielle Suites entfallen; Lizenzprüfung bleibt Pflicht; „keine allgemeine UI-Bibliothek“ ist für O-001 zulässig. |

## Offene Benutzerentscheidungen mit Empfehlung

### Block A – Auswahlrahmen

| ID | Zu entscheiden | Empfehlung | Konsequenz |
|---|---|---|---|
| O-028 | Copyrightinhaber der MIT-Lizenz | vollständigen Namen der natürlichen oder juristischen Person verwenden, welche die Rechte hält; Startjahr 2026 | ermöglicht eine eindeutige Root-`LICENSE` und die Lizenzbaseline in M0.1-T2 |
| O-013 | Browser, Versionen, Viewports | Edge Stable verbindlich, Chrome Stable kompatibel; automatisiert Edge/Chromium; volle Bearbeitung ab 1280×720, lesbar ab 1024 px; kein Mobile/Safari/Firefox im ersten Stand | begrenzt CSS, Testmatrix und Komponentenwahl realistisch |
| O-021 | Accessibility | WCAG 2.2 AA für Kernworkflows, axe-Smokes und manuelle Tastaturabnahme; keine formale Zertifizierung im ersten Stand | verhindert spätere unplanbare Nachrüstung |
| O-020 | Raw HTML, Links, Bilder, Paste | Raw HTML nicht ausführen; sichere URL-Schemata; externe Bilder nicht automatisch laden; Paste auf erlaubtes Markdown reduzieren | schützt Browser/PDF vor XSS, Tracking und lokalen/externen Ressourcenzugriffen |

### Block C – Host und MCP

| ID | Zu entscheiden | Empfehlung | Konsequenz |
|---|---|---|---|
| O-022 | produktive SQL-Identität/Secrets | Windows-Servicekonto mit integrierter SQL-Authentifizierung und minimalen Rechten; keine produktiven Kennwörter im Repository | kann bestehende Konfigurationsregel und Deploymentdoku ändern |
| O-019 | reale Clients und Versionen | täglich verwendete Cursor-, Claude-, Hermes-, Codex-Varianten explizit nennen und testen | der STDIO-Hard-Cut erfolgt erst nach ihrer grünen Abnahme |

### Block D – Produkt- und Arbeitsverhalten

| ID | Zu entscheiden | Empfehlung | Konsequenz |
|---|---|---|---|
| O-009 | Name, Logo, Farbe, Schrift | „KnowHowTo AI“, zunächst Textlogo, neutrales Blau, Systemschrift; später zentral austauschbar | schließt Theme- und PDF-Grundbranding |
| O-014 | Sprache/Lokalisierung | UI zunächst ausschließlich Deutsch; Texte zentral halten, keine vollständige Lokalisierungsinfrastruktur | kleine Oberfläche ohne verstreute Literaltexte |
| O-008 | initiale Rolle | keine stille fachliche Defaultrolle; letzte Wahl nur im Browsertab merken und bei Einstieg sichtbar bestätigen | verhindert unbemerkte Rollenauflösung |
| O-007 | Transaction Actor ohne Auth | beim Beginnen frei eingebbar, pro Browsertab vorbefüllt, danach immutable; `Client` fest auf Webclient setzen | liefert nachvollziehbare Metadaten ohne vorgetäuschte Identität |
| O-025 | mehrere Clients in derselben Transaction | zulassen, keine Locks; `ChangeVersion` bei jeder Mutation, stale Write ablehnen und neu laden | UI und MCP bleiben gleichwertige Clients ohne Lockverwaltung |
| O-026 | offene Transactions | kein automatischer Ablauf/keine automatische Löschung; Alter anzeigen, explizit committen/discarden | kein stiller Wissensverlust; alte Transactions bleiben sichtbar |
| O-027 | Undo | Editor-Undo nur vor dem Speichern; kein globaler Undo-Stack; persistierte Korrektur als Gegenmutation oder gesamtes Discard | vermeidet komplexes Event-Sourcing-/Undo-Subsystem |

### Block E – Betrieb und Last

| ID | Zu entscheiden | Empfehlung | Konsequenz |
|---|---|---|---|
| O-012 | OS, Servicehost, Proxy, Hostname, TLS | Windows Service hinter vorhandenem IIS/Unternehmensproxy; ein HTTPS-Origin; konkrete Infrastruktur des Benutzers erfragen | bestimmt Hosting-, Proxy- und Zertifikatsabnahme |
| O-017 | Daten- und Parallelitätsprofil | 100.000 Nodes, 1.000 direkte Kinder, 20 Circuits, 10 parallele MCP-Aufrufe; P95 Server ≤ 500 ms, sichtbare UI-Reaktion ≤ 1 s | liefert messbare M6-Abnahme statt „schnell genug“ |
| O-023 | RPO/RTO/Aufbewahrung | RPO ≤15 min, RTO ≤4 h, tägliches Full plus Log-Backups, 30 tägliche/12 monatliche Restorepunkte | definiert echten Wissensbunkerbetrieb |
| O-024 | Logs, Health, Alarmierung | strukturierte Rolling Files 30 Tage, kritische Ereignisse zusätzlich Windows Event Log, `/health/live` und `/health/ready`, zunächst keine Metrikplattform | ermöglicht Betrieb ohne vorsorglichen Observability-Stack |

### Block F – niedrige Priorität

| ID | Zu entscheiden | Empfehlung | Konsequenz |
|---|---|---|---|
| O-010 | Markdown-Quellmodus | aufnehmen; kontrollierter Wechsel, gleiche Validierung und Dirty-State | gibt Experten Zugriff auf das kanonische Format |
| O-011 | PDF-Basislayout | A4, kein Deckblatt, Inhaltsverzeichnis ab zwei Ebenen, Logo im Header, Seitenzahl im Footer | hält ersten Export klein und brauchbar |
| O-004 | Assetspeicher | im M8-Spike SQL-Metadaten plus getrennten immutable Binärspeicher gegen SQL-Varbinary bewerten; Backup/Mehrinstanzbetrieb mitentscheiden | keine vorzeitige Speicherfestlegung |
| O-016 | Assetgrenzen | zunächst PNG/JPEG/WebP, 10 MiB, 40 MP; SVG aus Sicherheitsgründen nicht im ersten Stand | reduziert Sanitization- und Active-Content-Risiko |

### Block G – spätere Vorhaben

| ID | Empfehlung |
|---|---|
| O-005 | Presentation Views nicht in die aktuelle Roadmap ziehen; kanonischer Baum und stabile `NodeId`s erhalten die Option |
| O-006 | integrierte KI erst nach stabilen manuellen Workflows als eigenes Vorhaben konzipieren |

## Noch zu recherchieren oder im Spike zu beweisen

- Lizenzinventar aller direkten und transitiven Abhängigkeiten jedes in die engere Wahl genommenen OSS-Kandidaten einschließlich einzuhaltender Copyright-/NOTICE-Pflichten.
- Native Blazor-/HTML-/CSS-Basis versus gegebenenfalls Fluent UI oder MudBlazor mit realem Interactive-Server-Prototyp, Theme, Accessibility und messbarem Abhängigkeitsgewicht.
- Tree-Paging mit 1.000 Geschwistern und 100.000 Gesamt-Nodes; keine Scheindemonstration mit vollständig geladenem In-Memory-Baum.
- Milkdown-Roundtrip gegen den tatsächlich erlaubten Markdownumfang und Blazor-JS-Lifecycle.
- Paste-Sanitization und Raw-HTML-Verhalten nach O-020.
- Aktuelle Streamable-HTTP-Unterstützung jeder konkret genannten Claude-/Codex-/Hermes-/Cursor-Version.
- Produktive Zielumgebung, SQL-Edition, vorhandener Proxy, Zertifikatsprozess, Backupplattform und Betriebsverantwortung.
- Reale Größenordnung von Nutzern, Wissensbestand, Änderungsrate und Wiederherstellungsanforderung zur Kalibrierung von O-017/O-023.

## Bereits verbindlich und nicht erneut zu diskutieren

- Blazor Interactive Server, eine EXE und standardmäßig ein Port.
- HTTP-MCP ersetzt STDIO vollständig; kein dauerhafter Doppelbetrieb.
- Keine allgemeine REST-/OpenAPI-API ohne konkreten Integrationsfall.
- Keine Authentifizierung/Autorisierung in dieser Roadmap; Betrieb nur im freigegebenen Intranet.
- Kein Autosave; explizites Speichern und explizite Transactions.
- TODO-Texte sind normaler Content ohne Sondermodell oder Exportfilter.
- PDF und Assets bleiben niedrig priorisiert; Assets folgen nach dem einfachen PDF-Export.
- Markdown ist kanonisch; Dokumentstruktur liegt im Node-Baum und Headings sind im gespeicherten Content verboten.
- Keine kostenpflichtigen Komponenten; kleine UI-/CSS-Lösungen vor umfangreichen Bibliotheken, spezialisierte OSS-Komponenten nur bei belegtem Mehrwert und kompatibler Lizenz.

## Relevante Konzept-Commits

| Commit | Inhalt |
|---|---|
| `b901807` | Roadmap in ausführbare Agent-Tasks gegliedert |
| `dbd9db8` | STDIO-Ziel vollständig durch HTTP-MCP ersetzt |
| `e716dc6` | offene Fragen und Prioritäten bereinigt |
| `579eaf8` | Agentenausführung, Abnahmen und verbindliche Projekt-/Klassenstruktur präzisiert |
| `f574c1a` | Agentenregeln auf Web-/HTTP-Ziel vorbereitet |
| `bd7c09e` | Agentenregeln tokenarm verdichtet |
| `f590981` | 360°-Audit: fehlende Produkt-, Sicherheits- und Betriebsentscheidungen ergänzt |

Diese Commits sind bereits abgeschlossen und werden nicht erneut umgesetzt. Der nächste Workshop-Schritt verändert ausschließlich Entscheidungen, die der Benutzer ausdrücklich beantwortet.

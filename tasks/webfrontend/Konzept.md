# Konzept: Webfrontend und Wissensplattform

Stand: 2026-09-17

Status: Diskussionsgrundlage; keine Beschreibung des implementierten Ist-Zustands

Pflege: Dieses Dokument wird bei fachlich relevanten Gesprächen fortgeschrieben. Änderungen werden automatisch atomar committed.

## 1. Vision

KnowHowTo AI wird von einem reinen MCP-Server zu einer Wissensplattform für Menschen und Agenten.

- Die Datenbank ist der zentrale, versionierte **Wissensbunker**.
- Menschen pflegen, strukturieren, prüfen und publizieren Wissen über ein Webfrontend.
- Programmieragenten in Cursor, Claude, Hermes oder anderen MCP-fähigen Clients lesen und bearbeiten dasselbe Wissen über MCP.
- Agenten sollen bei ausreichendem Wissensstand primär gezielt lesen; unkontrollierte Neuerzeugung und Wissensduplikate werden vermieden.
- Wissen bleibt ohne Agent editierbar. Agenten sind Beschleuniger, nicht Voraussetzung für Betrieb oder Datenpflege.
- Später kann KI-Orchestrierung, z. B. mit Semantic Kernel, in das Webfrontend integriert werden. Sie ist keine Voraussetzung des ersten Frontends.

## 2. Problem und Nutzen

Der MCP-Server bietet sichere fachliche Primitive, aber keine menschlich erfassbare Gesamtsicht. Ausschließlich agentische Bedienung reicht nicht aus:

- Struktur, Umfang und Zustand der Wissensbasis sind schwer überschaubar.
- Manuelle Nachbearbeitung ist unnötig indirekt.
- Offene Transactions, Releases, historische Snapshots, Rollenauflösung, Stale-Zustände und Qualitätswarnungen benötigen visuelle Arbeitsoberflächen.
- Strukturänderungen wie Verschieben und Sortieren von Nodes sind visuell einfacher und sicherer.
- Publikationsfähige Dokumente für definierte Zielgruppen benötigen einen reproduzierbaren, konfigurierbaren Exportprozess.

Das Frontend macht den Wissensbestand sichtbar, direkt pflegbar und publizierbar, ohne die agentische Nutzung zu schwächen.

## 3. Bereits gesetzte Produktentscheidungen

### 3.1 Aus diesem Vorhaben

- Es wird ein Webfrontend geben.
- Webfrontend und Agenten arbeiten auf demselben Datenbestand.
- Die Datenbank bleibt die zentrale Wissensquelle.
- Menschen können Nodes ohne Agent erstellen, verschieben, sortieren, bearbeiten und speichern.
- Das Frontend zeigt mindestens Strukturen, Nodes, Transactions und Releases.
- Redaktionelle Hinweise wie `TODO Besser formulieren` müssen erfassbar und später durch einen Agenten bearbeitbar sein.
- Rollenbezogene Teilbäume sollen mit einem Klick als gestaltetes PDF exportierbar sein, z. B. `Kundenanpassungen / Max Schulze GmbH` für die Rolle `Endkunde`.
- PDF-Ausgaben benötigen definierbares Layout, Inhaltsverzeichnis, Logo und weitere Publikationsparameter.
- Integrierte KI ist eine spätere Option, kein Bestandteil des ersten Frontends.
- Das Frontend wird als Blazor Web App umgesetzt.
- Es entsteht eine vollumfängliche Weboberfläche für die KnowHowTo-AI-Datenbank, keine reine Admin- oder Zusatzansicht.
- Der Zielbetrieb ist ein zentraler Server im vertrauenswürdigen Firmennetz; mehrere Menschen und Agenten greifen darauf zu.
- Authentifizierung und Autorisierung sind nicht Bestandteil des ersten Umsetzungsschritts.
- Etablierte Standardkomponenten werden verwendet, wenn sie die Anforderungen erfüllen; allgemeine UI-, Editor-, Baum-, Upload- und PDF-Technik wird nicht selbst entwickelt.
- Ein Rich-Text-/WYSIWYG-Editor mit Markdown als kanonischem Speicherformat und Bildunterstützung ist vorgesehen.
- Blazor-UI, allgemeine HTTP-API und MCP sollen langfristig in einem ASP.NET-Core-Host mit einer gemeinsamen Konfiguration betrieben werden.

### 3.2 Aus dem implementierten Kern

Diese Leitplanken bleiben erhalten:

- Eine globale Node-Hierarchie mit stabilen `NodeId`s.
- Rollenabhängiger Content bei gemeinsamer Hierarchie.
- Node-Titel bilden die Dokumentstruktur; gespeicherter Markdown-Content enthält keine Überschriften.
- Alle Änderungen am versionierten Wissenszustand erfolgen in einer KnowHowTo-AI-Transaction.
- Working Snapshots können validiert, committed oder verworfen werden.
- Committed Snapshots sind unveränderlich; konkurrierende Commits erzeugen `SnapshotConflict`.
- Releases sind benannte, unveränderliche Verweise auf committed Snapshots.
- Rollen-Fallback, Content-Provenienz und transitive Stale-Erkennung bleiben transparent.
- Reads sind metadata-first und paginiert; das Frontend darf nicht standardmäßig komplette Bäume und Contents laden.
- MCP STDIO ist ein Adapter. Fachlogik bleibt in Domain und Application; ein Webadapter greift auf dieselben Application Services zu.
- Content-Rollen sind Zielgruppenrollen, keine Benutzerrechte.

## 4. Produktprinzipien

1. **Eine fachliche Wahrheit:** Keine Geschäftslogik in Browser, MCP-Handler oder Web-Endpunkten duplizieren.
2. **Menschen und Agenten als gleichwertige Clients:** Beide verwenden dieselben Regeln, Transactions, Validatoren und Identitäten.
3. **Expliziter Arbeitsstand:** Die Oberfläche zeigt jederzeit, ob Current Snapshot, historischer Snapshot oder Working Transaction betrachtet wird.
4. **Sicheres Editieren:** Kein stilles Direkt-Speichern in den Current Snapshot; jeder Edit gehört zu einer sichtbaren Transaction.
5. **Reproduzierbare Publikation:** Offizielle Exporte referenzieren einen committed Snapshot oder Release sowie eine versionierte Publikationsdefinition.
6. **Progressive Disclosure:** Erst Struktur und Metadaten, Content nur bei Bedarf.
7. **Intranet-first:** Der reguläre Betrieb erfolgt zentral im Firmennetz; lokale Entwicklung und Tests bleiben möglich.
8. **KI optional:** Alle Kernworkflows funktionieren deterministisch ohne LLM.
9. **Standardkomponenten vor Eigenbau:** Etablierte Komponenten werden anhand fachlicher Eignung, Wartung, Lizenz, Barrierefreiheit und Integrationskosten ausgewählt.
10. **Offene Integrationsgrenzen:** Browser, n8n und MCP-Clients verwenden explizite, dokumentierte Endpunkte desselben Hosts.

## 5. Zielgruppen und Hauptabläufe

| Akteur | Hauptaufgaben |
|---|---|
| Wissensautor | Navigieren, suchen, Nodes anlegen/verschieben, Content bearbeiten, Hinweise erfassen |
| Consultant | Fachwissen und Kundenanpassungen pflegen, Rollen-Content ableiten, Publikationen vorbereiten |
| Entwickler | Technisches Wissen gezielt lesen und ergänzen, historische Stände und Diffs prüfen |
| Redakteur | Stale Content, TODOs und Qualitätswarnungen abarbeiten |
| Endkunde | Erhält freigegebene, rollenbezogene PDF-Dokumentation; kein direkter Systemzugriff im ersten Schritt |
| Externer Agent | Liest und schreibt über MCP innerhalb expliziter Transactions |
| Späterer integrierter Agent | Bearbeitet ausgewählte redaktionelle Aufträge aus dem Webfrontend |

## 6. Zieloberfläche

### 6.1 Grundlayout

Empfohlenes Desktop-Layout:

```text
┌ Navigation/Baum ─────┬ Node-Editor/Ansicht ─────┬ Kontext/Status ────┐
│ Suche und Filter     │ Titel, Beschreibung          │ Rolle/Fallback       │
│ lazy Tree           │ Rollen-Content              │ Freshness/Quellen    │
│ Drag-and-drop       │ Markdown + Vorschau         │ Findings/TODOs       │
│ Badges              │ Diff bei Änderung          │ Historie             │
└──────────────────────┴────────────────────────────┴─────────────────────┘
Kontextleiste: Wissensbasis | Snapshot/Transaction | Rolle | ungespeicherte Änderungen
```

Der Selektor für Snapshot/Transaction und Rolle ist global sichtbar. Ein Benutzer darf nie unbemerkt einen historischen oder aufgelösten Fallback-Content bearbeiten.

### 6.2 Dashboard

- Current Snapshot und letzter Release.
- Offene Transactions mit Zweck, Akteur, Alter und Base Snapshot.
- Stale Derived Contents.
- Harte Validierungsfehler und Qualitätswarnungen.
- Offene redaktionelle Hinweise/TODOs.
- Zuletzt geänderte Nodes und Releases.
- Direkte Einstiege: Wissensbaum, Transaction fortsetzen, Release vergleichen, Publikation erzeugen.

### 6.3 Wissensbaum

- Lazy Loading über Root und paginierte Children.
- Suche nach Titel, Beschreibung und rollenaufgelöstem Content.
- Filter nach Rolle, Availability, Freshness, Findings und redaktionellen Hinweisen.
- Badges für eigenen Content, Fallback, fehlenden Content, stale, Warnung und TODO.
- Drag-and-drop für `move_node` und Sortierung; Zielposition vor Ausführung anzeigen.
- Strukturänderungen nur in einer offenen Working Transaction.
- Tiefe Kundenstrukturen werden nicht vollständig vorab geladen.

### 6.4 Node-Ansicht und Editor

- Struktur: Titel, Description, Parent, SortOrder, stabile NodeId.
- Rollen-Tabs oder Rollen-Selektor mit Kennzeichnung `Explicit`, `Fallback`, `None`.
- Anzeige von `requestedRole`, `resolvedRole`, Content Mode, Revision und Freshness.
- Rich-Text-/WYSIWYG-Editor mit Markdown als kanonischem Ein-/Ausgabeformat.
- Die verwendete Standardkomponente muss Markdown verlustarm roundtrippen; ein HTML-first-Editor mit nachträglicher, verlustbehafteter Konvertierung reicht nicht.
- Heading-Funktionen werden deaktiviert; verbotene Markdown-/HTML-Headings werden unmittelbar markiert und serverseitig weiterhin abgelehnt.
- Formatierungen, Links, Listen, Tabellen, Code, Zitate und Bilder werden über bedienbare Editorfunktionen angeboten.
- Ein optionaler Markdown-Quellmodus bleibt für präzise Nachbearbeitung erhalten.
- Vorschau erzeugt die Node-Überschrift aus der Struktur, nicht aus `ContentMd`.
- Derived Content zeigt seine Source-Revisions und deren aktuellen/stale Zustand.
- Versionsvergleich zum Base Snapshot und zu auswählbaren historischen Snapshots.
- Explizite Aktion zum Löschen von Rollen-Content, getrennt vom globalen Löschen eines Nodes.

### 6.5 Bilder und Assets

- Bilder werden über Upload, Drag-and-drop und Zwischenablage in den Editor eingefügt.
- Keine Data-URLs und keine unkontrollierten lokalen Dateipfade im Markdown.
- Markdown referenziert stabile Asset-Identitäten bzw. vom Server auflösbare URLs.
- Assets besitzen mindestens ID, Revision/Hash, MIME-Type, Dateiname, Größe und Erstellungsmetadaten.
- Assets sind unveränderlich oder revisioniert und werden in Snapshots/Releases reproduzierbar referenziert.
- Browseransicht, REST-Ausgabe, MCP, HTML-Vorschau und PDF-Export verwenden dieselbe Asset-Auflösung.
- Das konkrete Speichermedium wird separat entschieden. Große Binärdaten dürfen nicht bei jeder Snapshot-Kopie dupliziert werden.

### 6.6 Transaction-Arbeitsbereich

- Transaction beginnen oder vorhandene Transaction fortsetzen.
- Zweck, Akteur, Client und Commit Message sichtbar pflegen.
- Strukturierter Netto-Diff nach Rollen, Resolution Orders, Nodes, Contents und Dependencies.
- Validierung mit Fehlern, Warnungen, Stale Contents und Refactoring-Kandidaten.
- Commit erst nach bewusster Diff- und Validation-Sicht; Warnungen blockieren nicht automatisch.
- Discard mit klarer Auswirkung auf den Working Snapshot.
- Bei `SnapshotConflict`: betroffenen Base/Current-Stand zeigen; kein vorgetäuschtes automatisches Merge.

### 6.7 Historie und Releases

- Snapshots mit Zustand, Zeit, zugehöriger Transaction und Metadaten auflisten.
- Beliebige committed Snapshots vergleichen.
- Releases anzeigen und auf ihren Snapshot navigieren.
- Release aus einem committed Snapshot anlegen.
- Node-Historie als aus Snapshot-Diffs abgeleitete Sicht; kein erfundenes Operation Log.

### 6.8 Rollenverwaltung

- Rollen erstellen, umbenennen und löschen.
- Resolution Order per sortierbarer Liste bearbeiten.
- Auswirkung auf Fallback transparent vorschauen.
- Änderungen erfolgen wie andere Wissensänderungen in einer Transaction.
- Keine Vermischung mit Authentifizierung, ACL oder Mandantenrechten.

## 7. Redaktionelle Hinweise und Agentenaufträge

Ein Text wie `TODO Besser formulieren` darf nicht unkontrolliert in Endkundenexporte gelangen. Deshalb wird zwischen Fachinhalt und Redaktionsmetadaten unterschieden.

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

- Im Editor erscheinen Hinweise direkt beim betroffenen Node bzw. Rollen-Content.
- Hinweise sind such- und filterbar.
- Agenten können offene Hinweise gezielt abfragen und in einer Transaction bearbeiten.
- Das Auflösen eines Hinweises und die fachliche Inhaltsänderung müssen nachvollziehbar zusammengehören.
- PDF-Exporte enthalten standardmäßig keine redaktionellen Hinweise.

Offen ist, ob Hinweise Teil des versionierten Snapshots oder separates Workflow-Metadatum sind. Empfehlung: zunächst separat versionierbar bzw. historisierbar modellieren, aber nicht als `ContentMd`; die genaue Konsistenzsemantik ist vor Implementierung festzulegen.

## 8. Publikation und PDF-Export

### 8.1 Anwendungsfall

Beispiel:

```text
Teilbaum: Kundenanpassungen / Max Schulze GmbH
Rolle: Endkunde
Stand: Release 2026.09
Profil: Max-Schulze-Endkundendokumentation
Ausgabe: PDF
```

Ein Klick erzeugt ein reproduzierbares Dokument aus dem gewählten Wissensstand.

### 8.2 Publikationsprofil

Ein Publikationsprofil definiert mindestens:

- Name und Dokumenttitel.
- Root Node bzw. Auswahl mehrerer Teilbäume.
- angefragte Rolle und Verhalten bei Fallback/fehlendem Content.
- Snapshot oder Release als Datenquelle; Vorschauen dürfen optional eine Working Transaction verwenden.
- Logo, Farben, Schriften, Seitenformat, Ränder, Kopf-/Fußzeilen.
- Deckblatt, Inhaltsverzeichnis und Seitennummerierung.
- maximale bzw. dargestellte Hierarchietiefe.
- Ein-/Ausschlussregeln für leere Struktur-Nodes, stale Content und Findings.
- Ausgabeformat und Dateinamensschema.

Empfehlung: Publikationsprofile und verwendete Assets versionieren oder mindestens unveränderlich referenzieren. Nur dann ist ein später erneut erzeugtes Release-PDF reproduzierbar.

### 8.3 Pipeline

```text
Snapshot/Release
  → rollenbezogene Baumauflösung
  → neutrales Dokumentmodell
  → Template/Layout
  → HTML-Vorschau
  → PDF-Renderer
  → Exportprotokoll mit Eingangsstand und Findings
```

Das neutrale Dokumentmodell trennt Wissensselektion von PDF-Technik. Weitere Formate wie HTML oder DOCX können später denselben aufgelösten Stand verwenden.

### 8.4 Freigabe

- Arbeitsvorschau: Working Transaction zulässig, deutlich gekennzeichnet.
- Offizielle Publikation: committed Snapshot oder Release.
- Empfohlene optionale Policy: Export blockieren oder bestätigen lassen, wenn ausgewählter Content stale ist, harte Findings besitzt oder redaktionelle Hinweise offen sind.
- Erzeugte Datei enthält maschinenlesbar oder im Exportprotokoll: SnapshotId, Release, Profilversion und Erzeugungszeit.

## 9. Kundenwissen, Rollen und Sicherheit

Eine Hierarchie wie `Kundenanpassungen / Max Schulze GmbH` passt fachlich in das bestehende globale Node-Modell. Dabei sind drei Konzepte getrennt zu halten:

| Konzept | Bedeutung |
|---|---|
| Hierarchie | Fachliche Ablage und Navigation des Kundenwissens |
| Content-Rolle `Endkunde` | Zielgruppengerechte Darstellung |
| Berechtigung/Mandant | Wer Wissen lesen oder ändern darf |

Die Content-Rolle `Endkunde` schützt keine Kundendaten. Für den ersten Schritt ist der Betrieb ohne Authentifizierung entschieden. Daraus folgt eine harte Betriebsgrenze: ausschließlich in einem kontrollierten, vertrauenswürdigen Firmennetz, keine Veröffentlichung im Internet und kein direkter Endkundenzugriff. Netzwerkzugriff bedeutet sonst Vollzugriff auf Lese- und Schreibendpunkte. Authentifizierung, Autorisierung, Audit und Mandantentrennung werden vor einer Erweiterung dieses Nutzerkreises als eigener Schritt umgesetzt. Die bestehende Alternative für harte Isolation ist eine eigene Datenbank bzw. Serverinstanz pro Wissensbasis.

## 10. Technisches Zielbild

### 10.1 Adapter und Schichten

```text
Browser
  → Blazor Web UI (/)
      → Application Services
          → Domain
              → Repository
                  → SQL Server

n8n / sonstige Systeme
  → REST API (/api/v1, OpenAPI)
      → dieselben Application Services

MCP-Clients
  → MCP Streamable HTTP (/mcp)
      → dieselben Application Services
```

- Das Webfrontend greift nicht direkt auf SQL zu.
- Das Webfrontend steuert weder MCP noch REST per internem HTTP-Loopback an; serverseitige Blazor-Komponenten verwenden dieselben Application Services direkt.
- REST und MCP sind eigenständige Adapter für externe Clients.
- REST-, MCP- und UI-Modelle sind Transport-/Darstellungsverträge und werden von Domain-Typen getrennt.
- Fachliche Validierung bleibt serverseitig; clientseitige Prüfungen verbessern nur die Bedienung.
- Paging, opake Cursors und `ChangeVersion` werden bis in die UI respektiert.

### 10.2 HTTP-Endpunkte und n8n

Blazor Web App, Minimal APIs und MCP-over-HTTP nutzen dasselbe ASP.NET-Core-Endpoint-Routing. Blazor erzeugt jedoch keine fachliche API automatisch. Die drei Oberflächen werden explizit registriert:

```text
/                  Blazor Web App
/api/v1/...        REST/JSON für n8n und andere Integrationen
/openapi/v1.json   generierter OpenAPI-Vertrag
/mcp               MCP Streamable HTTP
/assets/...        kontrollierte Asset-Auslieferung
```

- REST wird als versionierte Minimal API nach Features organisiert.
- OpenAPI wird aus den REST-Endpunkten generiert; eine interaktive API-Oberfläche ist optional.
- n8n verwendet für normale Workflows die REST-API per HTTP Request. MCP ist kein Ersatz für stabile Integrationsendpunkte.
- Eingehende n8n-Aufrufe verwenden REST. Vom Server aktiv ausgelöste n8n-Workflows können später über konfigurierbare Webhooks angebunden werden.
- REST und MCP dürfen vorhandene Application Services nur mappen und delegieren; keine doppelte Geschäftslogik.
- Transaktionale Schreibendpunkte erwarten eine explizite `TransactionId`; ein HTTP-Request wird nicht zu einer lang laufenden SQL-Transaction.

### 10.3 MCP-Transport

STDIO war für den lokalen V1-MCP-Server fachlich korrekt: Der MCP-Client startet den Server als lokalen Child Process. Es ist kein Fehler im Domain-/Application-Design, weil diese Schichten bereits transportneutral sind.

Für den zentralen Firmennetzbetrieb wird MCP auf **Streamable HTTP** umgestellt:

- ASP.NET-Core-Integration über das offizielle `ModelContextProtocol.AspNetCore`-Paket.
- Endpoint `/mcp` im selben Host wie Blazor und REST.
- Stateless Transport als Ziel, weil alle fachlich relevanten Zustände bereits explizit durch `TransactionId`, `SnapshotId`, Cursor und Rolle adressiert werden.
- Kein Transport-Sessionzustand als versteckte fachliche Quelle.
- STDIO-Ablösung erfolgt als eigener Hard-Cut-Schritt; kein dauerhafter Doppelbetrieb aus Kompatibilitätsgründen.

### 10.4 Projekt- und Namespace-Struktur

Ein deploybares Projekt bleibt für den aktuellen Umfang sinnvoll:

```text
src/KnowHowToAI.Server/
├─ Program.cs
├─ appsettings.json
├─ Configuration/
├─ Hosting/
├─ Mcp/
│  ├─ Contracts/
│  ├─ Mapping/
│  └─ Tools/
├─ Api/
│  ├─ Contracts/
│  ├─ Mapping/
│  └─ Endpoints/<Feature>/
├─ Web/
│  ├─ Components/Layout/
│  ├─ Components/Pages/
│  ├─ Components/Shared/
│  ├─ Features/<Feature>/
│  └─ State/
└─ wwwroot/
```

Namespaces folgen `KnowHowToAI.Server.Mcp`, `.Api` und `.Web`. Die bestehende Schreibweise `Mcp` bleibt bestehen; `MCP` wird nicht als abweichende Parallelstruktur eingeführt.

- `KnowHowToAI.Server` wird ASP.NET-Core-Webhost und Composition Root.
- Das Projekt wechselt bei Implementierung auf `Microsoft.NET.Sdk.Web`.
- Eine `appsettings.json` enthält die gemeinsame Basiskonfiguration für Datenbank, Policies, Web, REST und MCP. Umgebungsspezifische Overrides bleiben eine Betriebsoption, keine zweite fachliche Konfiguration.
- Features werden innerhalb `Api` und `Web` weiter unterteilt; ein Projekt bedeutet keine flachen Sammelordner oder God Components.
- Domain-, Application- und Storage-Code verbleiben in den vorhandenen Projekten.
- Ein separates Web-Client-Projekt ist erst erforderlich, falls später bewusst Interactive WebAssembly gewählt wird.

### 10.5 Festgelegter und noch zu wählender Stack

Festgelegt:

- ASP.NET Core als gemeinsamer Webhost.
- Blazor Web App mit Interactive Server für den ersten Stand.
- Minimal APIs plus OpenAPI für allgemeine Integrationen.
- MCP Streamable HTTP als Zieltransport für den zentralen Betrieb.
- Einsatz etablierter Komponenten statt Eigenbau.

Noch anhand von Spikes auszuwählen:

- Markdown-nativer Rich-Text-/WYSIWYG-Editor mit Bild-Upload-Hooks und kontrollierbarer Toolbar.
- Baumkomponente mit Lazy Loading, Drag-and-drop und virtueller Darstellung.
- Upload-/Asset-Komponenten.
- HTML/CSS-basierter serverseitiger PDF-Renderer.
- optionales UI-Komponentenpaket für Layout, Formulare, Tabellen und Dialoge.

Komponentenauswahlkriterien: aktive Pflege, kompatible Lizenz, .NET-10-/Blazor-Kompatibilität, Barrierefreiheit, Internationalisierung, Testbarkeit, Theme-Fähigkeit, keine erzwungene Cloud-Abhängigkeit und kein proprietäres Contentformat.

### 10.6 Betrieb

- Ein ASP.NET-Core-Prozess hostet Blazor, REST, Assets und MCP.
- Eine Deployment-Einheit und eine gemeinsame Basiskonfiguration.
- Bindung an die festgelegte Intranet-Adresse; kein Internet-Exposure.
- Der erste Stand besitzt keine Authentifizierung. Netzwerksegmentierung, Firewall und Hostzugriff bilden bis zum Security-Schritt die Betriebsgrenze.
- `AllowedHosts` wird auf die tatsächlichen Intranet-Hostnamen begrenzt; keine pauschale Wildcard.
- UI, REST und MCP liegen im selben Origin. CORS wird nicht allgemein geöffnet; serverseitige n8n-Aufrufe benötigen kein CORS.
- Reverse Proxy und Netzwerk müssen die langlebigen Verbindungen von Blazor Interactive Server unterstützen.
- Blazor-Circuit-State ist flüchtiger UI-Zustand. Fachlicher Arbeitsstand bleibt über SQL, `TransactionId`, `SnapshotId` und `ChangeVersion` rekonstruierbar.
- TLS ist auch im Firmennetz anzustreben, spätestens bevor sensible Kundendaten oder nicht vertrauenswürdige Netzsegmente angebunden werden.
- Späterer Mehrinstanzbetrieb darf keine In-Memory-Fachzustände voraussetzen; Skalierungsanforderungen der Blazor-Circuits werden dann separat behandelt.

## 11. Umsetzungsschnitte

### Schnitt 0: Gemeinsamer Webhost und MCP-Transport

- `KnowHowToAI.Server` auf `Microsoft.NET.Sdk.Web` und `WebApplication` umstellen.
- Gemeinsamen Host mit Blazor-Shell, REST-/OpenAPI-Grundstruktur und Asset-Route aufsetzen.
- Offizielles ASP.NET-Core-MCP-Paket integrieren und `/mcp` als Streamable HTTP mappen.
- Bestehende Tool-Verträge und Application Services unverändert weiterverwenden.
- Relevante MCP-Clients gegen Streamable HTTP abnehmen.
- STDIO im selben kohärenten Slice per Hard Cut entfernen.

**Nutzen:** Eine zentrale Deployment-Einheit und eine gemeinsame HTTP-Basis für Menschen, Integrationen und Agenten.

### Schnitt 1: Lesbares System

- ASP.NET-Core-Host mit Blazor Interactive Server.
- Dashboard mit Current Snapshot, Transactions und Releases.
- Lazy Knowledge Tree, Suche, Rollenumschaltung und read-only Node-Ansicht.
- Snapshot-/Release-Navigation sowie vorhandener Markdown-Export.

**Nutzen:** Sofortiger Überblick über Struktur und Wissensstand bei geringem Änderungsrisiko.

### Schnitt 2: Menschliches Editieren

- Transaction beginnen/fortsetzen/verwerfen/committen.
- Node-Editor und ausgewählte Markdown-native Rich-Text-Komponente.
- Nodes erstellen, verschieben, sortieren und löschen.
- Validierung, Findings und strukturierter Transaction-Diff.
- Rollen und Resolution Orders pflegen.
- REST-Endpunkte für die freigegebenen Lese- und Schreibworkflows.

**Nutzen:** Vollständige manuelle Pflege ohne Agent.

### Schnitt 3: Redaktioneller Workflow

- Strukturierte TODOs/Fragen/Reviews/AgentTasks.
- Arbeitslisten für stale Content, fehlende Rollen-Inhalte und Refactoring-Kandidaten.
- MCP-Zugriff auf redaktionelle Aufträge.
- Nachvollziehbares Auflösen zusammen mit Inhaltsänderungen.

**Nutzen:** Geordnete Zusammenarbeit zwischen Mensch und externen Agenten.

### Schnitt 4: Publikation

- Publikationsprofile und Assets.
- HTML-Vorschau und PDF-Export.
- Release-bezogene Reproduzierbarkeit und Exportprotokoll.
- Freigabeprüfungen für stale Content, Findings und offene Hinweise.

**Nutzen:** Ein-Klick-Endkundendokumentation aus demselben Wissensbestand.

### Schnitt 5: Integrierte Assistenz

- Agentenauftrag aus ausgewähltem Node/TODO starten.
- Vorschlag, Diff und Validierung vor Commit anzeigen.
- Später Semantic Kernel oder eine andere Orchestrierung hinter einer eigenen Application-Grenze.

**Nutzen:** Komfort, ohne deterministische Kernworkflows oder externe MCP-Clients zu ersetzen.

## 12. Bewusst nicht im ersten Frontend

- Integrierter Chat oder autonome Agentenläufe.
- Semantische Suche und Embeddings.
- Gleichzeitiges kollaboratives Live-Editing.
- Automatisches Merge/Rebase bei Snapshot-Konflikten.
- Vollständiges Benutzer-, ACL- oder Mandantenmodell.
- Direkter Endkundenzugang.
- Visueller, frei platzierbarer Graph als Ersatz für den kanonischen Baum.
- Ein umfassendes CMS mit beliebigen Seitentypen.

## 13. Offene Entscheidungen

| Priorität | Frage | Empfehlung für den Start |
|---|---|---|
| Hoch | Arbeitsmodell | Pro Browserarbeitskontext genau eine aktive Transaction; bewusster Wechsel erlaubt |
| Hoch | Redaktionelle Hinweise | Strukturiert speichern, nicht in exportierbarem Markdown |
| Hoch | Publikationskonfiguration | Versioniertes Profil mit unveränderlich referenzierten Assets |
| Hoch | Rich-Text-Komponente | Markdown-nativ, verlustarmer Roundtrip, Upload-Hooks, Headings deaktivierbar, aktiv gepflegt |
| Hoch | Asset-Speicher | Immutable/revisioniert und dedupliziert; SQL-Metadaten, Binärspeicher nach Messung/Spike |
| Hoch | MCP-Cutover | Nach HTTP-Abnahme Hard Cut von STDIO auf Streamable HTTP |
| Hoch | Unauthentifizierter Intranetbetrieb | Zulässige Netzsegmente und Firewallgrenzen vor Deployment explizit festlegen |
| Mittel | Offizieller Exportstand | Nur committed Snapshot/Release; Working nur als Wasserzeichen-Vorschau |
| Mittel | Navigation für Zielgruppen | Kanonischen Baum behalten; später Presentation Views auf denselben NodeIds |
| Mittel | PDF-Renderer | Nach Spike anhand CSS-Unterstützung, TOC, Header/Footer, Lizenz und Deployment wählen |
| Niedrig | Integrierte KI | Erst nach stabilen manuellen Workflows und klarer Agent-Task-Semantik |

## 14. Risiken und Gegenmaßnahmen

| Risiko | Gegenmaßnahme |
|---|---|
| UI umgeht Domainregeln | Ausschließlich Application Services verwenden; End-to-End-Vertragstests für MCP und HTTP |
| Benutzer verliert den Arbeitskontext | Snapshot/Transaction/Rolle permanent sichtbar; Navigation vor Kontextwechsel absichern |
| TODOs gelangen in Kundendokumente | Strukturierte Editorial Notes; Export schließt sie standardmäßig aus |
| Tiefe Bäume werden langsam/unübersichtlich | Lazy Loading, Suche, Breadcrumbs, gespeicherte Filter; kein vollständiger Tree-Dump |
| Drag-and-drop erzeugt falsche Struktur | Zielvorschau, serverseitige Zyklus-/Parent-Prüfung, Transaction-Diff vor Commit |
| PDF ist nicht reproduzierbar | Snapshot/Release + Profilversion + Assetversion protokollieren |
| Content-Rollen werden als Rechte missverstanden | UI-Texte und Architektur trennen Zielgruppe strikt von Zugriffsschutz |
| Mehrere Clients committen parallel | `SnapshotConflict` sichtbar behandeln; später geführtes manuelles Reapply statt implizitem Merge |
| Frontend wird zum zweiten Produktkern | Fachlogik und Validierung ausschließlich in Core; dünne Adapter |
| Firmennetz wird fälschlich als sichere Authentifizierung behandelt | Kein Internet-Exposure; Netzgrenzen dokumentieren; Authentifizierung als eigener zwingender Ausbau vor größerem Nutzerkreis |
| Rich-Text-Editor verändert Markdown unbemerkt | Roundtrip-Tests mit realen Inhalten; Markdown-natives Modell; Quellmodus; keine HTML-first-Konvertierung |
| Bilder blähen Snapshots auf | Immutable, per Hash deduplizierte Assets; Snapshots referenzieren Assets statt Binärdaten zu kopieren |
| REST, MCP und UI entwickeln abweichende Semantik | Gemeinsame Application Services und transportübergreifende Vertragstests |

## 15. Erfolgskriterien des ersten nutzbaren Frontends

- Ein Benutzer versteht ohne MCP-Client, welche Wissensstruktur, Rollen, offenen Transactions und Releases existieren.
- Ein Benutzer kann einen Node und rollenbezogenen Content in einer Transaction anlegen, bearbeiten, verschieben, validieren und committen.
- Fallback, Freshness, Warnungen und der konkrete Arbeitsstand sind jederzeit sichtbar.
- Ein externer Agent sieht die anschließend committed Änderung unverändert über MCP.
- Eine vom Agenten committed Änderung erscheint ohne Synchronisationsschritt im Frontend.
- Kein Webworkflow schreibt direkt in committed Snapshots oder umgeht Domainregeln.
- Das Frontend bleibt ohne integriertes LLM vollständig verwendbar.
- Ein n8n-Workflow kann einen dokumentierten REST-Endpunkt aufrufen und erhält denselben fachlichen Stand wie UI und MCP.
- Ein MCP-Client kann zentral über `/mcp` auf dieselben Tools zugreifen, sobald der separate Transport-Schnitt umgesetzt ist.

## 16. Nächster Klärungsschritt

Vor Implementierung wird ein fachlicher MVP-Schnitt entschieden. Empfohlen ist zuerst der durchgängige Workflow:

```text
Baum ansehen
→ Transaction starten
→ Node auswählen/erstellen
→ Rollen-Content bearbeiten
→ Node verschieben
→ Diff und Validierung prüfen
→ committen
→ Ergebnis über MCP lesen
```

Parallel werden technische Spikes getrennt bewertet:

1. Markdown-nativer Rich-Text-Editor inklusive Heading-Sperre, Tabellen, Code und Bild-Upload.
2. Lazy Tree mit Drag-and-drop und großen/tiefen Strukturen.
3. Asset-Modell und reproduzierbare Referenzierung aus Markdown, Snapshots und Releases.
4. Reproduzierbarer PDF-Renderer.
5. ASP.NET-Core-Host mit Blazor, Minimal API/OpenAPI und MCP Streamable HTTP in einem Prozess.

Die Spikes prüfen Integrationsrisiken und Komponenten; Fachlogik bleibt in Core/Application.

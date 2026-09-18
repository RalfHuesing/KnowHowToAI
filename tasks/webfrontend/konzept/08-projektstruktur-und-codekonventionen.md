# Projektstruktur und Codekonventionen

## Verbindlichkeit

Dieses Dokument definiert die Zielstruktur für alle Webfrontend-Roadmap-Tasks. Es ergänzt die allgemeinen Projektregeln und wird vor jeder Implementierung gelesen.

- Bestehende fachliche Strukturen in `Core`, `Storage.SqlServer` und `Server/Mcp` bleiben erhalten, sofern ein Task keine fachlich notwendige Änderung verlangt.
- Neue Dateien werden ausschließlich den hier definierten Projekten, Ordnern und Namespaces zugeordnet.
- Fehlt für einen neuen Baustein eine Zuordnung, wird dieses Dokument vor der Codeänderung präzisiert. Kein Agent legt improvisierte Parallelstrukturen an.
- Der umgesetzte Ist-Zustand wird im selben Task in [`docs/Architektur.md`](../../../docs/Architektur.md) nachgeführt. Dieses Dokument bleibt Zielkonzept, `docs/` bleibt Ist-Dokumentation.

## Projektgraph

Es entstehen keine zusätzlichen Produktionsprojekte für Web, MCP, PDF oder Assets.

```text
KnowHowToAI.Server
  ├─> KnowHowToAI.Core
  └─> KnowHowToAI.Storage.SqlServer

KnowHowToAI.Storage.SqlServer
  └─> KnowHowToAI.Core
```

Verboten:

- Referenz von `Core` auf `Server` oder `Storage.SqlServer`.
- Referenz von `Storage.SqlServer` auf `Server`.
- Geschäftslogik in Razor-Komponenten, MCP-Tools, HTTP-Endpunkten oder SQL-Repositories.
- Ein zusätzliches Web-API-, Shared-Models- oder DTO-Projekt ohne neue, dokumentierte Architekturentscheidung.

## Zielstruktur der Solution

```text
LICENSE
THIRD-PARTY-NOTICES.md

src/
├─ KnowHowToAI.Core/
├─ KnowHowToAI.Storage.SqlServer/
└─ KnowHowToAI.Server/

tests/
├─ KnowHowToAI.Core.Tests/
├─ KnowHowToAI.IntegrationTests/
├─ KnowHowToAI.Web.Tests/
└─ KnowHowToAI.BrowserTests/
```

- `LICENSE` enthält den unveränderten MIT-Lizenztext mit `Copyright (c) 2026 Ralf Hüsing`.
- `THIRD-PARTY-NOTICES.md` inventarisiert direkte und transitive externe Abhängigkeiten mit Version, Quelle, Lizenz und einzuhaltenden Hinweisen. Jede Abhängigkeitsänderung aktualisiert das Inventar im selben Commit.
- Roadmap und Konzepte schreiben keine Fremdversionsnummern vor. Beim erstmaligen Aufnehmen einer Abhängigkeit wählt der Umsetzungstask die dann aktuelle stabile, zueinander kompatible Version aus offizieller Quelle; Preview-, Beta- und RC-Versionen sind ausgeschlossen. Die konkret aufgelösten Versionen gehören ausschließlich in Paketverwaltung, Lockfile und `THIRD-PARTY-NOTICES.md`. Bereits im Produkt konfigurierte Versionen werden nicht nebenbei durch einen Featuretask ersetzt, sondern nur im Rahmen einer beauftragten Abhängigkeitsaktualisierung oder eines belegten Kompatibilitätsfixes samt Lizenz- und Testnachweis geändert.

| Projekt | Verantwortung |
|---|---|
| `KnowHowToAI.Core` | Domain, transportneutrale Use Cases, Ports und fachliche Ergebnisse |
| `KnowHowToAI.Storage.SqlServer` | SQL-Verbindungen, Migrationen, Repositories und SQL-Mapping |
| `KnowHowToAI.Server` | Composition Root, Konfiguration, gemeinsamer Kestrel-Host, MCP-Streamable-HTTP-, Blazor-, PDF- und Browser-Endpunkt-Adapter |
| `KnowHowToAI.Core.Tests` | schnelle Domain- und Application-Tests |
| `KnowHowToAI.IntegrationTests` | SQL-, Host-, MCP-, PDF-Prozess- und HTTP-Grenztests |
| `KnowHowToAI.Web.Tests` | schnelle Razor-Komponenten- und Circuit-State-Tests mit bUnit und xUnit v3 |
| `KnowHowToAI.BrowserTests` | vollständige Browserabläufe mit Microsoft.Playwright .NET gegen einen real gestarteten Server und Google Chrome Stable |

`KnowHowToAI.Web.Tests` und `KnowHowToAI.BrowserTests` sind Zielprojekte ab M1.2 und existieren vorher noch nicht. Beim Anlegen werden sie in `KnowHowToAI.slnx` und das jeweils zuständige zentrale Testskript aufgenommen; ihre Paketversionen werden in `Directory.Packages.props` verwaltet. `KnowHowToAI.Web.Tests` läuft im FastTest-Gate; `KnowHowToAI.BrowserTests` läuft im Integrationstest-Gate.

- `KnowHowToAI.Web.Tests` referenziert `KnowHowToAI.Server` und `KnowHowToAI.Core`.
- `KnowHowToAI.BrowserTests` behandelt den gebauten Server als Black Box und referenziert kein Produktionsprojekt; der Testhost startet die veröffentlichte Server-EXE mit expliziten Environment-/Kommandozeilen-Overrides, nicht mit einer zweiten produktiven Appsettings-Datei.
- `KnowHowToAI.IntegrationTests` behält seine bestehenden Referenzen auf Server, Core und SQL-Storage.
- Reale Kestrel-, Routing-, MCP- und HTTP-Grenztests verbleiben in `KnowHowToAI.IntegrationTests`; `KnowHowToAI.Web.Tests` dupliziert diese Hostgrenzen nicht.
- Die Testwerkzeuge werden beim ersten Einrichten gemäß der allgemeinen Abhängigkeitsregel gewählt. Spätere Aktualisierungen sind eigenständige, nachvollziehbar geprüfte Abhängigkeitsänderungen und aktualisieren das Lizenzinventar.

## `KnowHowToAI.Core`

Bestehende Featuregrenzen bleiben maßgeblich. Neue Webanforderungen erweitern Core nur, wenn ein transportneutraler Use Case fehlt.

```text
KnowHowToAI.Core/
├─ Domain/
│  ├─ Common/
│  ├─ Hierarchy/
│  ├─ Roles/
│  ├─ Content/
│  ├─ Dependencies/
│  ├─ Versioning/
│  ├─ Validation/
│  └─ Assets/                         # erst M8
└─ Application/
   ├─ Abstractions/
   │  ├─ Persistence/
   │  ├─ Runtime/
   │  ├─ Documents/                  # PDF-Renderer-Port ab M7
   │  └─ Assets/                     # Binärspeicher-Port ab M8
   ├─ Dashboard/                     # aggregierte Dashboard-Reads
   ├─ Navigation/
   ├─ Transactions/
   ├─ Mutations/{Nodes,Roles,Content}/
   ├─ Retrieval/{Search,Export}/
   ├─ History/
   ├─ Assets/                        # Asset-Use-Cases ab M8
   ├─ Policies/
   └─ Runtime/
```

### Neue Application-Bausteine

| Baustein | Zielnamespace | Regel |
|---|---|---|
| Dashboard-Abfrage | `KnowHowToAI.Core.Application.Dashboard` | aggregiert ausschließlich über vorhandene oder schmale neue Read-Ports; keine UI-Typen |
| PDF-Export-Orchestrierung | `KnowHowToAI.Core.Application.Retrieval.Export` | verwendet `MarkdownExportService` und `IPdfRenderer`; kennt weder Pandoc noch Dateipfade |
| PDF-Renderer-Port | `KnowHowToAI.Core.Application.Abstractions.Documents` | genau ein Interface `IPdfRenderer`; Implementierung liegt im Server |
| Asset-Domain | `KnowHowToAI.Core.Domain.Assets` | immutable Assetmetadaten; `AssetId` liegt bei den übrigen IDs in `Domain.Common` |
| Asset-Use-Cases | `KnowHowToAI.Core.Application.Assets` | Upload-Metadaten, Lesen und Referenzauflösung; keine HTTP-Typen |
| Asset-Ports | `KnowHowToAI.Core.Application.Abstractions.Assets` und `.Persistence` | Binärspeicher und Metadatenpersistenz getrennt; konkrete Implementierung nach O-004 |

Request-, Query-, Result- und Page-Records liegen im Namespace des zugehörigen Use Cases. Es gibt keinen globalen Ordner `Dtos`, `Models`, `Commands`, `Queries`, `Helpers`, `Utils` oder `Services`.

### Fest eingeplante neue C#-Typen

| Milestone | Typ | Pfad/Namespace | Verantwortung |
|---|---|---|---|
| M1 | `WebServiceRegistration` | `Server/Web` | Blazor- und Web-Services registrieren |
| M1 | `WebEndpointRegistration` | `Server/Web` | technische Browser-Endpunkte featureweise mappen |
| M3 | `DashboardService`, `DashboardQuery`, `DashboardResult` | `Core/Application/Dashboard` | transportneutrale Dashboardaggregation |
| M3 | `IDashboardRepository` | `Core/Application/Abstractions/Persistence` | ausschließlich fehlende, effizient aggregierbare Dashboard-Reads |
| M3 | `SqlDashboardRepository` | `Storage.SqlServer/Repositories/Retrieval` | SQL-Implementierung des Dashboard-Ports |
| M3 | `WorkspaceState` | `Server/Web/State` | aus URL rekonstruierbarer Circuit-Cache für Node, Rolle und Read Context |
| M3 | `MarkdownDownloadEndpoint` | `Server/Web/Endpoints` | Markdown-Download auf `MarkdownExportService` mappen |
| M7 | `IPdfRenderer`, `PdfRenderRequest`, `PdfRenderResult` | `Core/Application/Abstractions/Documents` | transportneutraler PDF-Renderer-Port |
| M7 | `PdfExportService`, `PdfExportRequest`, `PdfExportResult` | `Core/Application/Retrieval/Export` | Markdown-Teilbaum und Renderer orchestrieren |
| M7 | `PdfOptions`, `PdfOptionsValidator` | `Server/Pdf/Configuration` | PDF-Konfiguration binden und vollständig validieren |
| M7 | `PandocPdfRenderer` | `Server/Pdf/Rendering` | `IPdfRenderer` über Pandoc/WeasyPrint implementieren |
| M7 | `PdfProcessRunner` | `Server/Pdf/Rendering` | genau einen begrenzten externen Prozess starten, abbrechen und diagnostizieren |
| M7 | `PdfDownloadEndpoint` | `Server/Web/Endpoints` | PDF-Download auf `PdfExportService` mappen |
| M8 | `AssetId` | `Core/Domain/Common/KnowledgeIdentifiers.cs` | symmetrische Guid-Identität |
| M8 | `KnowledgeAsset` | `Core/Domain/Assets` | immutable fachliche Assetmetadaten |
| M8 | `AssetService` | `Core/Application/Assets` | Upload, Metadatenread und Referenzauflösung orchestrieren |
| M8 | `IAssetMetadataRepository` | `Core/Application/Abstractions/Persistence` | persistente Assetmetadaten |
| M8 | `IAssetBinaryStore` | `Core/Application/Abstractions/Assets` | Binärinhalt speichern und streamen |
| M8 | `SqlAssetMetadataRepository` | `Storage.SqlServer/Repositories/Assets` | SQL-Metadatenpersistenz |
| M8 | `AssetUploadEndpoint`, `AssetContentEndpoint` | `Server/Web/Endpoints` | multipart Upload beziehungsweise immutable Binärauslieferung mappen |

Weitere Typen folgen den Namens- und Platzierungsregeln dieses Dokuments. Ein neuer Typ mit überschneidender Verantwortung ersetzt oder erweitert den eingeplanten Typ, statt parallel angelegt zu werden.

## `KnowHowToAI.Storage.SqlServer`

```text
KnowHowToAI.Storage.SqlServer/
├─ Configuration/
├─ Connections/
├─ Migrations/
├─ Mapping/
└─ Repositories/
   ├─ Transactions/
   ├─ Knowledge/
   ├─ Snapshots/
   ├─ History/
   ├─ Retrieval/
   └─ Assets/                         # Assetmetadaten ab M8
```

- Repositories folgen dem fachlichen Zugriffsmuster, nicht einzelnen Tabellen.
- Jede öffentliche Repositoryoperation entspricht einem Core-Port.
- Dapper-Zeilenmodelle bleiben intern unter `Mapping` oder beim eng zugehörigen Repository.
- Assetmetadaten liegen in `Repositories.Assets`. Der Ort der Binärspeicherimplementierung wird in M8.1-T1 durch O-004 festgelegt und vor Implementierung hier ergänzt.

## `KnowHowToAI.Server`

```text
KnowHowToAI.Server/
├─ Program.cs
├─ appsettings.json
├─ Configuration/
├─ Hosting/
├─ Mcp/
│  ├─ Contracts/{History,Navigation,Transactions,Mutations}/
│  ├─ Mapping/
│  └─ Tools/{History,Navigation,Retrieval,Transactions,Mutations}/
├─ Web/
│  ├─ Components/
│  │  ├─ App.razor
│  │  ├─ Routes.razor
│  │  ├─ _Imports.razor
│  │  ├─ Layout/
│  │  └─ Shared/
│  ├─ Features/
│  │  ├─ Dashboard/
│  │  ├─ Knowledge/
│  │  ├─ Search/
│  │  ├─ History/
│  │  ├─ Transactions/
│  │  ├─ Roles/
│  │  ├─ Content/
│  │  ├─ PdfExport/                  # M7
│  │  └─ Assets/                     # M8
│  ├─ Endpoints/
│  ├─ State/
│  ├─ WebEndpointRegistration.cs
│  └─ WebServiceRegistration.cs
├─ Pdf/                              # M7
│  ├─ Configuration/
│  ├─ Rendering/
│  └─ Templates/Default/
│     ├─ template.html
│     ├─ document.css
│     ├─ logo.svg
│     └─ fonts/
└─ wwwroot/
   └─ css/app.css
```

### Server-Namespaces

| Ordner | Namespace | Inhalt |
|---|---|---|
| `Configuration` | `KnowHowToAI.Server.Configuration` | bindbare immutable Options, Validatoren und Redaction |
| `Hosting` | `KnowHowToAI.Server.Hosting` | Composition-Root-Erweiterungen, Hoststart und Hosted Services |
| `Mcp` | `KnowHowToAI.Server.Mcp.*` | ausschließlich MCP-Contracts, Mapping und dünne Tools |
| `Web.Components` | `KnowHowToAI.Server.Web.Components.*` | App, Router, Layout und featureübergreifende Darstellung |
| `Web/Features/Dashboard` bis `Web/Features/Assets` gemäß Zielbaum | gleichnamiger Namespace unter `KnowHowToAI.Server.Web.Features`, beispielsweise `KnowHowToAI.Server.Web.Features.Knowledge` | Razor-Seiten, featurelokale Komponenten, ViewModels, Mapping und UI-Orchestrierung |
| `Web.Endpoints` | `KnowHowToAI.Server.Web.Endpoints` | ausschließlich technisch notwendige Browser-Uploads/-Downloads |
| `Web.State` | `KnowHowToAI.Server.Web.State` | flüchtiger Circuit-State ohne fachliche Wahrheit |
| `Pdf.Configuration` | `KnowHowToAI.Server.Pdf.Configuration` | Pfade, Timeout und Startvalidierung für Pandoc/WeasyPrint |
| `Pdf.Rendering` | `KnowHowToAI.Server.Pdf.Rendering` | `IPdfRenderer`-Implementierung und kontrollierte Prozessausführung |

`Program.cs` erstellt den Host und ruft ausschließlich klar benannte Registrierungs-/Mapping-Erweiterungen auf. Featurelogik und Optionsvalidierung stehen nicht in `Program.cs`.

### Host-, Transport- und UI-Grenzen aus M0

- `KnowHowToAI.Server` ist die einzige EXE und betreibt Blazor sowie MCP auf demselben Kestrel-Origin und demselben konfigurierten Port. Es entsteht kein zweiter Listener und für M1–M2 keine Reverse-Proxy-Voraussetzung.
- MCP verwendet das offizielle Paket `ModelContextProtocol.AspNetCore` einschließlich seiner kompatiblen SDK-Abhängigkeiten. `MapMcp("/mcp")` wird ausschließlich als stateless Streamable HTTP mit `SessionMode = Stateless` und `EnableLegacySse = false` registriert; Legacy-SSE und zustandsbehaftete MCP-Sessions werden nicht angelegt.
- `/api` und `/api/{**reservedPath}` bleiben bis zu einer späteren Integrationsentscheidung explizit reserviert und dürfen ebenso wie `/mcp` nie vom Blazor-Fallback beantwortet werden. Die konkrete Registrierungs- und Mappingreihenfolge steht im [Architekturkonzept](05-architektur-api-und-mcp.md#mcp-transport).
- Web-Basiskomponenten verwenden ausschließlich natives Blazor, semantisches HTML und eigenes CSS. Es wird keine allgemeine UI-Komponentenbibliothek referenziert; der `KnowledgeTree` ist eine native Komponente und kein Radzen- oder anderer Fremd-Tree.

## Web-Featurezuordnung

| Feature | Route | Routable Page | Featurelokale Hauptkomponenten |
|---|---|---|---|
| Dashboard | `/` | `DashboardPage.razor` | `SnapshotSummary`, `OpenTransactionList`, `QualitySummary`, `RecentChanges` |
| Wissen | `/knowledge`, `/knowledge/{NodeId:guid}` | `KnowledgePage.razor` | nativer `KnowledgeTree`, `Breadcrumbs`, `NodeDetails`, `RoleContentView` |
| Suche | `/search` | `SearchPage.razor` | `SearchForm`, `SearchResults`, `KnowledgeFilter` |
| Historie | `/history` | `HistoryPage.razor` | `SnapshotList`, `ReleaseList`, `SnapshotDiff`, `CreateReleaseDialog` |
| Transactions | `/transactions`, `/transactions/{TransactionId:guid}` | `TransactionsPage.razor`, `TransactionPage.razor` | `TransactionHeader`, `TransactionValidation`, `TransactionDiff`, `CommitDialog` |
| Rollen | `/roles` | `RolesPage.razor` | `RoleEditor`, `ResolutionOrderEditor`, `FallbackPreview` |
| Content | keine eigene Route | – | `ContentEditor`, `ContentMetadata`, `SourceRevisionEditor`, `MarkdownSourceEditor` nach O-010 |
| PDF | keine eigene Route | – | `PdfExportButton` in der Wissensansicht |
| Assets | keine eigene Route | – | `AssetUpload`, `AssetImage`, Integration in `ContentEditor` |

Regeln:

- Ein Feature besitzt keinen eigenen Unterordner `Services`. UI-Orchestrierung heißt nach konkretem Zweck, beispielsweise `KnowledgeLoader` oder `TransactionWorkspace`, und entsteht nur bei mehr als einer konsumierenden Komponente.
- Featurelokale ViewModels und Mapper bleiben im Featureordner. Unterordner `Components`, `Mapping` oder `Models` werden erst angelegt, wenn mindestens drei Dateien derselben Art vorhanden sind.
- Gemeinsam verwendet bedeutet Nutzung durch mindestens zwei Features. Erst dann wird ein rein darstellender Baustein nach `Web/Components/Shared` verschoben.
- `ContentEditor` bleibt Bestandteil der Knowledge-Seite; es entsteht keine zweite, konkurrierende Node-Editor-Seite.
- `KnowledgeTree` verwendet keine Fremdkomponente. Paging, Cachegrenze, Semantik und Move-Positionen sind im [Bedienkonzept](02-bedienkonzept-und-ui.md#wissensbaum) verbindlich festgelegt.
- `ContentEditor` bindet ausschließlich Milkdown `@milkdown/crepe` über `ContentEditor.razor.js` ein. Die konkrete lokale npm-/Bundle-Erzeugung wird vor M5-Produktivcode im manuellen M5.0-Gate festgelegt; die dann aufgelösten Werkzeugversionen werden im Lockfile festgehalten. Die M0-Vite-Fixture ist ausdrücklich keine Vorentscheidung.
- Das M5.0-Gate ergänzt vor M5.1-T1 in diesem Dokument den exakten Ablageort von Paketmanifest, Lockfile, Buildkonfiguration und erzeugten lokalen Milkdown-Assets. Vor dieser Entscheidung wird weder ein `package.json` noch ein vorläufiger Bundle-/Vendor-Ordner als Produktstruktur festgelegt.

## URL- und Arbeitskontext

Der fachliche Lesekontext ist rekonstruierbar und wird nicht ausschließlich im Circuit gespeichert.

- Ohne Selektor wird der Current Snapshot gelesen.
- Genau einer der Query-Parameter `transactionId`, `snapshotId` oder `releaseId` darf gesetzt sein.
- `releaseId` wird an der Web-Grenze auf den unveränderlichen Snapshot des Releases aufgelöst.
- `roleId` ist für rollenaufgelösten Content, Suche sowie Markdown- und PDF-Export verpflichtend. O-008 legt nur fest, ob die UI beim Einstieg keine Rolle, eine feste Standardrolle oder die zuletzt verwendete Rolle auswählt; jeder fachliche Aufruf übergibt anschließend eine explizite `RoleId`.
- Der ausgewählte Node steht in der Route `/knowledge/{NodeId}`.
- Filter, Paging-Cursor und Dialogzustand sind kein globaler fachlicher Kontext und bleiben featurelokal.
- Eine URL mit ungültigem oder nicht mehr vorhandenem Kontext zeigt einen fachlichen Fehler und fällt nicht still auf Current zurück.

`WorkspaceState` darf die aus Route und Query gelesenen Werte für den Circuit cachen. Route und Query bleiben die rekonstruierbare Quelle für Node, Rolle und Read Context. Persistierter Wissenszustand liegt ausschließlich in Application/Storage.

## Technische Browser-Endpunkte

Es entsteht keine allgemeine REST-API. Zulässige Endpunktgruppen:

| Methode und Route | Zeitpunkt | Zweck |
|---|---|---|
| MCP-Mapping auf `/mcp` | M1 | einziger MCP-Transport, Streamable HTTP |
| `GET /downloads/markdown` | M3 | Markdown-Teilbaum als Datei; Query verwendet `nodeId`, `roleId` und höchstens einen Read-Context-Selektor |
| `GET /downloads/pdf` | M7 | PDF-Teilbaum als Datei mit demselben Queryvertrag wie Markdown |
| `POST /assets` | M8 | kontrollierter Bild-Upload als `multipart/form-data` |
| `GET /assets/{AssetId:guid}` | M8 | immutable Assetbinärdaten mit gespeichertem MIME-Type |

Endpunkte validieren HTTP-spezifische Eingaben, mappen auf Application-Aufrufe und liefern HTTP-Ergebnisse. Sie enthalten keine Fachregeln und greifen nie direkt auf Repositories zu. Neue Browser-Endpunkte werden in `WebEndpointRegistration` registriert; `/api` bleibt für eine spätere allgemeine Integrations-API frei.

Fehlerantworten der Browser-Endpunkte verwenden RFC-9457-`ProblemDetails` und enthalten zusätzlich den stabilen fachlichen Fehlercode sowie die Correlation-ID. Die feste Zuordnung lautet:

| HTTP-Status | Bedeutung |
|---:|---|
| `400` | ungültige oder widersprüchliche Query-/Form-Daten |
| `404` | Node, Rolle, Snapshot, Release, Transaktion oder Asset nicht vorhanden |
| `409` | fachlicher Zustandskonflikt, beispielsweise bereits geschlossene Transaktion |
| `413` | Asset überschreitet die in M8 festgelegte Maximalgröße |
| `415` | nicht zugelassener Asset-MIME-Type |
| `500` | unerwarteter interner oder externer Werkzeugfehler; keine Prozessausgabe, Pfade oder Secrets in der Clientantwort |

Ein fehlgeschlagener Download liefert ausschließlich `ProblemDetails` und niemals eine teilweise erzeugte Datei. Erfolgreiche Uploads liefern `201 Created` mit der kanonischen Asset-URL; erfolgreiche Downloads liefern `200 OK` mit korrektem MIME-Type und `Content-Disposition: attachment`.

Downloadantworten setzen `Cache-Control: no-store`. Der Dateiname des PDF-Exports lautet `<bereinigter-NodeTitel>-<RoleId>.pdf`, der Markdown-Dateiname entsprechend `.md`. Ungültige Dateinamenszeichen werden durch `-` ersetzt, wiederholte Bindestriche zusammengezogen und der Basisname auf 120 Zeichen begrenzt; bei leerem Ergebnis wird die `NodeId` verwendet.

## Konfigurationsstruktur

Es existiert genau eine JSON-Konfigurationsdatei: `src/KnowHowToAI.Server/appsettings.json`. Es werden keine `appsettings.{Environment}.json`, keine testprojektspezifischen Appsettings und keine zweite Serverkonfiguration angelegt. Abweichende Umgebungs- und Testwerte kommen ausschließlich aus Environment-Variablen oder Kommandozeilenargumenten gemäß ASP.NET-Core-Konfigurationspriorität. Neue PDF-Konfiguration liegt unter `KnowHowToAI:Pdf`:

```text
PandocExecutablePath        Default: pandoc
WeasyPrintExecutablePath   Default: weasyprint
TemplateDirectory          Default: Pdf/Templates/Default
ProcessTimeoutSeconds      Default: 120; Bereich: 10..600
MaximumConcurrentExports   Default: 2; Bereich: 1..16
```

Die Pfade werden relativ zum Content Root aufgelöst, sofern sie nicht absolut sind. Der Start validiert Toolerreichbarkeit, Template, CSS, Logo und Fonts-Verzeichnis. Assetkonfiguration wird erst nach O-004 definiert und vor M8-Produktivcode in diesem Abschnitt ergänzt.

## Razor- und Klassennamen

| Artefakt | Namensregel | Beispiel |
|---|---|---|
| Routable Razor-Komponente | Suffix `Page` | `KnowledgePage.razor` |
| Nicht routable Razor-Komponente | fachlicher Substantivname | `KnowledgeTree.razor` |
| Code-behind | gleicher Name plus `.razor.cs` | `KnowledgeTree.razor.cs` |
| Scoped CSS | gleicher Name plus `.razor.css` | `KnowledgeTree.razor.css` |
| isoliertes JavaScript | gleicher Name plus `.razor.js` | `ContentEditor.razor.js` |
| UI-Datenmodell | immutable `record`, Suffix `ViewModel` | `NodeDetailsViewModel` |
| Boundary-Mapping | statische Klasse, Suffix `Mapper` | `NodeDetailsMapper` |
| flüchtiger Circuit-State | Suffix `State` | `WorkspaceState` |
| Application-Orchestrierung | Suffix `Service` | `PdfExportService` |
| Persistence-Port/-Implementierung | Suffix `Repository` | `IAssetMetadataRepository`, `SqlAssetMetadataRepository` |
| Konfiguration | `*Options` plus `*OptionsValidator` | `PdfOptions`, `PdfOptionsValidator` |
| technischer Browser-Handler | Suffix `Endpoint` | `PdfDownloadEndpoint` |
| HTTP-Registrierung | Suffix `EndpointRegistration` | `WebEndpointRegistration` |

- Routable Pages und zustandsbehaftete Featurekomponenten verwenden immer `.razor` plus `.razor.cs`. Rein präsentative Komponenten ohne C#-Logik bleiben in einer `.razor`-Datei.
- Feature-CSS ist scoped. `wwwroot/css/app.css` enthält nur Reset, globale Tokens und frameworkweite Basisklassen.
- JavaScript ist nur für fehlende Browser-/Komponentenfunktionen zulässig und wird per JS-Isolation featurelokal gehalten. Der native Dialogwrapper und `ContentEditor.razor.js` bleiben dünne Interopgrenzen ohne eigene Zustandsmaschine.
- Konkrete C#-Klassen sind `sealed`, Namespaces file-scoped und asynchrone IO-Grenzen führen `CancellationToken` weiter.
- Ein Produktions-`.cs` enthält grundsätzlich einen öffentlichen oder internen Top-Level-Typ. Kleine private Hilfstypen bleiben in der konsumierenden Klasse.
- Verbotene Namen: `Helper`, `Utils`, `CommonService`, `Manager`, `BaseService`, `Models` als globaler Sammelnamespace.

## Dependency Injection und Zustand

- Bestehende stateless Application Services und Repositories bleiben Singleton, solange sie keinen Request-, Circuit- oder Mutable State halten.
- `WorkspaceState` und anderer flüchtiger Browserzustand sind Blazor-Scoped und damit Circuit-bezogen.
- Razor-Komponenten halten nur Darstellungs- und unpersistierten Eingabezustand.
- MCP- und Browser-Endpunkte erhalten den Request-Scope, speichern darin aber keinen fachlichen Zustand über den Request hinaus.
- Options und unveränderliche Policies sind Singleton.
- `PandocPdfRenderer` ist stateless; jeder Renderaufruf besitzt einen eigenen begrenzten Prozess und ein eigenes Arbeitsverzeichnis.
- Ausgewählter Node, Rolle, Snapshot und Transaction werden jedem Application-Aufruf explizit übergeben.

## Teststruktur

```text
tests/KnowHowToAI.Web.Tests/
├─ Components/{Layout,Shared}/
├─ Features/{Dashboard,Knowledge,Search,History,Transactions,Roles,Content,PdfExport,Assets}/
├─ State/
└─ TestSupport/

tests/KnowHowToAI.BrowserTests/
├─ ReadOnly/
├─ Transactions/
├─ Content/
├─ PdfExport/
├─ Assets/
└─ TestSupport/
```

- `Web.Tests` spiegelt die Produktionsfeaturegrenzen. Component-Tests verwenden bUnit mit xUnit v3, prüfen Rendering und Interaktion und mocken dünnes JS-Interop; fachliche Varianten verbleiben in `Core.Tests`.
- `BrowserTests` enthält nur vollständige Benutzerabläufe und verwendet Microsoft.Playwright .NET. Page Objects liegen ausschließlich in `TestSupport` und enthalten keine Assertions. Jeder reguläre Lauf startet ausschließlich die installierte aktuelle Google-Chrome-Stable-Version mit `Channel = "chrome"` und `Headless = true`; fehlendes Chrome ist ein klarer Preflight-Fehler. Es gibt keinen Chromium-Fallback, keinen sichtbaren Browserstart und keine weitere Browsermatrix.
- Der Browser-Testhost startet die veröffentlichte Server-EXE aus einem frisch erzeugten `dotnet publish`-Verzeichnis und verwendet dieses als Content Root. Readiness, Circuitzustand und Interaktionen werden ausschließlich über beobachtbare Zustände und Playwright-Web-first-Assertions abgewartet; feste Sleeps sind verboten. Der Host wird auch bei Testfehlern beendet und der gebundene Port freigegeben.
- Locator-Priorität ist Rolle, Label und danach stabile Test-ID. Screenshots werden erst nach semantischen und Verhaltensassertionen erzeugt; volatile Inhalte werden stabil maskiert und Baselines nie im regulären Lauf automatisch überschrieben.
- Vitest ist im aktuellen Zielstand nicht erforderlich und es wird kein `package.json` allein für JS-Tests angelegt. Erst wenn eigener JavaScript-/TypeScript-Code Zustand mit Verzweigungen, Transformationen oder Retry-/Lifecyclelogik verwaltet, muss der einführende Task vor dem Code Testablage und FastTest-Befehl in diesem Dokument ergänzen. Dünne `mount`-/`readMarkdown`-/`focus`-/`dispose`- und Dialogaufrufe lösen diese Pflicht nicht aus.
- Host-/Routing-/MCP-Tests bleiben in `KnowHowToAI.IntegrationTests/Server`.
- SQL- und Assetmetadaten-Tests bleiben in `KnowHowToAI.IntegrationTests/SqlServer`.
- PDF-Prozessgrenztests liegen in `KnowHowToAI.IntegrationTests/Server/Pdf`; reine PDF-Orchestrierungstests liegen in `Core.Tests/Application/Retrieval/Export`.
- Testdateien spiegeln den Namen des geprüften Typs oder Verhaltens und enden mit `Tests`.

### Feste Testabhängigkeiten und Befehle

| Einsatz | Abhängigkeit/Runtime | Regel |
|---|---|---|
| Razor-Komponenten | `bunit` | nur `KnowHowToAI.Web.Tests`; Version gemäß allgemeiner Abhängigkeitsregel |
| Testframework | `xunit.v3` | nur für die Testprojekte; Version gemäß allgemeiner Abhängigkeitsregel |
| Browsersteuerung | `Microsoft.Playwright` | nur `KnowHowToAI.BrowserTests`; Version gemäß allgemeiner Abhängigkeitsregel |
| Browser | Google Chrome Stable | installierte aktuelle Stable-Version, `Channel = "chrome"`, ausschließlich headless |

Die Projekte werden über `pwsh -NoProfile -File scripts/test-fast.ps1` beziehungsweise `pwsh -NoProfile -File scripts/test-integration.ps1` ausgeführt. Für gezielte lokale Nachweise sind zusätzlich `dotnet test tests/KnowHowToAI.Web.Tests/KnowHowToAI.Web.Tests.csproj` und `dotnet test tests/KnowHowToAI.BrowserTests/KnowHowToAI.BrowserTests.csproj` zulässig. BrowserTests installieren keinen Playwright-Chromium-Browser. Die Chrome-Installation wird vor dem Lauf über den Windows-Uninstall-Eintrag auf Vorhandensein geprüft; die Version selbst wird nicht festgenagelt, jede installierte Stable-Version ist zulässig. Eine nichtinteraktive Bereitstellung darf `winget install --id Google.Chrome --exact --silent --accept-package-agreements --accept-source-agreements` verwenden.

Die ignorierten M0-Fixtures unter `temp/webfrontend-spikes/` dienen ausschließlich als Nachweisreferenz. Produktions- oder Testcode wird nicht daraus kopiert; der jeweilige Umsetzungstask implementiert gegen die hier festgelegten Verträge und übernimmt nur verifizierte Golden-Master-Daten, soweit deren Lizenz und Herkunft dies erlauben.

## Änderungsregel für Agenten

Vor dem Anlegen einer Datei beantwortet der ausführende Agent in dieser Reihenfolge:

1. Welcher fachliche Use Case wird geändert?
2. Ist die Änderung Domain, Application, Persistence, MCP, Web, PDF-Infrastruktur oder Test?
3. Welcher vorhandene Featureordner ist laut diesem Dokument zuständig?
4. Existiert bereits ein Typ mit derselben Verantwortung?
5. Sind neue Struktur und Namespace in diesem Dokument bereits erlaubt?

Ist Punkt 3 oder 5 nicht eindeutig, wird zuerst dieses Konzept präzisiert oder eine offene Frage dokumentiert. Es wird kein generischer Sammelordner als Ausweichlösung angelegt.

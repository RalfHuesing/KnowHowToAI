# Konfiguration und Betrieb

## Konfigurationstrennung

Die KnowHowTo-AI-Datenbank enthält ausschließlich Wissen und die zu seiner
Versionierung, Bearbeitung und Freigabe benötigten Daten (Snapshots,
Transactions, Zielgruppen, Zielgruppen-Auflösungsreihenfolgen, Releases). Nicht versionierte
Betriebs-, Retrieval- oder Quality-Konfiguration wird nicht in der
Wissensdatenbank gespeichert.

Die Konfiguration ist strikt nach Verantwortlichkeit getrennt:

1. **Harte fachliche Invarianten** sind nicht abschaltbar und nicht
   konfigurierbar: Heading-Verbot, Transaction-Pflicht, unveränderliche committed
   Snapshots, höchstens ein Root, zyklenfreie Hierarchie und Content
   Dependencies, exakt ein Treffer bei `replace_text`
   ([Invarianten](Invarianten.md)).
2. **Quality-Policies** warnen, blockieren aber keinen fachlich gültigen Commit.
3. **Retrieval-/Transportgrenzen** begrenzen Seiten- und Snippetgrößen.
4. **Storage-/Startparameter** steuern SQL-Timeout und Migrationsverhalten.
5. **Datenbankverbindungen**: Die versionierten Sektionen `DatabaseConnection`,
   `BrowserTestDatabaseConnection` und `BrowserVisualTestDatabaseConnection` in
   `appsettings.json` enthalten jeweils `Server`, `Database`, `UserName`,
   `Password`, `UseWindowsAuthentication`. Erstere ist die Verbindung eines
   normalen Serverprozesses. `BrowserTestDatabaseConnection` bezeichnet
   ausschließlich die manuell bereitgestellte Workflow-Datenbank
   `KnowHowToAi_BrowserTests`; `BrowserVisualTestDatabaseConnection` bezeichnet
   den separaten minimalen Datenbestand `KnowHowToAi_Test` für bytegenaue
   Shell-Baselines. Der Browser-Testhost liest die jeweils benötigte Sektion
   ausschließlich aus der versionierten Datei und übergibt ihre fünf Werte nur
   prozesslokal als Environment-Overrides der veröffentlichten Test-EXE auf
   `DatabaseConnection`. Damit bleibt die Produktbindung unverändert, die
   Browser-Suite benötigt nach Rechnerneustart keine gesetzte Testumgebung und
   Zugangsdaten stehen nie in Prozessargumenten oder Testdiagnosen. Alle
   Serverwerte dürfen
   Windows-Umgebungsplatzhalter enthalten (Default
   `%COMPUTERNAME%\MSSQLSERVER2022`, erst zur Laufzeit expandiert). Es gibt
   **keine** alternative Connection-String-Umgebungsvariable, User Secrets oder
   Secretstore für die Verbindung. Alle produktiven Verbindungen tragen den festen
   `ApplicationName` `KnowHowToAi` (in `SqlConnectionFactory` gesetzt, bestehende
   Werte im Connection String werden überschrieben) — das erlaubt eindeutige
   Zuordnung in SQL Profiler und `sys.dm_exec_sessions`.

## Schlüssel, Defaults und Bereiche

Alle Schlüssel liegen unter `KnowHowToAI` in der zentralen `appsettings.json`
(Quelldatei enthält erläuternde Kommentare):

| Schlüssel | Default | Gültiger Bereich | Bedeutung |
|---|---:|---:|---|
| `Validation:ContentSizeWarningBytes` | `4096` | `512..1048576` | `NodeTooLarge` ab normalisierter UTF-8-Größe |
| `Validation:ChildCountWarning` | `25` | `2..10000` | `TooManyChildren` für einen Parent |
| `Validation:HierarchyDepthWarning` | `8` | `2..256` | `HierarchyTooDeep` ab globaler Node-Tiefe |
| `Validation:PossibleEmbeddedHeadingWarning` | `true` | Boolean | heuristische Warnung für Ersatztitel |
| `Retrieval:DefaultPageSize` | `20` | `1..MaximumPageSize` | Standardseite für Listen |
| `Retrieval:MaximumPageSize` | `100` | `1..1000` | größte akzeptierte Listenseite |
| `Retrieval:SearchPageSize` | `10` | `1..SearchMaximumPageSize` | Standardseite für Search |
| `Retrieval:SearchMaximumPageSize` | `50` | `1..500` | größte akzeptierte Search-Seite |
| `Retrieval:SnippetMaximumCharacters` | `300` | `50..4000` | maximale Länge eines Search-Snippets |
| `Storage:CommandTimeoutSeconds` | `30` | `1..600` | SQL-Command-Timeout |
| `Migrations:LockTimeoutSeconds` | `60` | `1..600` | Warten auf Migration-Lock |
| `Migrations:ApplyOnStartup` | `true` | Boolean | ausstehende Migrationen beim Start anwenden |

Dazu `Logging:MinimumLevel` (Default `Information`) und das optionale
`Logging:FilePath` (täglich rotierende Protokolldatei; `null` = rein stderr).

## Bindung und Validierung

Die effektive Override-Reihenfolge ist:
`appsettings.json` < `appsettings.{Environment}.json` < Environment-Variablen
(`KnowHowToAI__`-Präfix mit Doppelunterstrich, z. B.
`KnowHowToAI__Retrieval__MaximumPageSize=200`) < Kommandozeilenargumente.

- Optionen werden einmalig beim Prozessstart an immutable typed Options gebunden
  und vollständig validiert (Werte, Bereiche, feldübergreifende Beziehungen).
  Ungültige Werte verhindern den Start mit klarem Fehler (fail-fast). Kein
  Live-Reload; Änderungen wirken nach einem Prozessneustart.
- Defaultwerte stehen genau einmal in der versionierten `appsettings.json`;
  Validierungsbereiche stehen genau einmal in zentralen `*OptionsValidator`-Typen.
  Typed Options besitzen keine versteckten Fallbackwerte.
- Domain, Application und Storage kennen weder `IConfiguration` noch frei
  verteilte Konfigurationsschlüssel; der Composition Root übergibt fertig
  validierte Options-/Policy-Records. Dadurch entstehen im Fachcode keine Magic
  Numbers. Tests dürfen abweichende Options explizit injizieren; auch Tests
  enthalten keine Magic Numbers.
- Warnungen geben den effektiv verwendeten Grenzwert und den Ist-Wert zurück.

`Migrations:ApplyOnStartup` führt ausstehende Schema-Migrationen genau einmal vor
der Betriebsbereitschaft aus; bei Deaktivierung wird der Schritt sicher
übersprungen. Ein Migrationsfehler verhindert den Start und spiegelt keine
Credentials.

## Protokollierung

Der Server läuft als ASP.NET-Core-Webhost auf Kestrel. Scheme, Adresse und Port
stammen ausschließlich aus der normalen ASP.NET-Core-Hostkonfiguration und ihren
Kommandozeilen-Overrides; der Server definiert weder eine eigene Portoption noch
einen zweiten Listener. MCP ist als stateless Streamable HTTP unter `/mcp`
erreichbar; Legacy-SSE ist deaktiviert (Standard des SDK).
Blazor Interactive Server bedient die minimale Shell auf `/` über
denselben Origin. Der Shell-Read ruft die Application-Schicht direkt per DI auf
und verwendet keinen HTTP-Loopback.
Protokollausgaben gehen ausschließlich nach `stderr` und optional in die
konfigurierbare, täglich rotierende Datei.

Geheimnisse (Passwörter, Verbindungszeichenfolgen) und vollständige
Content-Payloads werden niemals protokolliert; sensitive Werte erreichen die
Protokollierung als strukturierte Eigenschaften mit sensitivem Namen und werden
vom Protokollierungsaufbau redigiert, bevor ein Kanal schreibt. Der MCP-Transport
schreibt Roh-Protokollnachrichten ausschließlich auf Trace-Ebene; der
Betriebsdefault (`Information`) liegt darüber.

Konfigurationsfehler werden vor einer Kestrel-Bindung validiert; aktivierte
Schema-Migrationen laufen ebenfalls vor der Betriebsbereitschaft. Fehler in
Konfiguration, Migration oder Kestrel-Start führen zu `StartupFailure`.
Herunterfahren und Abbruch (SIGTERM/Ctrl+C) werden kontrolliert behandelt und
führen zu einem geordneten Herunterfahren mit stabilem Exitcode `Success`, ohne
unbehandelte Exception.

## Build, Tests und Linter

```text
pwsh -NoProfile -File scripts/build.ps1
pwsh -NoProfile -File scripts/publish.ps1
```

`scripts/build.ps1` führt vor dem Solution-Build den Server-Targetschritt für
die Frontend-Assets aus: `npm ci --ignore-scripts` stellt ausschließlich aus
`src/KnowHowToAI.Server/Frontend/package-lock.json` wieder her und
`npm run build` ruft ausschließlich `Frontend/build.mjs` auf. Der Build erzeugt
`wwwroot/generated/content-editor/content-editor.js`; `scripts/publish.ps1`
veröffentlicht dieselbe lokale Datei als Static Web Asset. Node/npm sind dafür
Buildvoraussetzungen und keine Runtime-Abhängigkeiten. `Frontend/node_modules`
und `wwwroot/generated` bleiben unversioniert. Die Solution ist `.slnx`; der
Build muss fehler- und warnungsfrei sein (`TreatWarningsAsErrors`). Das
zugehörige direkte und transitive npm-Lizenzinventar einschließlich
NOTICE-Prüfung steht in `THIRD-PARTY-NOTICES.md`.

Testebenen (Details: `.agents/rules/TestRichtlinien.mdc`):

```text
pwsh -NoProfile -File scripts/test-fast.ps1                       # Category=Unit (Core, Integration und Razor-Shell)
pwsh -NoProfile -File scripts/test-integration.ps1                # Category=Integration (Serverstart, MCP-Verträge, Browser-Shell)
pwsh -NoProfile -File scripts/test-integration.ps1 -Filter 'Category=ManualDatabaseIntegration'
```

Das FastTest-Skript stellt zusätzlich die lokale Frontend-Toolchain per
`npm ci --ignore-scripts` wieder her und führt `npm test` (Vitest) vor den
.NET-Testprojekten aus. Damit werden die zustandsbehaftete Crepe-Instanzablage,
Callback-Weitergabe und der idempotente Dispose-Adapter bei jedem FastTest-Gate
geprüft.

- Integrationstests mit echtem SQL Server (Kategorie `ManualDatabaseIntegration`)
  binden ausschließlich `BrowserTestDatabaseConnection`. Vor jedem Teststart und
  beim Ende einer frischen Testverbindung bereinigt der Testharness ausschließlich
  `dbo`-Tabellen, Trigger und Fremdschlüssel mit `KnowHowToAI_`-Präfix in genau
  dieser Browser-Testdatenbank. `DatabaseConnection` wird von dieser Suite weder
  als Cleanup-Ziel verwendet noch bereinigt; vor jedem Cleanup werden die
  aufgelösten Produkt-, Workflow- und Visualziele ohne Credentials normalisiert
  verglichen. Identität mit der Produktverbindung sowie `master`, `model`, `msdb`
  und `tempdb` werden fail-fast abgewiesen; identische Browserziele werden
  dedupliziert. Ein gemeinsamer Fallback ist ausgeschlossen. Der Harness erzeugt
  oder entfernt niemals Datenbanken oder Schemas. Fehlende SQL-
  Voraussetzungen sind ein klarer Preflight-Fehler, kein grüner Skip. Das
  Integrationsskript führt diesen Preflight ausschließlich für den expliziten
  `ManualDatabaseIntegration`-Lauf aus, damit ein Browser-Gate nicht von der
  Produktdatenbank abhängt.
- Der Browser-Shell-Smoke verlangt Google Chrome Stable (installierte aktuelle
  Version) im headless `chrome`-Channel. Eine fehlende Installation ist ein
  Preflight-Fehler; Chromium oder ein anderer Browser ist kein Fallback. Der
  Testhost prüft die Installation vor jedem Lauf über den Windows-Uninstall-
  Eintrag; eine nichtinteraktive Bereitstellung ist über
  `winget install --id Google.Chrome --exact --silent --accept-package-agreements --accept-source-agreements`
  möglich. Die Version selbst wird nicht festgenagelt, jede installierte
  Stable-Version ist zulässig.
- Gezielte Browser-Smoke-Läufe:
  `pwsh -NoProfile -File scripts/test-integration.ps1 -Filter 'FullyQualifiedName~SmokeTests'`
  beziehungsweise
  `dotnet test tests/KnowHowToAI.BrowserTests/KnowHowToAI.BrowserTests.csproj`.
  Die Abhängigkeits- und Befehlsregeln stehen im Strukturkonzept unter
  „Feste Testabhängigkeiten und Befehle“
  (`tasks/webfrontend/konzept/08-projektstruktur-und-codekonventionen.md`).
- Die funktionalen Browser-Smokes verwenden ausschließlich die manuell
  bereitgestellte `BrowserTestDatabaseConnection` (`KnowHowToAi_BrowserTests`)
  und starten die veröffentlichte Server-EXE mit
  `Migrations:ApplyOnStartup=true`. Ihr Host migriert und initialisiert diese
  dedizierte Datenbank vor der Betriebsbereitschaft selbst. Die gemeinsame
  Workflow-Fixture ergänzt anschließend ausschließlich über den realen
  MCP-Transport einen deterministischen Bestand mit `Default`,
  `BrowserDownloadAudience`, exportierbarem Teilbaum, Historienständen und Release.
  Diese Seedoperation und Browser-Smokes, die eine Working Transaction öffnen,
  teilen einen schmalen Prozess-Gate; die erzeugte Transaction wird im `finally`
  über `discard_transaction` auf dem echten MCP-Produktpfad verworfen. Da auch
  dedizierte Browser-Smoke-Hosts dasselbe manuell bereitgestellte Workflowziel
  bereinigen, deaktiviert das Browser-Testprojekt seine xUnit-Parallelisierung;
  damit überlappen Cleanup, Seed und laufende Browserflüsse nie.
- Visuelle Shell-Baselines verwenden ausschließlich
  `BrowserVisualTestDatabaseConnection` mit einem eigenen Host und dem stabilen,
  minimalen Bestand. Workflow-Smokes beschreiben diese Datenbank nie. Ihr
  Testhost darf vor dem Start ebenfalls ausschließlich `dbo`-Objekte mit
  `KnowHowToAI_`-Präfix in diesem exakt konfigurierten Ziel bereinigen.
  `DatabaseConnection` bleibt für alle Testharness-Cleanup-Pfade unerreichbar;
  die vier SQL-Systemdatenbanken, nicht konfigurierte Ziele und Datenbank-/Schema-
  Erzeugung oder -Löschung werden durch den gemeinsamen Guard ausgeschlossen.
  Identische Browserziele werden dedupliziert.
  Ein bewusst ausgeführter Host-Neustart innerhalb desselben Browserflows kann
  den Cleanup gezielt überspringen, damit offene Working Transactions über den
  veröffentlichten Hostprozess hinweg erhalten bleiben; der Neustart verwendet
  weiterhin ausschließlich das bereits gewählte Browser-Testziel.
- Visuelle Shell-Baselines: Die Smoke-Klasse `VisualShellSmokeTests` vergleicht
  die Shell bei 1280 × 720 und 1024 × 720 gegen die versionierten PNG-Baselines
  unter `tests/KnowHowToAI.BrowserTests/TestSupport/Baselines/`. Im regulären
  Lauf werden Baselines nur verglichen und niemals automatisch aktualisiert;
  Abweichungen schlagen den Test mit Pfadangaben fehl. Nach einer beabsichtigten
  UI-Änderung wird der aktuelle Screenshot aus `temp/visual-shell/` nach einer
  manuellen Diff-Prüfung bewusst in die Baseline übernommen (per `git add`).
  Volatile Inhalte (Animationen, Caret, Schriftnachladung) maskiert der Test
  vor der Aufnahme.
- On-demand UI-Audit-Aufnahmen werden ausschließlich über
  `pwsh -NoProfile -File scripts/capture-ui-audit.ps1` gestartet. Der Runner
  verwendet den separaten xUnit-Filter `Category=UiAudit`. Bei normaler
  Testausführung wird der Test zwar entdeckt, ohne Aktivierungsvariable aber
  übersprungen und startet keinen Host oder Browser. Die Standardausgabe liegt unter
  `temp/ui-audit/<yyyy-MM-dd_HH-mm-ss>/`; sie enthält semantisch
  benannte PNGs für den festen Desktop-Viewport (1280 × 800) sowie
  ein `manifest.json`. Mit `-OutputRoot <Verzeichnis>` lässt sich bewusst ein
  anderer Zielort schreiben. Der Runner nutzt dieselbe Chrome-/Host-/MCP-
  Infrastruktur wie die Browser-Smokes, verwendet die dedizierte
  `BrowserVisualTestDatabaseConnection` mit dem minimalen Visual-Shell-Bestand,
  maskiert volatile Werte und schreibt keine Baselines. Die temporären
  Artefakte sind nicht für die Versionierung vorgesehen.
- Teilnachweis: `pwsh -NoProfile -File scripts/test-integration.ps1 -Filter
  'FullyQualifiedName~<Testklasse>'` führt nur berührte SQL-Tests aus.
- Skriptausgaben in eine Logdatei umleiten und die Datei auswerten, statt pwsh
  direkt durch eine Pipe zu schicken.

Linter-Gate: AiNetLinter-MCP `verify` mit absolutem `targetPath` zur `.slnx`.
Für Abschlüsse gilt ausschließlich `verify(targetPath, scope: "solution")` mit
`verdict=pass`, `score=10.0` und `violationCount=0`. Während der Arbeit genügt
`verify(targetPath)` ohne Zusatzparameter. Workflow:
`.agents/rules/AiNetLinter-McpWorkflow.mdc`.

Für reine Dokumentations-, Markdown- oder Agentenregel-Änderungen sind Build und
Tests nicht erforderlich; `git diff --check` und eine Diff-Prüfung genügen.

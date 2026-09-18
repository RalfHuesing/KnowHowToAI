# Konfiguration und Betrieb

## Konfigurationstrennung

Die KnowHowTo-AI-Datenbank enthält ausschließlich Wissen und die zu seiner
Versionierung, Bearbeitung und Freigabe benötigten Daten (Snapshots,
Transactions, Rollen, Role Resolution Orders, Releases). Nicht versionierte
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
5. **Datenbankverbindung**: eigene, versionierte `DatabaseConnection`-Sektion in
   `appsettings.json` mit `Server`, `Database`, `UserName`, `Password`,
   `UseWindowsAuthentication`. Der Serverwert darf Windows-Umgebungsplatzhalter
   enthalten (Default `%COMPUTERNAME%\MSSQLSERVER2022`, erst zur Laufzeit
   expandiert). Es gibt **keine** alternative Connection-String-Umgebungsvariable,
   User Secrets oder Secretstore für die Verbindung. Alle produktiven Verbindungen
   tragen den festen `ApplicationName` `KnowHowToAi` (in `SqlConnectionFactory`
   gesetzt, bestehende Werte im Connection String werden überschrieben) — das
   erlaubt eindeutige Zuordnung in SQL Profiler und `sys.dm_exec_sessions`.

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
dotnet build KnowHowToAI.slnx -v q --nologo
```

Die Solution ist `.slnx`. Der Build muss fehler- und warnungsfrei sein
(`TreatWarningsAsErrors`).

Testebenen (Details: `.agents/rules/TestRichtlinien.mdc`):

```text
pwsh -NoProfile -File scripts/test-fast.ps1                       # Category=Unit (Core, Integration und Razor-Shell)
pwsh -NoProfile -File scripts/test-integration.ps1                # Category=Integration (Serverstart, MCP-Verträge, Browser-Shell)
pwsh -NoProfile -File scripts/test-integration.ps1 -Filter 'Category=ManualDatabaseIntegration'
```

- Integrationstests mit echtem SQL Server (Kategorie `ManualDatabaseIntegration`)
  laufen gegen die manuell bereitgestellte, konfigurierte Datenbank; der Harness
  erzeugt oder entfernt keine Datenbanken. Fehlende SQL-Voraussetzungen sind ein
  klarer Preflight-Fehler, kein grüner Skip.
- Der Browser-Shell-Smoke verlangt Google Chrome Stable (installierte aktuelle
  Version) im headless `chrome`-Channel. Eine fehlende Installation ist ein
  Preflight-Fehler; Chromium oder ein anderer Browser ist kein Fallback.
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

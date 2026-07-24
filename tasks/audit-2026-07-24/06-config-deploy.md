# Dimension 6 — Konfiguration & Deployment

> **Vergleichsbasis:** `.agents/rules/06-configuration.mdc` (alles veränderliche nach
> `appsettings.json`), `docs/03-Projektstruktur-und-Konfiguration.md` (Schema, Beispiele,
> bewusste Ausnahmen), `scripts/publish.ps1` und `.github/workflows/release.yml` (Deployment).
> **Methodik:** Stichprobe der `appsettings.json`, `LoadOptions`, `ConfigureLogger`,
> `publish.ps1`, Release-Workflow. Abgleich gegen Konfigurations-Konventionen.

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-CD-001](#f-cd-001) | **High** | `Logging.MinimumLevel` und `Logging.RollingInterval` werden via `Enum.Parse` ohne Validation akzeptiert — Tippfehler in `appsettings.json` führen zu kryptischer Laufzeit-Exception | `Cli/Program.cs:174, 177` |
| [F-CD-002](#f-cd-002) | Medium | `appsettings.json` mit `ConnectionString` inkl. Credentials ist committed — bewusste Ausnahme dokumentiert, aber: kein `appsettings.example.json` als sicherer Default | `Cli/appsettings.json:4` |
| [F-CD-003](#f-cd-003) | Medium | Keine `appsettings.Production.json` / `Development.json`-Unterstützung (Microsoft.Extensions.Configuration hat das eingebaut, ist aber nicht genutzt) | `Cli/Program.cs:155-158` |
| [F-CD-004](#f-cd-004) | Medium | `appsettings.json`-File-Path-Fallback ist hartcodiert (`Path.Combine(AppContext.BaseDirectory, "appsettings.json")`) — kein `dotnet user-secrets`-Support, keine `--secrets`-Flag | `Cli/Program.cs:149` |
| [F-CD-005](#f-cd-005) | Medium | `publish.ps1` ruft `dotnet test` *nicht* vor dem Publish — wenn Tests rot sind, wird trotzdem publiziert | `scripts/publish.ps1:13-19` |
| [F-CD-006](#f-cd-006) | Low | `publish.ps1` hat keine `git clean` / Output-Cleanup — alte Build-Artefakte in `publish/` werden nicht entfernt | `scripts/publish.ps1:13-19` |
| [F-CD-007](#f-cd-007) | Low | `publish.ps1` schreibt Output immer als `KnowHowToAI.Cli.exe` — keine Versionierung im Filename | `scripts/publish.ps1:21` |
| [F-CD-008](#f-cd-008) | Low | `Cli/KnowHowToAI.Cli.csproj` `<Version>1.0.2</Version>` ist statisch — muss manuell für Releases erhöht werden, kein "auto-increment" | `Cli/Cli.csproj:27` |
| [F-CD-009](#f-cd-009) | Info | `--config` und `--target` Options-Defaults sind sinnvoll gewählt | `Cli/Program.cs:26-34` |
| [F-CD-010](#f-cd-010) | Info | `ConnectionString` mit `%COMPUTERNAME%`-Expansion ist clever und sauber dokumentiert | `Cli/Program.cs:163-169`, `docs/03:80` |
| [F-CD-011](#f-cd-011) | Info | `LoadOptions` validiert Config-File-Existenz, gibt klare Fehlermeldung — kein stiller Fallback | `Cli/Program.cs:150-153` |

## Detail-Findings

### F-CD-001 — String-Enum-Validation in `Logging`-Options

**Schweregrad:** High (kryptische Fehlermeldung bei Tippfehler)

**Beobachtung:**
`src/KnowHowToAI.Cli/Program.cs:174, 177`:
```csharp
.MinimumLevel.Is(Enum.Parse<LogEventLevel>(loggingOptions.MinimumLevel))
// ...
rollingInterval: Enum.Parse<RollingInterval>(loggingOptions.RollingInterval),
```

`Enum.Parse<T>("Banana")` wirft `ArgumentException: Requested value 'Banana' was not found.`.
Das passiert erst, *nachdem* `LoadOptions` erfolgreich war — also beim Logger-Setup
in `RunValidate`/`RunImport`/`RunExport`/`RunServer`. Die Exception wird vom
Top-Level-`catch` in `Program.cs` gefangen und führt zu Exit-Code 2 mit der rohen
Exception-Message.

**Szenario:** User kopiert `appsettings.json`, ändert `"MinimumLevel": "Information"`
auf `"MinimumLevel": "information"` (kleingeschrieben) → `Enum.Parse` ist
case-sensitive, wirft. User sieht: `'information' was not found.`

**Bessere Variante:**
```csharp
private static LogEventLevel ParseLogLevel(string value) =>
    Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level)
        ? level
        : throw new InvalidOperationException(
            $"Ungültiger Logging.MinimumLevel '{value}'. " +
            $"Erlaubt: {string.Join(", ", Enum.GetNames<LogEventLevel>())}.");

private static RollingInterval ParseRollingInterval(string value) =>
    Enum.TryParse<RollingInterval>(value, ignoreCase: true, out var interval)
        ? interval
        : throw new InvalidOperationException(
            $"Ungültiger Logging.RollingInterval '{value}'. " +
            $"Erlaubt: {string.Join(", ", Enum.GetNames<RollingInterval>())}.");
```

Plus Unit-Tests:
- `ParseLogLevel_AcceptsLowercaseInput`
- `ParseLogLevel_RejectsInvalidWithAllowedValuesList`

**Detail-Datei:** (nicht erstellt, da Medium im Severity-Range, der Schwellwert für
`_findings/`-Detail-Dateien ist High; siehe Synthese-Sektion)

**Aufwand:** ~20 Minuten + Tests.

---

### F-CD-002 — `appsettings.example.json` als sicherer Default fehlt

**Schweregrad:** Medium (F-DK-005, F-SE-005 Wiederholung mit Architektur-Bezug)

**Beobachtung:**
Die `appsettings.json` ist committed und enthält:
- Den vollen Connection-String mit Credentials (`User Id=Agent;Password=Agent!`)
- Den vollen Pfad zum Dev-Docs-Root (`C:\Daten\Entwicklung\Ralf\KnowHowToAI\demo-docs`)

Beides ist in `docs/03` Zeile 81 als bewusste Ausnahme dokumentiert:
> "Für dieses konkrete lokale Dev-/Demo-Setup hat der Projektverantwortliche das
> Committen explizit freigegeben."

**Risiko-Pattern:**
- Wenn ein neuer Contributor das Repo klont und `appsettings.json` lokal anpasst
  (z.B. seinen eigenen SQL-Server), wird die Anpassung committed → Force-Push oder
  PR mit Credentials-Wechsel. Unschön.
- Wenn das Repo als Vorlage für ein anderes Projekt kopiert wird, sind die
  Credentials in der History.

**Saubere Variante:**
1. `appsettings.json` in `.gitignore` aufnehmen
2. `appsettings.example.json` mit allen Keys + Dummy-Werten committen
3. `LoadOptions` so anpassen, dass es `appsettings.json` *sucht*, und wenn nicht
   gefunden, auf `appsettings.example.json` mit Warnung "no real config, using
   example values" zurückfällt — oder hart fehlschlägt
4. Im README dokumentieren: "Kopiere `appsettings.example.json` nach
   `appsettings.json` und fülle deine Werte"

**Wichtig:** Die Migration ist Breaking — `appsettings.json` ist aktuell in Git
tracked, also ist es in jeder Clone-History. Wer das umstellt, muss `git rm
--cached appsettings.json` machen.

**Aufwand:** ~30 Minuten + Doku-Update.

---

### F-CD-003 — Keine `appsettings.{Environment}.json`-Unterstützung

**Schweregrad:** Medium (Standard-Pattern, fehlt)

**Beobachtung:**
`Microsoft.Extensions.Configuration` hat ein eingebautes Pattern: `appsettings.json`
+ `appsettings.{Environment}.json` (z.B. `appsettings.Production.json`),
wobei `{Environment}` per `DOTNET_ENVIRONMENT` oder `ASPNETCORE_ENVIRONMENT` Env-Var
gesetzt wird.

Aktueller Code (`Program.cs:155-158`):
```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile(path, optional: false)
    .AddEnvironmentVariables()
    .Build();
```

Kein `AddJsonFile($"appsettings.{env}.json", optional: true)`.

**Konsequenz:** Wer Production + Development configs parallel haben will (z.B. in
einem CI-Setup), muss manuell `--config` setzen oder per `ASPNETCORE_ENVIRONMENT`
jonglieren. Aktuell geht das nicht.

**Fix-Empfehlung:**
```csharp
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.GetDirectoryName(path)!)
    .AddJsonFile(Path.GetFileName(path), optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();
```

**Aufwand:** ~15 Minuten + Doku.

---

### F-CD-004 — Kein `dotnet user-secrets`-Support

**Schweregrad:** Medium (sichere Alternative zu committed Credentials)

**Beobachtung:** Die Cli referenziert `Microsoft.Extensions.Configuration` ohne
`Microsoft.Extensions.Configuration.UserSecrets`. Damit gibt es keine
standardmäßige Integration mit `dotnet user-secrets`, was die saubere
Secret-Verwaltung in Dev-Setups wäre.

**Fix-Empfehlung:**
- `<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets"
  Version="10.0.9" />` hinzufügen (nur, wenn `UserSecretsId` in der csproj gesetzt ist)
- `builder.AddUserSecrets<Program>()` in `LoadOptions` (nur im Development-Env)

**Aufwand:** ~20 Minuten.

---

### F-CD-005 — `publish.ps1` ruft keine Tests vor dem Publish

**Schweregrad:** Medium (CI-Hygiene)

**Beobachtung:**
`scripts/publish.ps1:13-19` ruft nur `dotnet publish` auf. Kein vorheriger
`dotnet test`. Die `create-release.ps1` macht das (laut `docs/03` Zeile 145-147),
aber `publish.ps1` allein kann ein User aufrufen, um "mal eben" eine lokale
Single-File-Build zu erzeugen — und wenn Tests rot sind, kommt trotzdem eine `.exe`
raus.

**Fix-Empfehlung:**
```powershell
# Vor dotnet publish:
dotnet test -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests sind rot — Publish abgebrochen." }
```
Plus `dotnet build` vorher, falls `--no-build` im Test-Aufruf nicht greift.

**Aufwand:** ~5 Minuten.

---

### F-CD-006 — `publish/`-Cleanup fehlt

**Schweregrad:** Low

**Beobachtung:** `publish.ps1` ruft `dotnet publish --output $output` auf, ohne
vorher `publish/` zu leeren. Wenn der Output-Pfad bereits Dateien aus einem
früheren Build enthält (anderer Runtime, alte DLLs), bleiben die drin.

**Fix-Empfehlung:**
```powershell
if (Test-Path $output) { Remove-Item -Path $output -Recurse -Force }
```

**Aufwand:** ~2 Minuten.

---

### F-CD-007 / F-CD-008 — Output-Naming & Versionierung (Low)

**Beobachtung:**
- `publish.ps1:21` schreibt Output als `$output\KnowHowToAI.Cli.exe` — kein
  Version-Suffix.
- `Cli/Cli.csproj:27` hat `<Version>1.0.2</Version>` als statischen Wert, der
  manuell erhöht werden muss.

**Konsequenz:** Mehrere Builds nebeneinander (z.B. zum Vergleichen) erfordern
manuelles Umbenennen. Das ist *bewusst* für Single-File-Build, der per MCP-Launch-
Config referenziert wird. Nicht-änderungswürdig.

**Aufwand:** 0 (akzeptiert).

---

### F-CD-009 / F-CD-010 / F-CD-011 — Positive Befunde (Info)

`--config` und `--target` Options-Defaults sind sauber, die
`%COMPUTERNAME%`-Expansion ist clever, und `LoadOptions` failt loudly bei
fehlender Config-Datei. Alle drei entsprechen den Doku-Vorgaben in `docs/03`.

---

## Deployment-Pipeline-Bewertung

| Schritt | Tool | Coverage |
| --- | --- | --- |
| Build verifizieren | `dotnet build` | ✅ in `publish.ps1` implizit (publish bricht bei Build-Fail) |
| Tests verifizieren | `dotnet test` | ⚠️ nur in `create-release.ps1`, nicht in `publish.ps1` |
| Lint verifizieren | AiNetLinter | ✅ in `AiNetLinterTests.cs` (läuft bei `dotnet test`) |
| Publish | `dotnet publish` | ✅ in `publish.ps1` |
| GitHub Release | `.github/workflows/release.yml` | ✅ in Release-Workflow |
| Versionierung | manuelle `<Version>` Erhöhung | ⚠️ fehleranfällig (manuelles Bump) |
| Output-Cleanup | `Remove-Item` | ❌ fehlt in `publish.ps1` |
| Smoke-Test gegen echten SQL Server | manuell | ⚠️ offen (Roadmap DoD) |

## Zusammenfassung Dim 6

- **11 Findings**, davon 1 × High, 5 × Medium, 2 × Low, 3 × Info.
- **Wichtigster Hebel:** F-CD-001 (String-Enum-Validation) ist klein (~20 Min) und
  verbessert die User-UX bei Config-Fehlern erheblich.
- **Mittelfristig wichtig:** F-CD-002/003/004 sind das Config-Sicherheits-Pattern.
  Wenn das Repo das erste Mal produktiv eingesetzt wird, sollte mindestens eines
  davon umgesetzt sein.
- **Build-Pipeline ist solide**, aber F-CD-005 (Test-vor-Publish) ist eine Quick
  Win.

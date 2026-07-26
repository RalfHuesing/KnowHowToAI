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

- **6 Findings** (nach Prio G-Extraktion), davon 1 × High (PrioA), 0 × Medium, 0 × Low, 3 × Info (positive Befunde).
- **Wichtigster Hebel:** F-CD-001 (String-Enum-Validation) ist klein (~20 Min) und
  verbessert die User-UX bei Config-Fehlern erheblich.
- **Mittelfristig wichtig:** F-CD-002/003/004 sind das Config-Sicherheits-Pattern.
  Wenn das Repo das erste Mal produktiv eingesetzt wird, sollte mindestens eines
  davon umgesetzt sein.
- **Build-Pipeline ist solide**, aber F-CD-005 (Test-vor-Publish) ist eine Quick
  Win.

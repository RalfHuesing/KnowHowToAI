# Audit Prio G — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** PrioA (umgesetzt), PrioB-F (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (52 Findings nach Prio A-F) wurden die 5 Findings extrahiert, die unter „Config-Deploy (Rest Dim 6)" zusammengefasst sind. Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand |
|---|---|---|---|
| [F-CD-002](#f-cd-002--appsettingsexamplejson-als-sicherer-default-fehlt) | `appsettings.example.json` als sicherer Default fehlt | Medium | ~30 Min + Doku |
| [F-CD-003](#f-cd-003--keine-appsettingsenvironmentjson-unterstützung) | Keine `appsettings.{Environment}.json`-Unterstützung | Medium | ~15 Min + Doku |
| [F-CD-004](#f-cd-004--kein-dotnet-user-secrets-support) | Kein `dotnet user-secrets`-Support | Medium | ~20 Min |
| [F-CD-005](#f-cd-005--publishps1-ruft-keine-tests-vor-dem-publish) | `publish.ps1` ruft keine Tests vor dem Publish | Medium | ~5 Min |
| [F-CD-006](#f-cd-006--publish-cleanup-fehlt) | `publish/`-Cleanup fehlt | Low | ~2 Min |

**Gesamt-Aufwand:** ~75 Min (10 Min Code + 30 Min Build/Config + 35 Min Doku). Aufteilbar in 2-3 Commits.

**Leitidee:** Production-readiness für Config-Setup: Standard-Pattern (`appsettings.{Environment}.json`, `user-secrets`), sichere Defaults (`appsettings.example.json` als Template), CI-Hygiene (Tests vor Publish).

---

## F-CD-002 — `appsettings.example.json` als sicherer Default fehlt

> **Schweregrad:** Medium · **Dimension:** Konfiguration + Doku
> **Datei:** `src/KnowHowToAI.Cli/appsettings.json` (löschen) + `src/KnowHowToAI.Cli/appsettings.example.json` (neu) + `.gitignore` (anpassen) + `docs/03` (Doku)

### Problem

`appsettings.json` ist committed und enthält:
- Den vollen Connection-String mit Credentials (`User Id=Agent;Password=Agent!`)
- Den vollen Pfad zum Dev-Docs-Root (`C:\Daten\Entwicklung\Ralf\KnowHowToAI\demo-docs`)

Beides ist in `docs/03` Zeile 81 als bewusste Ausnahme dokumentiert:
> "Für dieses konkrete lokale Dev-/Demo-Setup hat der Projektverantwortliche das Committen explizit freigegeben."

**Risiko-Pattern:**
- Wenn ein neuer Contributor das Repo klont und `appsettings.json` lokal anpasst (z.B. seinen eigenen SQL-Server), wird die Anpassung committed → Force-Push oder PR mit Credentials-Wechsel.
- Wenn das Repo als Vorlage für ein anderes Projekt kopiert wird, sind die Credentials in der History.

### Fix-Empfehlung

1. `appsettings.json` in `.gitignore` aufnehmen
2. `appsettings.example.json` mit allen Keys + Dummy-Werten committen
3. Im README dokumentieren: "Kopiere `appsettings.example.json` nach `appsettings.json` und fülle deine Werte"

**Breaking:** `appsettings.json` ist aktuell in Git tracked, also in jeder Clone-History. Wer das umstellt, muss `git rm --cached appsettings.json` machen.

### Aufwand

- ~30 Min + Doku
- 1 Commit

### Risiko

Mittel. Breaking für alle Contributors mit `appsettings.json` im Working-Tree.

---

## F-CD-003 — Keine `appsettings.{Environment}.json`-Unterstützung

> **Schweregrad:** Medium · **Dimension:** Konfiguration
> **Datei:** `src/KnowHowToAI.Cli/Program.cs:155-158`

### Problem

`Microsoft.Extensions.Configuration` hat ein eingebautes Pattern: `appsettings.json` + `appsettings.{Environment}.json` (z.B. `appsettings.Production.json`), wobei `{Environment}` per `DOTNET_ENVIRONMENT` oder `ASPNETCORE_ENVIRONMENT` Env-Var gesetzt wird.

Aktueller Code:
```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile(path, optional: false)
    .AddEnvironmentVariables()
    .Build();
```

Kein `AddJsonFile($"appsettings.{env}.json", optional: true)`.

**Konsequenz:** Wer Production + Development configs parallel haben will, muss manuell `--config` setzen oder per Env-Var jonglieren.

### Fix-Empfehlung

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

### Aufwand

- ~15 Min + Doku
- 1 Commit

### Risiko

Niedrig. Additiv: wenn `appsettings.{env}.json` nicht existiert, greift die `appsettings.json`-Default.

---

## F-CD-004 — Kein `dotnet user-secrets`-Support

> **Schweregrad:** Medium · **Dimension:** Konfiguration
> **Datei:** `src/KnowHowToAI.Cli/KnowHowToAI.Cli.csproj` (PackageReference) + `Program.cs:LoadOptions`

### Problem

Die Cli referenziert `Microsoft.Extensions.Configuration` ohne `Microsoft.Extensions.Configuration.UserSecrets`. Damit gibt es keine standardmäßige Integration mit `dotnet user-secrets`, was die saubere Secret-Verwaltung in Dev-Setups wäre.

### Fix-Empfehlung

```xml
<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.9" />
```

```csharp
// In LoadOptions (nur im Development-Env):
if (environment == "Development")
{
    builder.AddUserSecrets<Program>();
}
```

Plus `<UserSecretsId>`-GUID in der csproj.

### Aufwand

- ~20 Min
- 1 Commit (kann mit F-CD-003 kombiniert werden)

### Risiko

Niedrig. User-Secrets ist Dev-only, kein Production-Impact.

---

## F-CD-005 — `publish.ps1` ruft keine Tests vor dem Publish

> **Schweregrad:** Medium · **Dimension:** CI-Hygiene
> **Datei:** `scripts/publish.ps1`

### Problem

`scripts/publish.ps1:13-19` ruft nur `dotnet publish` auf. Kein vorheriger `dotnet test`. Die `create-release.ps1` macht das (laut `docs/03` Zeile 145-147), aber `publish.ps1` allein kann ein User aufrufen, um "mal eben" eine lokale Single-File-Build zu erzeugen — und wenn Tests rot sind, kommt trotzdem eine `.exe` raus.

### Fix-Empfehlung

```powershell
# Vor dotnet publish:
dotnet test -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests sind rot — Publish abgebrochen." }
```

Plus `dotnet build` vorher, falls `--no-build` im Test-Aufruf nicht greift.

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-DP-003 aus PrioC kombiniert werden, da beide CI-Hygiene sind)

### Risiko

Keine. Lokales Verhalten wird strenger.

---

## F-CD-006 — `publish/`-Cleanup fehlt

> **Schweregrad:** Low · **Dimension:** CI-Hygiene
> **Datei:** `scripts/publish.ps1`

### Problem

`publish.ps1` ruft `dotnet publish --output $output` auf, ohne vorher `publish/` zu leeren. Wenn der Output-Pfad bereits Dateien aus einem früheren Build enthält (anderer Runtime, alte DLLs), bleiben die drin.

### Fix-Empfehlung

```powershell
if (Test-Path $output) { Remove-Item -Path $output -Recurse -Force }
```

### Aufwand

- ~2 Min
- 1 Commit (kann mit F-CD-005 kombiniert werden)

### Risiko

Keine.

---

## Warum diese 5 und nicht andere?

### Aufgenommen

1. **F-CD-002** — Production-readiness, Standard-Pattern
2. **F-CD-003** — Standard-Pattern, fehlt
3. **F-CD-004** — Sichere Alternative zu committed Credentials
4. **F-CD-005** — CI-Hygiene, trivial
5. **F-CD-006** — CI-Hygiene, trivial

### Bewusst weggelassen (Kurzbegründung)

- **F-CD-007/008 (Output-Naming/Versionierung):** Per Audit "bewusst für Single-File-Build, der per MCP-Launch-Config referenziert wird. Nicht-änderungswürdig."
- **F-CD-009/010/011 (Positive Befunde Info):** Kein Handlungsbedarf.

Alle übrigen Findings (47) gehören thematisch in andere Brocken (H: Code-Quality-Rest, I: Doku-Rest, J: Architektur-Rest, K: Dependencies-Rest, L: Sicherheits-Rest, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-CD-006** + **F-CD-005** (~7 Min) — CI-Hygiene-Bündel
2. **F-CD-003** + **F-CD-004** (~35 Min) — Config-Pattern-Bündel
3. **F-CD-002** (~30 Min) — Breaking Change, separat

**Gesamt-Aufwand in dieser Reihenfolge:** ~75 Min, 3 Commits.

**Commit-Clustering-Vorschlag:**
- Commit 1: F-CD-005 + F-CD-006 (CI-Hygiene)
- Commit 2: F-CD-003 + F-CD-004 (Standard-Config-Pattern)
- Commit 3: F-CD-002 (Breaking: `appsettings.json` aus Git)

## Querverweise zu anderen Brocken

- **F-CD-001 in PrioA** — String-Enum-Validation; verwandt zu F-CD-003 (Config-Pattern).
- **F-SE-005 in PrioD** — Credentials-Doku-Hinweis; passt thematisch zu F-CD-002 (`appsettings.example.json`).
- **F-DP-003 in PrioC** — NuGet-Audit-Mode; verwandt zu F-CD-005 (CI-Hygiene).

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).

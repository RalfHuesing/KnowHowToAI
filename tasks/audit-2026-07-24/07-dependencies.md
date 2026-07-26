# Dimension 7 — Dependencies & NuGets

> **Vergleichsbasis:** Aktuelle NuGet-Versionen (Stand: 2026-07-24), Bekannte CVEs,
> Microsoft-Empfehlungen zu .NET 10 / `Microsoft.Extensions.*` Alignment.
> **Methodik:** Stichprobe der `<PackageReference>`s in beiden `.csproj`-Dateien,
> Web-Recherche zu den aktuellen Stable-Versionen der zwei Preview-Pakete
> (`ModelContextProtocol`, `System.CommandLine`), Recherche zu
> `Microsoft.Data.SqlClient 7.0.2`-Breaking-Changes.
> **Nicht im Scope:** Vollständige CVE-Datenbank-Cross-Check, Lizenz-Audit (MIT/Apache
> etc.) — letzteres ist nicht in den Tooling-Defaults vorgesehen.

## Dependency-Inventar

### `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj`

| Paket | Version | Veröffentlicht | Status | Eigene Recherche |
| --- | --- | --- | --- | --- |
| `Dapper` | `2.1.79` | 2026 | Stable | aktueller Minor-Patch |
| `Microsoft.Data.SqlClient` | `7.0.2` | 2026-Q2 | **Stable, aber Major-Version mit Breaking Changes** | siehe F-DP-002 |
| `YamlDotNet` | `18.1.0` | 2026 | Stable | aktueller Major |

### `src/KnowHowToAI.Cli/KnowHowToAI.Cli.csproj`

| Paket | Version | Veröffentlicht | Status | Eigene Recherche |
| --- | --- | --- | --- | --- |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.9` | 2026 | Stable | passt zu .NET 10 |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | `10.0.9` | 2026 | Stable | passt zu .NET 10 |
| `Microsoft.Extensions.Configuration.Json` | `10.0.9` | 2026 | Stable | passt zu .NET 10 |
| `Microsoft.Extensions.Hosting` | `10.0.9` | 2026 | Stable | passt zu .NET 10 |
| `ModelContextProtocol` | `2.0.0-preview.2` | 2026-06 | **Preview** | Stable `1.4.1` verfügbar, 2.0 hat Breaking Changes für 2026-07-28-Spec (siehe F-DP-001) |
| `Serilog.Extensions.Hosting` | `10.0.0` | 2025-Q4 | Stable | passt |
| `Serilog.Sinks.File` | `7.0.0` | 2024 | Stable | passt |
| `System.CommandLine` | `3.0.0-preview.5.26302.115` | 2026-06 | **Preview** | Stable `2.0.10` verfügbar (siehe F-DP-001) |

### `tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj`

| Paket | Version | Veröffentlicht | Status | Eigene Recherche |
| --- | --- | --- | --- | --- |
| `xunit.v3.mtp-v2` | `3.2.2` | 2026 | Stable | aktueller Major |

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-DP-004](#f-dp-004) | Medium | `Microsoft.Data.SqlClient 7.0.2` entfernt `Azure.Core.dll`/`Azure.Identity.dll` aus Core-Package; falls `MSSQLSERVER2022` mit Azure-Connectivity genutzt würde, wäre `Microsoft.Data.SqlClient.Extensions.Azure` zusätzlich nötig | `Core/Core.csproj:11` |
| [F-DP-005](#f-dp-005) | Low | `<PackageReference>`-Versionen sind alle "fixed" (keine `*` oder `[ , )`-Ranges) — gut für Reproduzierbarkeit, schlecht für automatische Sicherheits-Patches | (alle csproj) |
| [F-DP-006](#f-dp-006) | Low | `ModelContextProtocol.Core` ist transitiv; bei Major-Updates sollte explizit referenziert werden, damit man nicht von transitiver API abhängt | `Cli/Cli.csproj:16` |
| [F-DP-007](#f-dp-007) | Info | `YamlDotNet 18.1.0` ist ein großer Sprung (vorher ~16.x), sollte getestet sein — Build + Tests grün, daher OK | `Core/Core.csproj:12` |
| [F-DP-008](#f-dp-008) | Info | Keine bekannten CVEs in den aktuellen Versionen zum Audit-Zeitpunkt (keine vollständige Datenbank-Cross-Check durchgeführt) | n/a |

## Detail-Findings

---

### F-DP-004 — Azure-Extensions-Package nicht referenziert (Info)

**Beobachtung:** Falls jemals Azure-Connectivity benötigt wird, fehlt
`Microsoft.Data.SqlClient.Extensions.Azure`. Aktuell irrelevant, aber ein
zukünftiges Aufrüsten auf Entra-ID-Auth würde scheitern.

**Aufwand:** 0 jetzt, ggf. ~10 Min wenn nötig.

---

### F-DP-005 — Fixed Versions, keine automatischen Patches

**Schweregrad:** Low (Trade-off, bewusst)

**Beobachtung:** Alle `<PackageReference>`s sind mit fester Version gepinnt. Das ist
bewusst (Reproduzierbarkeit), aber: Sicherheits-Patches in Patch-Versionen werden
nicht automatisch aufgenommen. `dotnet outdated` zeigt verfügbare Updates.

**Empfehlung:** Periodisch (z.B. monatlich) `dotnet outdated` laufen lassen und
entscheiden, ob Minor/Patch-Versionen angehoben werden.

**Aufwand:** 0 (Prozess), ~30 Min pro Update-Zyklus.

---

### F-DP-006 — `ModelContextProtocol.Core` ist transitiv

**Schweregrad:** Low (Sauberkeit)

**Beobachtung:** `ModelContextProtocol` (Hauptpaket) hat `ModelContextProtocol.Core`
als transitive Dependency. Wenn die transitive Version jemals von der explizit
angeforderten abweicht, gibt es Versions-Konflikte.

**Fix-Empfehlung:** `ModelContextProtocol.Core` explizit referenzieren mit der
gleichen Version wie das Haupt-Paket. Vermeidet Transitiv-Rätsel.

**Aufwand:** ~2 Minuten.

---

### F-DP-007 / F-DP-008 — Info

Build + Tests grün mit den aktuellen Versionen. Keine bekannten CVEs. Beide
sind positive Bestätigungen, kein Handlungsbedarf.

---

## Zusammenfassung Dim 7

- **5 Findings** (nach Prio C-Extraktion), davon 0 × High, 0 × Medium, 2 × Low, 3 × Info.
- **F-DP-001** (Preview-Dependencies), **F-DP-002** (SqlClient Breaking Changes), **F-DP-003** (NuGet-Vulnerability-Policy) sind in PrioC extrahiert.
- **Insgesamt ist die Dependency-Lage solide.** Drei Major-Versionen sind aktuell
  (Dapper 2.1, SqlClient 7.0, YamlDotNet 18.1), vier `Microsoft.Extensions.*`-
  Pakete sind auf .NET-10-Linie (10.0.9). Die zwei Preview-Pakete sind die einzigen
  roten Flaggen.

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
| [F-DP-001](#f-dp-001) | **High** | Zwei Preview-Dependencies: `ModelContextProtocol 2.0.0-preview.2`, `System.CommandLine 3.0.0-preview.5` — Stable-Versionen existieren und sind produktionsreif | `Cli/Cli.csproj:16, 19` |
| [F-DP-002](#f-dp-002) | Medium | `Microsoft.Data.SqlClient 7.0.2` Major-Version mit Breaking Changes — `SqlBulkCopy` für SQL Server ≤ 2016 betroffen; Azure.Identity ist extrahiert; nirgends dokumentiert | `Core/Core.csproj:11` |
| [F-DP-003](#f-dp-003) | Medium | Keine `Transitive Dependency Audit` Policy — kein `<AuditMode>` oder `dotnet list package --vulnerable` im Workflow / `publish.ps1` | (fehlt) |
| [F-DP-004](#f-dp-004) | Medium | `Microsoft.Data.SqlClient 7.0.2` entfernt `Azure.Core.dll`/`Azure.Identity.dll` aus Core-Package; falls `MSSQLSERVER2022` mit Azure-Connectivity genutzt würde, wäre `Microsoft.Data.SqlClient.Extensions.Azure` zusätzlich nötig | `Core/Core.csproj:11` |
| [F-DP-005](#f-dp-005) | Low | `<PackageReference>`-Versionen sind alle "fixed" (keine `*` oder `[ , )`-Ranges) — gut für Reproduzierbarkeit, schlecht für automatische Sicherheits-Patches | (alle csproj) |
| [F-DP-006](#f-dp-006) | Low | `ModelContextProtocol.Core` ist transitiv; bei Major-Updates sollte explizit referenziert werden, damit man nicht von transitiver API abhängt | `Cli/Cli.csproj:16` |
| [F-DP-007](#f-dp-007) | Info | `YamlDotNet 18.1.0` ist ein großer Sprung (vorher ~16.x), sollte getestet sein — Build + Tests grün, daher OK | `Core/Core.csproj:12` |
| [F-DP-008](#f-dp-008) | Info | Keine bekannten CVEs in den aktuellen Versionen zum Audit-Zeitpunkt (keine vollständige Datenbank-Cross-Check durchgeführt) | n/a |

## Detail-Findings

### F-DP-001 — Zwei Preview-Dependencies, Stable verfügbar

**Schweregrad:** High (Dependency-Choice, kein akuter Bug, aber Risiko-Pattern)

**Beobachtung:**
`src/KnowHowToAI.Cli/KnowHowToAI.Cli.csproj:16, 19`:
```xml
<PackageReference Include="ModelContextProtocol" Version="2.0.0-preview.2" />
<!-- ... -->
<PackageReference Include="System.CommandLine" Version="3.0.0-preview.5.26302.115" />
```

**Externe Recherche (2026-07-24):**

**`ModelContextProtocol`:**
- Aktuelle Stable: `1.4.1` (2026-06-04)
- Preview im Projekt: `2.0.0-preview.2` (2026-06-26)
- Major-Version 2.0 ist *bewusst* auf 2026-07-28-Spec-Version ausgerichtet. Breaking Changes
  sind absehbar (siehe MCP-Blog "Beta SDKs for the 2026-07-28 MCP Spec Release").
- Empfehlung der SDK-Maintainer: "For any critical workloads, the stable SDK releases
  remain the recommended versions."
- **C# SDK ist seit Feb 2026 "left preview"** (Stable 1.0), aktuelle Stable ist 1.4.1.
- Breaking Changes in 2.0: Deprecation von `roots`, `sampling`, `logging` Capabilities;
  `EnableLegacySse` als obsolet markiert; HTTP-Transport-Default auf Stateless-Mode.

**`System.CommandLine`:**
- Aktuelle Stable: `2.0.10` (2026-06-09)
- Preview im Projekt: `3.0.0-preview.5.26302.115` (2026-06-09)
- 3.0 ist seit über einem Jahr in Preview-Phase (erste Preview: 2026-02). Issue #2500 auf
  GitHub: "Roadmap for release?" — Diskussion ob es jemals Stable wird.
- 2.0.x ist die breit genutzte Stable, mit ~231k Downloads für 2.0.9.

**Warum die Preview-Wahl?**
- `System.CommandLine 3.0` bringt voraussichtlich den "Powderhouse"-Source-Generator, der
  die API-Definition deklarativ macht. Aber: nicht GA, nicht kritisch für v1.
- `ModelContextProtocol 2.0`: bringt die neue 2026-07-28-Spec, die 4 Tage nach dem
  Audit-Datum final wird. Wenn das Repo *vor* 28.7. nochmal releasen will, ist 2.0-preview
  vermutbar in Ordnung. Nach 28.7. ist Stable 2.0 vermutlich verfügbar.

**Empfehlung:**
1. `System.CommandLine` auf `2.0.10` downgraden, wenn keine 3.0-spezifischen Features
   benötigt werden. Build + Tests sollten mit 2.0.10 weiterhin grün sein.
2. `ModelContextProtocol` auf `1.4.1` (Stable) lassen, bis 2.0.0 Stable verfügbar ist
   (vermutlich Ende Juli / Anfang August 2026 nach Spec-Release). Bei 2.0.0-Stable-
   Release: gezielter Major-Version-Bump mit Changelog-Review.

**Detail-Datei:** [`_findings/F-DP-001-preview-dependencies.md`](_findings/F-DP-001-preview-dependencies.md)

**Aufwand:** ~10 Minuten für beide Downgrades + Test-Run.

---

### F-DP-002 — `Microsoft.Data.SqlClient 7.0.2` Breaking Changes (undokumentiert)

**Schweregrad:** Medium (für aktuellen Use-Case irrelevant, aber Risiko-Lücke)

**Beobachtung:**
`src/KnowHowToAI.Core/KnowHowToAI.Core.csproj:11`:
```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.2" />
```

**Breaking Changes in 7.0 (laut Microsoft-Blog "Microsoft.Data.SqlClient 7.0 Is Here",
2026-Q2):**

1. **`Azure.Identity` ist NICHT MEHR transitive Dependency** des Core-Pakets. Wer
   Entra-ID-Authentifizierung braucht, muss explizit `Microsoft.Data.SqlClient.Extensions.Azure`
   referenzieren. Für dieses Projekt (SQL-Login, kein Entra-ID) **irrelevant**.
2. **`SqlBulkCopy` bricht auf SQL Server 2016**: dynamische Spalten-Metadata-Query
   (graph_type-Column gibt es erst ab SQL 2017). Issue #3714 dokumentiert. Behoben in
   7.0.1, aber 7.0.0 wirft `Invalid column name 'graph_type'`.
3. **`SqlVector` ist jetzt `readonly struct` (vorher `class`)**: Breaking Change für
   Code, der `SqlVector<float>` als `class` verwendet. **Für dieses Projekt irrelevant**
   (kein Vektor-Datentyp in Verwendung).
4. **`Packet Multiplexing` default disabled**: kann via App-Context-Switches aktiviert
   werden. Performance-relevant, aber Default ist konservativ.
5. **CER (Constrained Execution Region) Cleanup geändert**: Connection-Pool-Cleanup
   verhält sich leicht anders. Edge-Case.
6. **`ActiveDirectoryPassword` als Obsolete markiert**: Migration zu Interactive, Service
   Principal, Managed Identity oder Device Code Flow. **Für dieses Projekt irrelevant**.

**Konsequenz für KnowHowToAI:**
- Aktueller Use-Case (lokales SQL-Server, SQL-Login `Agent`): **keinerlei Impact**.
- Falls das Repo auf eine Azure-SQL-Instanz mit Entra-ID-Auth erweitert wird, ist
  F-DP-004 relevant (Extensions-Package hinzufügen).
- Falls auf SQL Server 2016 deployed wird (unwahrscheinlich): `SqlBulkCopy` würde
  scheitern. Aber: aktuell wird kein `SqlBulkCopy` im Code verwendet — alle Inserts
  gehen via Dapper `ExecuteAsync` in einer Schleife.

**Empfehlung:**
- Doku in `docs/03` Abschnitt 2 hinzufügen: "Microsoft.Data.SqlClient 7.0+ ist gepinnt;
  siehe Release-Notes für Breaking Changes. Bei SQL-Server-Versionen ≤ 2016 ist 6.x zu
  verwenden (kein automatisches Downgrade)."
- Optional: Pinning auf 7.0.2 (nicht 7.0.0) ist korrekt, weil 7.0.1 den SqlBulkCopy-Fix
  bringt.

**Aufwand:** ~5 Minuten Doku.

---

### F-DP-003 — Keine `dotnet list package --vulnerable` Policy

**Schweregrad:** Medium (CI-Hygiene)

**Beobachtung:** Weder in `publish.ps1` noch in `.github/workflows/release.yml` ist
ein Audit-Lauf für vulnerable Packages vorgesehen. NuGet hat seit .NET 8 einen
`<AuditMode>` in der csproj, der beim `dotnet restore` automatisch prüft.

**Fix-Empfehlung:**
1. In beiden csproj-Dateien: `<NuGetAuditMode>direct</NuGetAuditMode>` setzen
2. Optional: `<NuGetAuditLevel>high</NuGetAuditLevel>` (oder `moderate`/`low`)
3. Im CI-Workflow: `dotnet restore --audit` zusätzlich laufen lassen
4. Im `publish.ps1`: `dotnet list package --vulnerable --include-transitive` als
   Smoke-Check vor `dotnet publish`

**Aufwand:** ~15 Minuten.

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

- **8 Findings**, davon 1 × High, 3 × Medium, 2 × Low, 2 × Info.
- **Wichtigster Hebel:** F-DP-001 (Preview-Dependencies downgraden oder bewusst
  dokumentieren) — 10 Minuten Aufwand, klares Risiko-Profil.
- **Mittelfristig:** F-DP-003 (`<NuGetAuditMode>` aktivieren) ist Standard-CI-Hygiene.
- **Insgesamt ist die Dependency-Lage solide.** Drei Major-Versionen sind aktuell
  (Dapper 2.1, SqlClient 7.0, YamlDotNet 18.1), vier `Microsoft.Extensions.*`-
  Pakete sind auf .NET-10-Linie (10.0.9). Die zwei Preview-Pakete sind die einzigen
  roten Flaggen.

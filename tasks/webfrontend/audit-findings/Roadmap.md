# Roadmap: Audit-Findings Webfrontend M1 + M2

Stand: 2026-09-19

Status: Planungsartefakt; `docs/` beschreibt ausschließlich den implementierten Ist-Zustand.

Ursprung: Audit `temp/webfrontend-audit-M1-M2.md` (2026-09-19, Wegwerf-Ablage). Diese Roadmap ist selbstständig ausführbar; das Audit-Dokument wird dafür nicht benötigt.

## Zweck und Grenzen

Diese Roadmap beseitigt Befunde aus dem M1/M2-Audit: Vertragsabweichungen in der Browser-Testinfrastruktur, eine undokumentierte Playwright-Nutzung in `KnowHowToAI.Web.Tests`, eine nicht spiegelnde Testordnerstruktur und eine fehlende Dirty-State-Brücke. Sie enthält **keine** Fachfeatures aus M3 bis M8 und zieht nichts davon vor.

Ausführung wie die Haupt-Roadmap über [`../../.agents/prompts/roadmap-orchestrator.md`](../../.agents/prompts/roadmap-orchestrator.md): pro offenem `T`-Punkt genau ein schreibender Subagent, seriell, danach unabhängige Verifikation, erst dann Abhakung per Byte-Ersatz und atomarer Commit.

## Ausführungsmodell

Identisch zum [Ausführungsmodell der Haupt-Roadmap](../Roadmap.md):

- Nur Checkboxen mit `-T` in der ID sind eigenständig ausführbare Leaf-Tasks; alle anderen Checkboxen sind Fortschrittsaggregate.
- Ein Leaf-Task umfasst Analyse, Implementierung, Tests, Ist-Dokumentation und einen atomaren Commit.
- Die Reihenfolge ist AF1 → AF2 → AF3 → AF4. AF3 und AF4 sind voneinander unabhängig, AF2 setzt AF1.2-T1 voraus.

## Gemessene Test-Baseline (2026-09-19)

Diese Zahlen sind der Referenzpunkt für die Performance-Angaben in AF1. Sie wurden mit einem vollen Lauf an diesem Tag erhoben (TRX-Auswertung, Reihenfolge `scripts/test-fast.ps1` sowie direkter BrowserTests-Projektlauf):

| Gate/Projekt | Tests | Summe Testdauern | Wallclock | Schwerste Einzeltests |
|---|---:|---:|---:|---|
| FastTests Core.Tests | 363 | 2,5 s | (Build-dominiert) | ≤ 0,1 s je Test |
| FastTests IntegrationTests (Unit-Anteil) | 263 | 1,8 s | (Build-dominiert) | ≤ 0,1 s je Test |
| FastTests Web.Tests | 126 | 5,8 s | (Build-dominiert) | `DesignTokensShowcaseTests` 1,0 s (Chrome-Start) |
| BrowserTests (voll) | 10 | 112,2 s | 50 s | `ReconnectOverlaySmokeTests.InterruptedConnection` 36,3 s; `ResponsiveShellSmokeTests.KeyboardSequence` 11,9 s |

Bewertung: **Es besteht keine akute Performance-Not.** Das Fast-Gate ist mit rund 10 Sekunden reiner Testdauer gesund; die Wallclock entsteht durch Builds und Test-Hosts, nicht durch Testlogik. Die BrowserTests laufen in 50 Sekunden; 36,3 Sekunden davon entfallen auf einen einzigen Reconnect-Test, dessen Dauer aus der Retry-Planung des Blazor-Frameworks stammt und inhaltlich der Nachweis selbst ist. Die Aufgaben AF1 begründen sich deshalb primär aus **Vertragstreue (M2.4-T3) und Wartbarkeit**; Performance-Gewinne sind eine Nebenwirkung. Kein Task dieser Roadmap darf Tests entfernen oder abschwächen, um Laufzeit zu sparen.

## Milestones

Der Status wird ausschließlich in der jeweiligen Milestone-Datei gepflegt.

| Milestone | Ziel | Abhängigkeit |
|---|---|---|
| [AF1 – BrowserTests-Infrastruktur konsolidieren](01-browsertests-infrastruktur.md) | Serverstart pro Testkollektion, zentrale Chrome- und Interaktivitäts-Helfer, Testdauer-Sichtbarkeit | – |
| [AF2 – Playwright in Web.Tests präzisieren und absichern](02-playwright-webtests-praezisierung.md) | Zielkonzeptdokument korrekt, Chrome-Preflight im Fast-Gate wirksam | AF1.2-T1 |
| [AF3 – Web.Tests-Ordner an Produktionsgrenzen angleichen](03-webtests-ordnerstruktur.md) | Testablagen spiegeln `Web/Components/{Layout,Shared}` und `TestSupport` | – |
| [AF4 – Dirty-State-Brücke zwischen Kontextleiste und Reconnect](04-dirty-state-bruecke.md) | genau eine Dirty-Wahrheit, vor `beforeunload` sichtbar | – |

## Freigabestand

Alle Leaf-Tasks sind am 2026-09-19 geprüft und zur Agentenausführung freigegeben. Die Ausführung beginnt mit AF1.1-T1. Für AF2 gilt die untenstehende dokumentierte Ausnahme von der Konzept-Readonly-Regel; für alle übrigen Tasks bleibt sie unverändert in Kraft.

### Dokumentierte Ausnahme: Konzept-Edit in AF2

Der Orchestrator-Prompt erklärt `konzept/`-Dateien zu readonly. Für **AF2.1-T1 allein** und ausschließlich für die dort wortwörtlich angegebenen Bytes in `tasks/webfrontend/konzept/08-projektstruktur-und-codekonventionen.md` hebt der Benutzer diese Regel mit Erschaffung dieser Roadmap auf: Der Ist-Zustand (Playwright in `Web.Tests`) war nie ins Zielkonzept zurückgeschrieben worden. Der Task enthält die exakte Alt-/Neu-Fassung; der ausführende Agent darf nichts darüber hinaus am Konzept ändern.

## Verbindliche Arbeitsregeln

- Es gelten `AGENTS.md`, `.agents/rules/*.mdc`, die Lese-Matrix in [`docs/README.md`](../../docs/README.md) und [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md) als Pflichtlektüre für jeden Task mit Strukturänderung.
- Keine Fachfeatures aus M3+ vorziehen; keine neuen produktiven Test-Hooks; keine Test-Löschungen zur Laufzeitoptimierung.
- Bekannte Fallstricke aus dem Skill `knowhowtoai-development` sind von jedem Subagenten zu beachten; die relevanten stehen zusätzlich in den jeweiligen Tasks.

# Dimension 5 — Doku vs. Code-Drift

> **Vergleichsbasis:** `.agents/rules/04-docs-reference.mdc` ("`docs/` als Source of Truth"),
> `.agents/rules/05-documentation.mdc` ("Doku beschreibt den aktuellen Stand, nicht mehr
> und nicht weniger"), sowie der Code selbst (für Drift-Erkennung).
> **Methodik:** Stichprobe jeder Code-Komponente gegen die zugehörige `docs/`-Erwähnung.
> Drift = dokumentierter Sachverhalt stimmt nicht (mehr) mit dem Code überein, *oder*
> ein nicht-trivialer Sachverhalt im Code hat keine Doku-Entsprechung.
> **Nicht im Scope:** Rechtschreibprüfung, Sprachstil, Lesbarkeit — nur Inhalts-Drift.

## Drifts-Übersicht

| ID | Schwere | Titel | Doku-Stelle | Code-Stelle |
| --- | --- | --- | --- | --- |
| [F-DK-008](#f-dk-008) | Low | `docs/01-Konzept-und-Workflow.md` (nicht gelesen in diesem Audit, sondern referenziert) erwähnt "Phase 2: Doku erweitern oder umstrukturieren" — laut Roadmap-Punkt 9 wird `docs://authoring-guide` als Resource geliefert; Cross-Check: passt das mit den Slug-Regeln in `docs/04` Zeile 56 überein? | `docs/04:56` | `McpTools/DocsMcpResources.cs:46-50` |
| [F-DK-009](#f-dk-009) | Info | `docs/05-Roadmap.md` Zeile 53 nennt "End-to-End-Verifikation gegen eine befüllte DB" als offen — passt zur aktuellen Realität (kein SQL Server lokal) | konsistent | konsistent |
| [F-DK-010](#f-dk-010) | Info | `docs/05-Roadmap.md` Zeile 76 listet 3 offene DoD-Punkte, alle blockiert durch SQL-Setup-Problem — konsistent mit `docs/03:84` (Bekannter lokaler Stolperstein) | konsistent | konsistent |
| [F-DK-011](#f-dk-011) | Info | `docs/04` Edge-Case 4.3 (Transaktion + Nebenläufigkeit) sagt "READ COMMITTED reicht" — passt zum Standard-Default von SQL Server und zur `SqlDocumentsStore.ReplaceAllAsync`-Implementation | konsistent | konsistent |
| [F-DK-012](#f-dk-012) | Info | `docs/02` Zeile 26 nennt explizit "kein Konsolen-Sink für keines der vier Kommandos" — passt zum `ConfigureLogger` ohne Console-Sink | konsistent | konsistent |

## Detail-Findings

# Audit Prio J — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** PrioA (umgesetzt), PrioB-I (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (39 Findings nach Prio A-I) wurden die 6 Findings extrahiert, die unter „Architektur-Rest (Rest Dim 3)" zusammengefasst sind. Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand |
|---|---|---|---|
| [F-AR-002](#f-ar-002--core-services-ohne-ilogger-injection) | Core-Services ohne `ILogger<T>`-Injection | High (PrioA) | (in PrioA) |
| [F-AR-005](#f-ar-005--keine-zentrale-constants-datei) | Keine zentrale `Constants`-Datei | Medium | ~20 Min (sobald 2. Fall) |
| [F-AR-006](#f-ar-006--microsoftextensionslogging-nicht-in-core) | `Microsoft.Extensions.Logging` nicht in Core | Low | (Folge von F-AR-002) |
| [F-AR-008/009/010](#f-ar-008--f-ar-009--f-ar-010--info-findings) | Info-Findings (Idiomatische Patterns) | Info | 0 |

**Gesamt-Aufwand:** 0 (alle sind Doku/Beobachtungen oder bereits in PrioA). F-AR-005 ist Backlog.

**Leitidee:** Dim 3 schließen. F-AR-002 ist in PrioA, F-AR-005 ist Backlog-Vor-Bote, F-AR-006 ist Folge von F-AR-002, F-AR-008/009/010 sind positive Befunde.

---

## F-AR-002 — Core-Services ohne `ILogger<T>`-Injection

> **Schweregrad:** High · **Dimension:** Architektur
> **Status:** In PrioA extrahiert (siehe `tasks/audit-2026-07-24-PrioA/Konzept.md`)

### Problem

Vier Core-Services haben **keine** `ILogger<T>`-Injection:
- `ImportService` (Zeile 9: nimmt nur `Func<...>` und `int maxContentLengthWarning`)
- `ExportService` (Zeile 8: nur `Func<...>`)
- `SqlDocumentsStore` (Zeile 11: nur `string` × 2)
- `DocsValidator` (Zeile 8: nur `int`)

**Konsequenzen:**
- `SqlDocumentsStore.ReplaceAllAsync` weiß nicht, in welcher Bibliothek es gerade läuft
- `DocsValidator.Validate` kann nicht loggen
- `ImportService.ImportAsync` kann nicht loggen
- Bei Fehlern in Core: nur der Top-Level-`catch` loggt

### Fix-Empfehlung

Siehe PrioA-Konzept: `Microsoft.Extensions.Logging.Abstractions` zu Core.csproj, `ILogger<T>` per Constructor injizieren.

### Aufwand

- ~1,5 h (in PrioA)

### Risiko

Niedrig. Additiv.

---

## F-AR-005 — Keine zentrale `Constants`-Datei

> **Schweregrad:** Medium · **Dimension:** Architektur
> **Datei:** `KnowHowToAI.Core/Constants.cs` (neu, sobald 2. Fall)

### Problem

Die `.mdc`-Regel `06-configuration.mdc` Zeile 17 sagt:
> "Sobald ein zweiter Fall zu `FrontMatterParser.delimiter` hinzukommt, wird sie unter `KnowHowToAI.Core/Constants.cs` (oder passender benannt) angelegt."

Aktuell: nur 1 Fall (`FrontMatterParser.delimiter` in Zeile 59).

**Aktuelle "nahezu"-Konstanten, die in `Constants.cs` gehören würden:**
- `"---"` (YAML-Delimiter) — 1 Stelle im `FrontMatterParser`
- `"%.md"` / `"%.markdown"` (Markdown-Extension-Check) — 1 Validator-Stelle
- `"file://"` (Schema-Präfix) — 1 Validator-Stelle
- `"%COMPUTERNAME%"` (Env-Var-Literal) — 1 Stelle im Loader

**Empfehlung:** Beobachten und bei 2. Fall handeln. Aktuell nicht zwingend.

### Aufwand

- 0 jetzt
- ~20 Min sobald der 2. Fall eintritt

### Risiko

Keine.

---

## F-AR-006 — `Microsoft.Extensions.Logging` nicht in Core

> **Schweregrad:** Low · **Dimension:** Architektur
> **Status:** Folge von F-AR-002

### Beobachtung

Core referenziert `Microsoft.Extensions.Logging.Abstractions` *nicht* explizit, obwohl die Cli-Schicht `Microsoft.Extensions.Logging` (transient) referenziert. F-AR-002 löst das mit.

### Fix

In PrioA mit F-AR-002.

### Aufwand

- 0 (Teil von F-AR-002)

### Risiko

Keine.

---

## F-AR-008 / F-AR-009 / F-AR-010 — Info-Findings

> **Schweregrad:** Info · **Dimension:** Architektur

### F-AR-008: `sealed` als Class-Lock konsistent angewendet

Konsistenter Cross-Cutting-Stil. Kein Handlungsbedarf.

### F-AR-009: `partial class DocsValidator` mit `GeneratedRegex`

Idiomatisch, AiNetLinter-konform. Kein Handlungsbedarf.

### F-AR-010: Error-Handling ist konsistent

Alle vier CLI-Commands fangen `Exception` an der Top-Level und liefern Exit-Code 2. Kein Handlungsbedarf.

---

## Warum diese 6 und nicht andere?

### Aufgenommen

Alle verbleibenden Dim 3 Findings.

### Bewusst weggelassen

Keine. Dim 3 ist nach PrioJ vollständig aus PrioC/extrahiert.

Alle übrigen Findings (33) gehören thematisch in andere Brocken (K: Dependencies-Rest, L: Sicherheits-Rest, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

Keine Code-Änderungen. Brocken J schließt Dim 3 ab.

**Commit:** 1 Doku-/Cleanup-Commit zur Schließung von Dim 3.

## Querverweise zu anderen Brocken

- **F-AR-002 in PrioA** — bereits extrahiert.
- **F-AR-001, F-AR-003, F-AR-004, F-AR-007 in PrioC oder obsolet** — alle bereits extrahiert.

## Nächster Schritt

Nach PrioJ-Umsetzung: Dim 3 ist sauber. Weiter mit Brocken K (Dependencies-Rest).

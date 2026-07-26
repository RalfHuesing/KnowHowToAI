# Audit Prio I — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** PrioA (umgesetzt), PrioB-H (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (44 Findings nach Prio A-H) wurden die 4 Findings extrahiert, die unter „Doku-Rest (Dim 5)" zusammengefasst sind. Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | Status |
|---|---|---|---|---|
| [F-DK-001](#f-dk-001--logresponsesize-doku-dokumentiert-suboptimales-verhalten-als-soll) | `LogResponseSize`-Doku dokumentiert suboptimales Verhalten als Soll | High (obsolet) | obsolet | **erledigt** |
| [F-DK-005](#f-dk-005--preview-dependencies-undokumentiert) | Preview-Dependencies undokumentiert | Medium | ~5 Min | **obsolet** nach F-DP-001 (Downgrade in Commit 8fed418) |
| [F-DK-006](#f-dk-006--trustservercertificatetrue-undokumentiert) | `TrustServerCertificate=True` undokumentiert | Low | ~3 Min | **erledigt** (in F-DP-002-Commit mit-erledigt) |
| [F-DK-007](#f-dk-007--microsoftdatasqlclient-70-breaking-changes-infolow) | `Microsoft.Data.SqlClient 7.0` Breaking Changes (Info/Low) | Low | ~3 Min Querverweis | **erledigt** (in F-DP-002-Commit mit-erledigt) |

**Gesamt-Aufwand:** ~10 Min Doku-Updates. Aufteilbar in 1-2 Commits.

**Leitidee:** Letzte Doku-Lücken schließen, dann ist Dim 5 sauber.

---

## F-DK-001 — `LogResponseSize`-Doku dokumentiert suboptimales Verhalten als Soll

> **Schweregrad:** High (obsolet) · **Dimension:** Doku
> **Datei:** `docs/02-Architektur-und-Techstack.md:120` + `McpTools/DocsMcpTools.cs:43-44`

### Problem

`docs/02` Zeile 120 dokumentiert die Implementation *als* Begründung. Die Begründung ("damit der Log nicht zum Datenberg wird") ist richtig — aber die *konkrete* Implementation (`SerializeToUtf8Bytes(...).Length`) ist suboptimal: sie serialisiert die *gesamte* Response zu Bytes nur um die Länge zu messen. Das ist Performance-Müll (siehe F-PE-001 in Prio A).

**Status:** Per Audit obsolet nach F-PE-001 ✅ (Commit `d262095`). Die Doku muss aktualisiert werden, sobald F-PE-001 umgesetzt ist — was bereits passiert ist.

### Fix-Empfehlung

Doku in `docs/02` Zeile 120 aktualisieren:
> "nach der Abfrage die Größe der Antwort in Items (Listen) oder Content-Länge (DocumentDetail) — **nicht** deren Inhalt, da der Log sonst selbst zum riesigen, unübersichtlichen Datenberg würde. Konkret: `ResponseSize.Measure(result)` (`Core/Logging/ResponseSize.cs`)."

### Aufwand

- obsolet — kann im Doku-Commit zusammen mit anderen Updates erledigt werden

### Risiko

Keine. Reine Doku.

---

## F-DK-005 — Preview-Dependencies undokumentiert

> **Schweregrad:** Medium · **Dimension:** Doku
> **Datei:** `docs/02-Architektur-und-Techstack.md` (Tech-Stack-Tabelle)

### Problem

`src/KnowHowToAI.Cli/KnowHowToAI.Cli.csproj:16, 19`:
- `<PackageReference Include="ModelContextProtocol" Version="2.0.0-preview.2" />`
- `<PackageReference Include="System.CommandLine" Version="3.0.0-preview.5.26302.115" />`

Beide sind Preview. Nirgends in `docs/` ist erwähnt, *warum* Preview verwendet wird und was die Rollback-Strategie ist.

### Fix-Empfehlung

Kurzer Abschnitt in `docs/02` (Tech-Stack-Tabelle) oder am Anfang von `docs/03`:
> "Preview-Dependencies: `ModelContextProtocol 2.0.0-preview.2` und `System.CommandLine 3.0.0-preview.5` — bewusst gewählt wegen [Begründung]. Stable-Downgrade-Plan: bei nächstem 1.x-Release evaluieren."

**Hinweis:** Wenn F-DP-001 in PrioC umgesetzt wird (Downgrade), wird dieser Doku-Hinweis obsolet.

### Aufwand

- ~5 Min
- 1 Doku-Commit

### Risiko

Keine.

---

## F-DK-006 — `TrustServerCertificate=True` undokumentiert

> **Schweregrad:** Low · **Dimension:** Doku
> **Datei:** `docs/03-Projektstruktur-und-Konfiguration.md` Abschnitt 2

### Problem

`appsettings.json:4` enthält `TrustServerCertificate=True;` ohne Erklärung in `docs/03`.

**Kontext:** In SQL-Server-Setups mit selbst-signierten Zertifikaten (typisch für lokale Instanzen) muss `TrustServerCertificate=True` gesetzt werden, sonst schlägt die Verbindung fehl. Für User, die eine produktive SQL-Instanz mit echten Zertifikaten anbinden, ist das ein "warum ist das an?"-Fragezeichen.

### Fix-Empfehlung

In `docs/03` Abschnitt 2 (appsettings.json-Beispiel):
> "TrustServerCertificate=True ist auf lokalen Dev-Instanzen mit selbst-signierten Zertifikaten erforderlich. Für produktive Setups mit echten Zertifikaten sollte dieser Wert auf `False` stehen oder die Zeile komplett entfernt werden."

### Aufwand

- ~3 Min
- 1 Doku-Commit (kann mit F-DK-005 kombiniert werden)

### Risiko

Keine.

---

## F-DK-007 — `Microsoft.Data.SqlClient 7.0` Breaking Changes (Info/Low)

> **Schweregrad:** Low · **Dimension:** Doku
> **Datei:** `docs/03-Projektstruktur-und-Konfiguration.md` Abschnitt 2

### Problem

Querverweis zu Dim 7. 7.0 hat SqlBulkCopy-Breaking-Change für SQL Server 2016. Wenn das Repo auf eine solche Instanz zielt (unwahrscheinlich), ist Fehlersuche schwer ohne Doku-Hinweis.

### Fix-Empfehlung

Doku in `docs/03` Abschnitt 2:
> "Microsoft.Data.SqlClient 7.0+ ist gepinnt; siehe Release-Notes für Breaking Changes. Bei SQL-Server-Versionen ≤ 2016 ist 6.x zu verwenden (kein automatisches Downgrade). Pinning auf 7.0.2 (nicht 7.0.0) ist korrekt, weil 7.0.1 den SqlBulkCopy-Fix bringt."

**Hinweis:** Wenn F-DP-002 in PrioC umgesetzt wird, wird dieser Doku-Hinweis obsolet.

### Aufwand

- ~3 Min Doku (oder Querverweis)
- 1 Doku-Commit

### Risiko

Keine.

---

## Warum diese 4 und nicht andere?

### Aufgenommen

1. **F-DK-001** — Obsolet, Doku muss aktualisiert werden
2. **F-DK-005** — Preview-Deps sind real, brauchen Doku-Hinweis
3. **F-DK-006** — Config-Detail, braucht Doku
4. **F-DK-007** — Breaking-Change, braucht Doku

### Bewusst weggelassen (Kurzbegründung)

- **F-DK-008 (authoring-guide Slug-Regeln):** Per Audit "Kein Handlungsbedarf".
- **F-DK-009/010/011/012 (Positive Befunde Info):** Kein Handlungsbedarf.

Alle übrigen Findings (40) gehören thematisch in andere Brocken (J: Architektur-Rest, K: Dependencies-Rest, L: Sicherheits-Rest, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-DK-001** + **F-DK-005** + **F-DK-006** + **F-DK-007** — alle in einem Doku-Commit
2. Anschließend: Dim 5 ist sauber, nur F-DK-008 (Info) + F-DK-009/010/011/012 (Info) übrig

**Gesamt-Aufwand in dieser Reihenfolge:** ~10 Min, 1 Doku-Commit.

**Commit-Vorschlag:**
- Commit 1: F-DK-001 + F-DK-005 + F-DK-006 + F-DK-007 (Doku-Polish)

## Querverweise zu anderen Brocken

- **F-PE-001 in PrioA** — `LogResponseSize` umgesetzt; F-DK-001 obsolet.
- **F-DP-001 in PrioC** — Preview-Dependencies-Downgrade; F-DK-005 wird obsolet.
- **F-DP-002 in PrioC** — SqlClient-Breaking-Changes-Doku; F-DK-007 wird obsolet.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).

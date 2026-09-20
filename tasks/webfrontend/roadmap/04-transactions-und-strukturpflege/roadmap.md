# M4 – Transactions und Strukturpflege

[Roadmap-Index](../../Roadmap.md)

- [ ] **M4 abschließen**

Abhängigkeit: [M3](../03-read-only-wissenscockpit/roadmap.md)

Verbindliche M0-Basis: Strukturpflege erweitert denselben nativen, 100er-cursorpaginierten Knowledge Tree aus M3. Es wird keine Tree-Komponente gesucht oder ersetzt. `Parent`, `Before` und `After` sind die einzigen Move-Zielpositionen und verwenden den gemeinsamen Mutationseinstieg. Gemäß der späteren M4.3-T5-Entscheidung erfolgt die Strukturverschiebung bewusst per mausbasierter Drag-and-drop-Interaktion ohne Verschiebe- oder Zielpositionsbuttons. Komponenten- und Browsernachweise verwenden bUnit mit xUnit v3 beziehungsweise Microsoft.Playwright .NET mit der installierten aktuellen Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Benutzer können Working Transactions sicher führen und die Node-Struktur visuell ändern.

Referenzen: [Transaction-Arbeitsbereich](../../konzept/02-bedienkonzept-und-ui.md#transaction-arbeitsbereich), [Wissensbaum](../../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [DI-Grenzen](../../konzept/05-architektur-api-und-mcp.md#di--und-zustandsgrenzen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M4.0 – Manuelle Planung und Konzeptschärfung

- [x] **M4.0 abschließen**
  - Durchgeführt am 2026-09-19 gemeinsam mit dem Benutzer (zusammen mit M3.0).
  - Entschieden: O-007 (Actor per `ICurrentUserService`-Seam, Dummy-Implementierung jetzt; keine Änderung an Transaction-Komponenten nötig wenn Auth kommt), O-025 (mehrere gleichzeitige Clients erlaubt, keine Locks, stale `ChangeVersion` deterministisch ablehnen), O-026 (keine automatische Transaction-Lebensdauer, Warnbadge ab 7 Tagen), O-027 (Undo nur im Editor bis Speichern; kein globaler Undo-Stack).
  - Konzepte aktualisiert: [Bedienkonzept und UI](../../konzept/02-bedienkonzept-und-ui.md), [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md), [Offene Fragen](../../konzept/07-entscheidungen-und-offene-fragen.md).
  - Gate: M4.1 und folgende Arbeitspakete sind durch Implementierungsagenten ausführbar.

## M4.1 – Transaction-Arbeitskontext

- [x] **M4.1 abschließen**

  - [x] **M4.1-T1 – [Transaction beginnen, auflisten und fortsetzen](tasks/M4.1-T1.md)**
  - [x] **M4.1-T2 – [Transaction-State und Navigationsschutz implementieren](tasks/M4.1-T2.md)**
## M4.2 – Validierung und Abschluss

- [ ] **M4.2 abschließen**

  - [x] **M4.2-T1 – [Transaction validieren und Findings darstellen](tasks/M4.2-T1.md)**
  - [x] **M4.2-T2 – [Transaction-Diff vor Commit darstellen](tasks/M4.2-T2.md)**
  - [x] **M4.2-T3 – [Commit und Discard implementieren](tasks/M4.2-T3.md)**
  - [x] **M4.2-T4 – [Release aus committed Snapshot anlegen](tasks/M4.2-T4.md)**
## M4.3 – Node-Pflege

- [ ] **M4.3 abschließen**

  - [x] **M4.3-T1 – [Nodes erstellen und Stammdaten bearbeiten](tasks/M4.3-T1.md)**
  - [x] **M4.3-T2 – [Nodes kontrolliert löschen](tasks/M4.3-T2.md)**
  - [x] **M4.3-T3 – [Nodes per Drag-and-drop verschieben und sortieren](tasks/M4.3-T3.md)**
  - [x] **M4.3-T4 – [Initialen Root-Node im leeren Baum erstellen](tasks/M4.3-T4.md)**
  - [x] **M4.3-T5 – [Intuitives Drag-and-drop im Wissensbaum und Bereinigung der Verschiebe-Buttons](tasks/M4.3-T5.md)**
## M4.4 – Konflikte

- [ ] **M4.4 abschließen**

  - [x] **M4.4-T1 – [`SnapshotConflict` verständlich behandeln](tasks/M4.4-T1.md)**
## M4.5 – Manueller Milestone-Audit

- [x] **M4.5 – M4-Abschlussaudit manuell mit dem Benutzer durchführen**
  - Durchführung: am 2026-09-20 gegen den vollständigen M4-Ist-Stand; dieser Punkt ist kein delegierbarer Implementierungs-Leaf-Task.
  - Geprüft: Transaction-Lebenszyklus, Strukturpflege, Konfliktbehandlung, Abnahmebelege und synchronisierte Ist-Dokumentation gegen M4-Ziele und Invarianten.
  - Ergebnis: am 2026-09-20 gegen `c12f792` durchgeführt; Urteil „Nacharbeit erforderlich“, Details und verworfene Hinweise siehe [Audit](audit.md).

## M4.6 – Audit-Nacharbeiten

- [ ] **M4.6 abschließen**

  - [x] **M4.6-T1 – [Exakte Before-/After-Positionierung sicherstellen](tasks/M4.6-T1.md)**
  - [x] **M4.6-T2 – [Dirty-State der M4-Strukturformulare anbinden](tasks/M4.6-T2.md)**
  - [x] **M4.6-T3 – [MCP-Strukturmutationen gegen stale Writes absichern](tasks/M4.6-T3.md)**
  - [x] **M4.6-T4 – [Validierungsbefunde an die gelesene ChangeVersion binden](tasks/M4.6-T4.md)**
  - [x] **M4.6-T5 – [SnapshotConflict-Reapply mit echten Änderungen nachweisen](tasks/M4.6-T5.md)**
## Milestone-Abnahme

- Transaction-Lebenszyklus und vollständige Node-Strukturpflege funktionieren ohne Agent.
- Jede Mutation verwendet eine explizite Transaction und erscheint in Validierung sowie Diff.
- Navigation, Reconnect, Neustart und parallele MCP-Änderungen sind sicher behandelt.

# KnowHowTo AI – Implementierungs-Roadmap

Verbindlicher Projektfortschritt und Status-Tracking für Agenten und Entwickler.  
Jeder Meilenstein und jede Teilaufgabe wird bei Abschluss sofort von `[ ]` auf `[x]` gesetzt.

---

## M0: Solution-Setup & Testinfrastruktur
- [ ] **M0.1: Solution- & Projektstruktur anlegen**
  - [ ] .NET Solution anlegen (`KnowHowToAI.sln` bzw. `KnowHowToAI.slnx`)
  - [ ] Projekt `src/KnowHowToAI.Core` (Domänenmodelle, Invarianten, Interfaces, Schichtenentkopplung)
  - [ ] Projekt `src/KnowHowToAI.Storage.SqlServer` (ADO.NET / Dapper, DDL-Runner, Snapshot-Engine)
  - [ ] Projekt `src/KnowHowToAI.Server` (MCP STDIO Adapter, Tool-Hosting, CLI-Einstiegspunkt)
  - [ ] Testprojekt `tests/KnowHowToAI.Core.Tests` (FastTests: Unit-Tests für Fachlogik)
  - [ ] Testprojekt `tests/KnowHowToAI.IntegrationTests` (Integrationstests für SQL Server und MCP)
- [ ] **M0.2: Zentrale Build- & Codeanalyse-Konfiguration**
  - [ ] `Directory.Build.props` mit `TreatWarningsAsErrors=true`, `#nullable enable`, C# 12+ einrichten
  - [ ] `.editorconfig` für Coding-Standards (`sealed` by default, Records, Formatierung) definieren
  - [ ] AiNetLinter-Integration verifizieren (Solution-Target für Linter-MCP-Tools)
- [ ] **M0.3: Entwickler- & Testskripte**
  - [ ] `scripts/test-fast.ps1` für deterministische Unit-Tests (`Category=Unit`) bereitstellen
  - [ ] `scripts/test-integration.ps1` für Datenbank- und E2E-Tests bereitstellen
  - [ ] Initialer Build- und FastTest-Durchlauf fehlerfrei (`Incremental Gate` erfüllt)

---

## M1: Relationales Datenmodell & SQL-Schema
- [ ] **M1.1: Tabellendefinitionen (Präfix `KnowHowToAI_`)**
  - [ ] `sql-scripts/0001_create_system_and_snapshots.sql`:
    - [ ] `KnowHowToAI_SystemState` (CurrentSnapshotId, LastUpdatedUtc)
    - [ ] `KnowHowToAI_Snapshot` (SnapshotId, BaseSnapshotId, State, CreatedAtUtc, CommittedAtUtc)
    - [ ] `KnowHowToAI_Transaction` (TransactionId, BaseSnapshotId, WorkingSnapshotId, State, CreatedAtUtc, CommittedAtUtc, Metadata)
    - [ ] `KnowHowToAI_Release` (ReleaseId, SnapshotId, Name, ReleasedAtUtc)
  - [ ] `sql-scripts/0002_create_hierarchy_and_content.sql`:
    - [ ] `KnowHowToAI_Node` (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
    - [ ] `KnowHowToAI_NodeContent` (SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
  - [ ] `sql-scripts/0003_create_roles_and_dependencies.sql`:
    - [ ] `KnowHowToAI_Role` (RoleId, Name, Description)
    - [ ] `KnowHowToAI_RoleResolution` (RequestedRoleId, CandidateRoleId, Priority)
    - [ ] `KnowHowToAI_ContentDependency` (SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId)
- [ ] **M1.2: Schema-Migration & DDL-Runner**
  - [ ] `ISchemaMigrator` / Migrations-Runner im Storage-Projekt implementieren
  - [ ] Idempotente Skript-Ausführung und Versionskontrolle für Datenbank-Initialisierung
- [ ] **M1.3: Seed-Daten für Initialzustand**
  - [ ] Standard-Rolle `Default` und initiale Role Resolution Order definieren
  - [ ] Initialen leeren Committed Snapshot (ID 1) und `SystemState` anlegen
- [ ] **M1.4: Tests für M1**
  - [ ] Integrationstest für Schema-Erstellung auf frischer Datenbank
  - [ ] Integrationstest für Idempotenz der Migrationen

---

## M2: Transaktions- & Snapshot-Engine
- [ ] **M2.1: Transaktionseröffnung (`begin_transaction`)**
  - [ ] Neuen Working Snapshot erzeugen (`State = Working`, `BaseSnapshotId = CurrentSnapshotId`)
  - [ ] Vollständige Snapshot-Kopie via atomarem SQL `INSERT ... SELECT` (Nodes, NodeContents, Dependencies)
  - [ ] Transaktionsdatensatz persistieren (`State = Open`)
- [ ] **M2.2: Transaktionsabschluss (`commit_transaction`)**
  - [ ] Kurze, atomare SQL-Transaktion für den Commit-Vorgang
  - [ ] Concurrency-Check: Prüfen ob `BaseSnapshotId == CurrentSnapshotId`
  - [ ] Fehler `SnapshotConflict` werfen, falls konkurrierender Commit dazwischenkam
  - [ ] Snapshot-Status auf `Committed` setzen, `SystemState.CurrentSnapshotId` atomar umschalten
  - [ ] Transaktions-Status auf `Committed` setzen
- [ ] **M2.3: Transaktionsabbruch (`discard_transaction`)**
  - [ ] Snapshot-Status auf `Discarded` setzen
  - [ ] Transaktions-Status auf `Discarded` setzen
  - [ ] `SystemState` bleibt unverändert; keine Rückabwicklung von Working-Daten nötig
- [ ] **M2.4: Snapshot-Unveränderlichkeit & Soft-Delete**
  - [ ] Schreibschutz-Validierung: Keine Schreiboperationen auf bereits committed Snapshots
  - [ ] Soft-Delete-Semantik in Datenzugriffsschicht absichern (`IsDeleted = true`)
- [ ] **M2.5: Tests für M2**
  - [ ] Integrationstest: Transaktionszyklus (Begin -> Write -> Commit -> Verify Current)
  - [ ] Integrationstest: Discard-Zyklus (Begin -> Write -> Discard -> Verify Current unchanged)
  - [ ] Integrationstest: Parallele Transaktionen mit deterministischem `SnapshotConflict`

---

## M3: Domänenlogik, Validatoren & Invarianten
- [ ] **M3.1: Markdown-Heading-Validator**
  - [ ] Echten Markdown-Parser (Markdig) einbinden
  - [ ] Harte Invariante: Striktes Verbot von Markdown-Headings (`#`) im gespeicherten Content
  - [ ] Striktes Verbot von HTML-Headings (`<h1>` - `<h6>`)
  - [ ] Fenced Code Blocks mit `#` (z. B. C# Preprocessor `#if`, Kommentare) zulassen
- [ ] **M3.2: Node- & Content-Trennung**
  - [ ] Node-Titel und Metadaten ausschließlich in der Node-Hierarchie führen
  - [ ] Prüfung: Node-Titel darf nicht im Content repliziert werden
- [ ] **M3.3: Rollenauflösung (Role Resolution)**
  - [ ] Deterministische Resolution Order für angefragte Rolle laden
  - [ ] Fallback-Logik ausführen, falls kein expliziter Rollen-Content existiert
  - [ ] Transparentes Ergebnis: `requestedRole`, `resolvedRole`, `fallbackUsed`
- [ ] **M3.4: Content-Revisionen, Provenienz & Stale-Erkennung**
  - [ ] `ContentRevisionId`: Erzeugung neuer Revisions-ID bei echtem Inhalts-Update
  - [ ] `ContentMode`: Unterscheidung zwischen `Independent` und `Derived`
  - [ ] Abhängigkeiten speichern: `Target` verweist auf `Source` mit `SourceContentRevisionId`
  - [ ] Stale-Prüfung: Erkennen, wenn `SourceContentRevisionId` nicht mehr der aktuellen Quell-Revision entspricht
- [ ] **M3.5: Text-Patching (`replace_text`)**
  - [ ] Exakte Match-Prüfung im vorhandenen Text
  - [ ] Invariante: Exakt 1 Match erforderlich; Fehler bei 0 Matches (`TextNotFound`) oder >1 Matches (`MultipleTextMatches`)
- [ ] **M3.6: Hierarchie-Operationen**
  - [ ] Node anlegen (`create_node`), verschieben (`move_node`), umbenennen, umsortieren
  - [ ] Zyklus-Erkennung in der Eltern-Kind-Beziehung
  - [ ] `delete_node` (global, Fehler bei vorhandenen Children ohne Subtree-Flag) vs. `delete_content` (nur Rolle)
- [ ] **M3.7: Markdown-Export-Engine (`export_tree`)**
  - [ ] Export-Root wird Heading Level 1 (`# Title`)
  - [ ] Kind-Nodes erhalten relative Überschriftenebenen (`##`, `###`)
  - [ ] Rollenauflösung pro exportiertem Node berücksichtigen
- [ ] **M3.8: Tests für M3**
  - [ ] FastTests: Heading-Validator (Positiv-/Negativtests, Fenced Code, Edge Cases)
  - [ ] FastTests: Rollenauflösung und Fallback
  - [ ] FastTests: Stale-Erkennung und Revisions-Vergleiche
  - [ ] FastTests: `replace_text`-Fehlerfälle (0 Matches, Multi-Match, Single-Match)
  - [ ] FastTests: Hierarchie-Validierung und Zyklus-Prüfung
  - [ ] FastTests: Markdown-Export-Assembler

---

## M4: MCP-Server & STDIO-Tools
- [ ] **M4.1: Application Services Layer**
  - [ ] `IKnowledgeTransactionService` (Transaktionslebenszyklus)
  - [ ] `IKnowledgeNavigationService` (Lesen, Hierarchie, Auflösung)
  - [ ] `IKnowledgeMutationService` (Schreiben von Nodes und Content)
  - [ ] `IKnowledgeExportService` (Markdown-Export)
  - [ ] `Result<T>`-Rückgaben für alle Service-Methoden
- [ ] **M4.2: MCP STDIO Server Setup**
  - [ ] MCP-Hosting über STDIO konfigurieren
  - [ ] Dependency Injection / Service-Verdrahtung ohne Transport-Kopplung
- [ ] **M4.3: Tool-Implementierung**
  - [ ] **Transaktions-Tools**: `begin_transaction`, `commit_transaction`, `discard_transaction`, `get_transaction`
  - [ ] **Navigations-Tools**: `get_root`, `get_node`, `list_children`, `list_roles`
  - [ ] **Struktur-Tools**: `create_node`, `update_node`, `move_node`, `reorder_node`, `delete_node`
  - [ ] **Content-Tools**: `replace_content`, `replace_text`, `delete_content`
  - [ ] **Validierungs-Tools**: `validate_transaction`
  - [ ] **Export-Tools**: `export_tree`
- [ ] **M4.4: Fehlerbehandlung & Symmetrie**
  - [ ] Standardisierte maschinenlesbare Fehlercodes (`TransactionNotFound`, `SnapshotConflict`, etc.)
  - [ ] Tool-Outputs und Folge-Inputs symmetrisch halten (Zero-Transformation)
- [ ] **M4.5: Tests für M4**
  - [ ] Integrationstests: End-to-End-Tool-Aufrufe über MCP-Protokoll
  - [ ] Vertragstests für alle MCP-Tool-Schemas

---

## M5: Retrieval-Optimierung, Search & End-to-End
- [ ] **M5.1: Token-effiziente Navigation (Progressive Disclosure)**
  - [ ] `list_children` optimieren: Reiner Metadaten-Return (Titel, Description, ChildCount, Freshness, ContentSize)
- [ ] **M5.2: Textbasierte Suche (`search`)**
  - [ ] Parametrisierte Suche über Titel, Description und Markdown-Content
  - [ ] Treffer als schlanke Kontext-Snippets zurückgeben
- [ ] **M5.3: Qualitätswarnungen & Refactoring-Kandidaten**
  - [ ] Soft-Limits: Erkennung zu großer Nodes (`NodeTooLarge`), Warnungen statt Speicherverbot
  - [ ] Warnung bei Stale Derived Content in Transaktionen
- [ ] **M5.4: End-to-End-Workflow-Verifikation**
  - [ ] Vollständiger Lebenszyklus: Consultant-Erfassung -> Developer-Update -> Stale-Erkennung -> EndUser-Doku-Sync
  - [ ] Verifikation aller Quality Gates (`verify` Score 10.0, Solution fehler- und warnungsfrei)

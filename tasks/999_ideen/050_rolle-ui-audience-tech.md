# Konzept & Refactoring-Plan: Rolle (UI) / Audience (Tech)

**Status:** Beschlossene Sache (Entscheidung getroffen). Vollständiges technisches und fachliches Refactoring.  
**Ziel:** Konsequente und lückenlose Umbenennung des Konzepts „Rolle“ (`Role`) zu „Zielgruppe“ bzw. technisch **`Audience`** über alle Schichten hinweg (SQL, Core Domain, C# Services, MCP-API, Web-Frontend, Verzeichnisse, Namespaces, Tests und Dokumentation).  
**Kriterium nach Abschluss:** Eine Suche mit `ripgrep` (`rg -i "role"` und `rg -i "rolle"`) darf im gesamten technischen Repository-Code keine Treffer mehr liefern.

---

## 1. Ausgangslage & Begründung

### 1.1 Das Problem mit „Rolle“ / „Role“
Im Software-Engineering und in der IT-Sicherheit ist der Begriff **„Role“ (Rolle)** nahezu ausnahmslos mit **RBAC (Role-Based Access Control) / Berechtigungen / ACLs** verknüpft (*„Darf der Benutzer diesen Datensatz lesen oder schreiben?“*).

In KnowHowToAI beschreibt dieses Konzept jedoch **keine Berechtigung**, sondern die **inhaltliche Zielgruppe / Adressatenschaft** eines Wissensinhalts (*„Für wen ist dieser Inhalt verfasst?“*, z. B. Consultant, Developer, EndUser). Diese begriffliche Doppelbelegung führte wiederholt zu Missverständnissen und erforderte defensive Klarstellungen in der Dokumentation (z. B. in `docs/Rollen-und-Content.md`: *„Wissensrolle ist keine Berechtigungsrolle“*).

### 1.2 Die Entscheidung: `Audience` (Tech) & `Zielgruppe` / `Rolle` (UI)
- **Technischer Standardbegriff (Code, DB, API, Verträge):** **`Audience`**  
  `Audience` ist im Technical Writing, CMS-Design und Informationsmanagement der international etablierte Standardbegriff für genau dieses Modell (*Single-sourcing for multiple audiences*).
- **Benutzeroberfläche & Sprache:**  
  - *Option A (Empfohlen für striktes 0-Treffer-Kriterium):* Die deutsche Benutzeroberfläche verwendet durchgängig den Begriff **„Zielgruppe“** (z. B. „Zielgruppen verwalten“, „Zielgruppen-Auflösung“). Damit ist auch die Textsuche nach `rolle` im gesamten Repository bei 0 Treffern.  
  - *Option B:* Falls in der Benutzeroberfläche für menschliche Anwender der Begriff **„Rolle“** beibehalten wird, ist dies strikt auf sichtbare Anzeigetexte/Labels im deutschen Frontend (oder Lokalisierungsressourcen) begrenzt; jegliche technische Repräsentation (Routen `/audiences`, Komponenten, CSS-Klassen, Bindings) lautet auf `Audience`.

---

## 2. Vollständiger Scope der Umbenennung

Die Umbenennung betrifft ausnahmslos alle Schichten des Systems:

### 2.1 Datenbank & SQL Server
- **Tabellen:**
  - `Roles` → `Audiences`
  - `RoleResolutionOrders` → `AudienceResolutionOrders`
- **Fremdschlüssel & Spalten:**
  - `RoleId` → `AudienceId` (in `Audiences`, `AudienceResolutionOrders`, `Contents`, `ContentDependencies`, etc.)
  - `RequestedRoleId` → `RequestedAudienceId`
  - `ResolvedRoleId` → `ResolvedAudienceId`
  - `CandidateRoleId` → `CandidateAudienceId`
- **Constraints & Indizes:**
  - `PK_Roles` → `PK_Audiences`
  - `FK_RoleResolutionOrders_Roles` → `FK_AudienceResolutionOrders_Audiences`
  - `IX_RoleResolutionOrders_*` → `IX_AudienceResolutionOrders_*`
  - `CK_Role_*` → `CK_Audience_*`
- **Stored Procedures & Skripte:**
  - Alle Prozeduren von `usp_Role_*` auf `usp_Audience_*` umbenennen.
  - Seed-Skripte (`seed.sql`, Migrationen): Erzeugung der initialen `Default`-Audience.

### 2.2 Core Domain & Application Services (C#)
- **Models & Value Objects:**
  - `Role` / `RoleEntity` → `Audience` / `AudienceEntity`
  - `RoleId` → `AudienceId`
  - `RoleResolutionOrder` → `AudienceResolutionOrder`
  - `RoleResolution` → `AudienceResolution`
  - `RoleResolutionEntry` → `AudienceResolutionEntry`
- **Services & Interfaces:**
  - `IRoleService` → `IAudienceService`
  - `RoleService` → `AudienceService`
  - `IRoleMutationService` → `IAudienceMutationService`
  - `RoleMutationService` → `AudienceMutationService`
  - `RoleResolutionService` → `AudienceResolutionService`
- **Fehlercodes & Domänen-Events:**
  - `RoleNotFound` → `AudienceNotFound`
  - `RoleInUse` → `AudienceInUse`
  - `RoleNameRequired` → `AudienceNameRequired`
  - `InvalidRoleResolutionOrder` → `InvalidAudienceResolutionOrder`
- **Namespaces & Verzeichnisse:**
  - `KnowHowToAI.Core.Roles` → `KnowHowToAI.Core.Audiences`
  - Dateipfade unter `src/KnowHowToAI.Core/Audiences/`

### 2.3 MCP-Tools & Verträge
- **Tool-Namen:**
  - `get_roles` → `get_audiences`
  - `create_role` → `create_audience`
  - `rename_role` → `rename_audience`
  - `delete_role` → `delete_audience`
  - `set_role_resolution` → `set_audience_resolution`
  - `get_role_resolution` → `get_audience_resolution`
- **Parameter:**
  - `role` / `roleName` / `requestedRole` → `audience` / `audienceName` / `requestedAudience`
  - `roleId` → `audienceId`
- **DTOs & Envelopes:**
  - `RoleDto` → `AudienceDto`
  - `RoleResolutionDto` → `AudienceResolutionDto`
- **Fehlerbehandlung:**
  - JSON-RPC / MCP-Fehlercodes und Messages aktualisieren.

### 2.4 Web-Frontend (Blazor & HTTP-Endpunkte)
- **Routen:**
  - `/roles` → `/audiences`
- **Verzeichnisstruktur & Namespaces:**
  - `src/KnowHowToAI.Server/Web/Features/Roles/` → `src/KnowHowToAI.Server/Web/Features/Audiences/`
  - Namespace `KnowHowToAI.Server.Web.Features.Audiences`
- **Komponenten:**
  - `RolesPage.razor` / `.razor.cs` / `.razor.css` → `AudiencesPage.razor`
  - `RoleEditor.razor` / `.razor.cs` / `.razor.css` → `AudienceEditor.razor`
  - `RoleResolutionPanel.razor` → `AudienceResolutionPanel.razor`
- **State & ViewModels:**
  - State-Klassen, Navigation-Links und Dispatcher anpassen.

### 2.5 Tests & Fixtures
- **Testprojekte:**
  - `KnowHowToAI.Core.Tests`: `RoleServiceTests` → `AudienceServiceTests`, etc.
  - `KnowHowToAI.Mcp.Tests`: MCP-Tool-Tests für Audiences.
  - `KnowHowToAI.Sql.Tests`: Integrationstests für Audiences und Resolution Orders.
  - `KnowHowToAI.BrowserTests`: Playwright-Tests auf `/audiences` und neue Selektoren anpassen.
- **Fixtures & Builders:**
  - `RoleBuilder` → `AudienceBuilder`
  - Test-Daten von `Role` auf `Audience` umstellen.

### 2.6 Dokumentation
- `docs/Rollen-und-Content.md` → umbenennen in `docs/Zielgruppen-und-Content.md` (oder `docs/Audiences-und-Content.md`).
- Aktualisierung aller Verweise in:
  - `docs/README.md`
  - `docs/Architektur.md`
  - `docs/Datenmodell.md`
  - `docs/Invarianten.md`
  - `docs/Entscheidungen.md`
  - `docs/Retrieval.md`
  - `docs/Transaktionen-und-Historie.md`
  - `docs/Wissenshierarchie.md`
  - `.agents/rules/*`
  - Bestehende Task-Dateien in `tasks/`

---

## 3. Umsetzungsstrategie (Hard Cut)

Gemäß Grundprinzip des Projekts (*Greenfield / Hard Cut*) erfolgt keine Übergangs- oder Migrationsphase mit Legacy-Aliases oder Kompatibilitätsbrücken. Die Umstellung erfolgt als atomarer, systemweiter Refactoring-Schnitt.

### Phasenplan

- [ ] **Phase 1: Vorbereitung & Spezifikation**
  - [ ] Exakte Inventur aller Vorkommen (`rg -i "role"` und `rg -i "rolle"`).
  - [ ] Klärung UI-Wording (Zielgruppe vs. Rolle im deutschen Frontend).
  - [ ] Festlegung der genauen Commit- und Slice-Reihenfolge.

- [ ] **Phase 2: Datenbank & Datenzugriff**
  - [ ] Tabellen `Audiences` und `AudienceResolutionOrders` in SQL-Definitionen umbenennen.
  - [ ] Alle Stored Procedures, Fremdschlüssel und Indizes anpassen.
  - [ ] SQL-Repository-Implementierungen und Abfragen in C# anpassen.
  - [ ] SQL-Tests ausführen und verifizieren.

- [ ] **Phase 3: Core Domain & Services**
  - [ ] Entitäten, Records und DTOs umbenennen (`RoleId` → `AudienceId`, `Role` → `Audience`).
  - [ ] Services und Interfaces migrieren (`IAudienceService`, `IAudienceMutationService`).
  - [ ] Fehlercodes aktualisieren (`AudienceNotFound`, etc.).
  - [ ] Core-Unit-Tests anpassen und verifizieren.

- [ ] **Phase 4: MCP-Kante & Protokollverträge**
  - [ ] MCP-Tools umbenennen (`get_audiences`, `create_audience`, etc.).
  - [ ] Parameterverträge und Tool-Beschreibungen aktualisieren.
  - [ ] Integrationstests der MCP-Tools anpassen.

- [ ] **Phase 5: Web-Frontend & Navigation**
  - [ ] Verzeichnis `Web/Features/Roles` nach `Web/Features/Audiences` verschieben.
  - [ ] Razor-Komponenten und Code-Behinds umbenennen.
  - [ ] Routen `/roles` auf `/audiences` ändern.
  - [ ] Navigationselemente, Breadcrumbs und Labels aktualisieren.
  - [ ] BrowserTests anpassen.

- [ ] **Phase 6: Dokumentation & Richtlinien**
  - [ ] `docs/Rollen-und-Content.md` umbenennen und Inhalte anpassen.
  - [ ] Alle Architektur-, Datenmodell- und Regel-Dokumente aktualisieren.
  - [ ] Tasks und Roadmaps konsolidieren.

- [ ] **Phase 7: End-to-End-Validierung**
  - [ ] Vollständiger Build (`scripts/build.ps1`).
  - [ ] Gesamte Testsuite (FastTests, IntegrationTests, BrowserTests).
  - [ ] Linter-Validierung (`AiNetLinter`).
  - [ ] **Abschlussprüfung:** `rg -i "role"` liefert keine Treffer im Code.

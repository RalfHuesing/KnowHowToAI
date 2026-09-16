# M5.12 – Search-Abnahme: Abfrageplan und realistische SQL-Messung

Nachweis für `docs/Roadmap.md` M5.12: Die reale, parametrisierte Search-Query wurde
gegen eine repräsentative, ausschließlich über Working-Transaction/Commit-Pfade
erzeugte Datenmenge gemessen (tatsächlicher Ausführungsplan, logische Reads,
Laufzeit). Die Messwerte werden vom Integrationstest laufend neu nach
`temp/search-abnahme-messung.json` geschrieben (größenbezogen geglättet,
ungekürzte STATISTICS-Ausgaben enthalten).

## Testklassen

- `tests/KnowHowToAI.IntegrationTests/SqlServer/Abnahme/SqlSearchAbnahmeTests.cs` –
  Testgröße, Suchsemantik und Messung der Search-Query.
- `tests/KnowHowToAI.IntegrationTests/SqlServer/Abnahme/SqlDiffReleaseAbnahmeTests.cs` –
  Diff-Kategorien, historische Reproduktion, Release-Listing über mehrere Seiten;
  `SqlDiffReleaseAbnahmeTests.DiffPaging.cs` – seitenweises Diff-Blättern über die
  volle Datenmenge mit Pro-Seiten-Messung (M5.16).
- `tests/KnowHowToAI.IntegrationTests/TestSupport/WorkingTransactionSession.cs` und
  `SequentialIdentifierGenerator.cs` – gemeinsames Seeding ausschließlich über die
  produktiven Mutation-Pfade (`begin_transaction`, Node-/Content-/Role-Mutation,
  `commit_transaction`); committed Snapshots werden in Tests nie direkt per SQL
  verändert. IDs werden fortlaufend und damit vorab bekannt erzeugt, Zeitwerte sind
  fix vorgegeben (StaticClock); keine Wall-Clock- oder Zufallsabhängigkeit.

## Repräsentative Testgröße

| Größe | Wert |
| --- | --- |
| Nodes | 401 (1 Root + 400 Themen-Nodes, eine Hierarchieebene) |
| Rollen | 3 aktiv angelegte (`Entwickler`, `Endanwender`, `Berater`) plus geerbter Seed-`Default` |
| Role Resolutions | 2 Orders (`Entwickler←[Entwickler, Endanwender]`, `Berater←[Berater, Entwickler]`) |
| Contents | 440 (400 `Entwickler` Independent, 20 `Endanwender` Independent, 20 `Berater` Derived) |
| Dependencies | 20 (jeweils Derived-Content auf die eigene Entwickler-Quellrevision) |

Begründung: 400 Knoten sind genug, um Scan-/Join-Kosten sichtbar zu machen, ohne
dass das Seeding über die echten Mutation-Pfade (eine Roundtrip-Batch pro Mutation)
unangemessen lange dauert. Suchtreffer sind deterministisch gestreut:
`Installation` im Titel bei jedem 10. Node (40 Treffer), `Ablauf` in der
Description bei jedem 8. und im Content bei jedem 4. Node (Vereinigung = 100
Treffer, Description-Rang gewinnt).

## Messmethodik

Der Integrationstest führt den unveränderten SQL-Text von
`SqlRetrievalRepository.SearchSql` mit `SET STATISTICS IO`, `SET STATISTICS TIME`
und `SET STATISTICS PROFILE` aus (die Profile-Gridergebnis enthält den Ist-Plan;
`sys.dm_exec_query_stats` ist für die Testanmeldung nicht lesbar, weshalb der
Plan-Cache-Fallback bewusst entfällt). STATISTICS-Meldungen kommen als
InfoMessages an und werden zweisprachig (deutsche bzw. englische
SQL-Server-Spracheinstellung) geparst. Gemessen werden drei Varianten:

1. **OhneRolle** – `@roleId IS NULL`, Pattern `%Installation%`, `@limit = 50`.
2. **MitRolleErsteSeite** – `@roleId = N'Entwickler'`, Pattern `%Ablauf%`, `@limit = 50`.
3. **MitRolleFolgeseite** – wie 2. mit belegtem Keyset-Cursor (`@hasCursor = 1`,
   Cursor-Parameter aus dem letzten Treffer der ersten Seite).

Zusätzlich weist der Test die Suchsemantik über das echte Repository nach:
vollständige Paginierung der Treffermengen (40 bzw. 100 Treffer), korrekte
`HitField`-Ränge und `Availability.Explicit` für die angefragte Rolle.

## Messergebnis

Umgebung: SQL Server 2022 (`ProductMajorVersion` 16), lokale Instanz
`%COMPUTERNAME%\MSSQLSERVER2022`, Datenbank `KnowHowToAi`, Snapshot mit 401 Nodes
und 440 Contents.

| Variante | Treffer (Seite) | logische Reads gesamt | Verteilung | CPU / verstrichen (Ausführung) | Wall-Clock inkl. Kompilierung |
| --- | --- | --- | --- | --- | --- |
| OhneRolle | 40 | 17 | Node 15, Role 2 | 0 / 0 ms | 14,7 ms |
| MitRolleErsteSeite | 50 | 1795 | NodeContent 898, RoleResolution 880, Role 2, Node 15 | 0 / 0 ms | 12,2 ms |
| MitRolleFolgeseite | 50 | 1795 | identisch zur ersten Seite | 0 / 0 ms | 14,1 ms |

Ausführungsplan (Ist-Plan aus STATISTICS PROFILE):

- **OhneRolle:** Clustered Index Seek auf `PK_KnowHowToAI_Node` (Seek auf
  `SnapshotId`, Residual `IsDeleted = 0`), Nested Loops Left Outer Join mit Lazy
  Spool gegen `NodeContent`, Keyset-Prädikat im Filter. Kein Scan.
- **MitRolle:** Index Seek auf `IX_KnowHowToAI_NodeContent_Role`, Key-Lookups auf
  `PK_KnowHowToAI_NodeContent`, Index Seek auf `UQ_KnowHowToAI_RoleResolution_Candidate`
  pro Kandidatenrolle, Hash Match (Left Outer Join) über `NodeId`, Sort/Top für
  `HitRank, SortOrder, NodeId`.
- Der planbestimmte Kostenanteil ist die Kompilierung (7–9 ms); die reine
  Ausführungszeit liegt bei 0 ms (< 1 ms Auflösung).

## Entscheidung: keine Search-Index-Migration

Ein belegter Engpass liegt bei dieser Datenmenge nicht vor: maximal 1795 logische
Reads und ~14 ms Wall-Clock pro Suchseite, Ausführung selbst < 1 ms. Der Plan
arbeitet bereits mit Seek-Zugriffen; die Reads werden vom Nested-Loop-/Lookup-Zugriff
auf `NodeContent` und `RoleResolution` bestimmt und skalieren mit der Treffermenge
pro Rolle, nicht mit dem Gesamtbestand. Ein `LIKE '%…%'`-Suchmuster ist von Natur aus
nicht sargbar; ein zusätzlicher Suchindex könnte die Head-Lookups nur marginal
verbessern, während die Greenfield-Baseline unangetastet bleibt. Eine additive
Migration mit Search-Indizes wird daher **nicht** angelegt; Vorher-Messwerte sind
die obigen Werte, ein Nachher entfällt entsprechend. Bei deutlich größerer
Datenmenge (z. B. Faktor 100) sollte die Messung mit derselben Testklasse wiederholt
werden; sie gibt die Messwerte pro Lauf automatisch neu aus.

## M5.15 – Freshness-Ladekosten der Search

Nachweis für `docs/Roadmap.md` M5.15: eine zusätzliche Messvariante in
`SqlSearchAbnahmeTests` (Datei `SqlSearchAbnahmeTests.Freshness.cs`, Test
`SearchMitDerivedTreffern_KompletteFreshnessLadeKosten_NachweisInReadsUndLaufzeit`,
Kategorie `ManualDatabaseIntegration`) erfasst die **kompletten Kosten von
`SqlRetrievalRepository.SearchAsync` bei Derived-Content-Treffern** – also alle
fünf Statements, die der Repository-Pfad auf einer SQL-Verbindung ausführt:

1. `ListRolesSql` + `ListRoleResolutionsSql` (Rollen-/Resolution-Stand),
2. `SearchSql` (parametrisierte Such-Query),
3. `ListContentsSql` + `ListDependenciesSql` (volles Laden aller Contents und
   Dependencies, sobald die Trefferseite mindestens einen Derived-Content enthält;
   Grundlage für die transitive Freshness-Bewertung via `FreshnessEvaluator`).

Gemessen wird derselbe M5.12-Datensatz (401 Nodes, 440 Contents, 20 Dependencies);
Suchtext `Berater-Sicht` mit Rolle `Berater` liefert 20 Derived-Content-Treffer
(`HitField = Content`, `Availability = Explicit`, `Freshness = Current`). Der Test
weist außerdem nach, dass das Repository tatsächlich die kompletten Bestände lädt
(440 Contents, 20 Dependencies) und berechnet die Zeilenzahl der Freshness-Teilmenge
(20 Dependencies der Treffer + 20 unmittelbare Source-Contents = 40 Zeilen).
STATISTICS-Ausgaben werden wie in M5.12 zweisprachig geparst; die Messwerte werden
laufend neu nach `temp/search-abnahme-freshness-messung.json` geschrieben
(Umgebung: SQL Server 2022, Datenbank `KnowHowToAi`).

### Messergebnis (Vorher: volles Laden, Stand M5.15)

| Messgröße | Wert |
| --- | --- |
| Trefferseite | 20 Derived-Content-Treffer (eine Seite, `@limit = 25`) |
| Geladene Contents / Dependencies | 440 / 20 (vollständig) |
| Freshness-Teilmenge der Trefferseite | 40 Zeilen (20 Dependencies + 20 Source-Contents) |
| logische Reads gesamt (alle 5 Statements) | 1819 |
| Verteilung | NodeContent 916, RoleResolution 882, Node 15, Role 4, ContentDependency 2, Worktable/Workfile 0 |
| CPU / verstrichen pro Statement | 0 / 0 ms (< 1 ms Auflösung) |
| Wall-Clock SQL-Batch inkl. Kompilierung | 16,0 ms |
| Wall-Clock `SearchAsync` Ende-zu-Ende | 33,3 ms |

Vergleich mit der reinen Search-Query aus M5.12 (MitRolleErsteSeite: 1795 logische
Reads): das **volle** Laden aller 440 Contents und 20 Dependencies kostet nur
~24 zusätzliche logische Reads (~1,3 % der Gesamtkosten) – die Contents-Tabelle
belegt bei dieser Datenmenge ~18 Seiten, die Dependency-Tabelle 2; die Ausführung
bleibt unter der 1-ms-Auflösung. Die Kosten werden von der Search-Query selbst
(Rollen-Auflösung über `RoleResolution`-Index-Seek und Key-Lookups auf
`NodeContent`) bestimmt, nicht von der Freshness-Ladung.

### Entscheidung: keine Begrenzung auf die Freshness-Teilmenge

Ein belegter Engpass liegt **nicht** vor: das vollständige Laden kostet bei der
repräsentativen Datenmenge 24 logische Reads und < 1 ms Ausführung; das Laden
skaliert linear mit dem Gesamtbestand (ein Clustered-Index-Scan pro Tabelle), die
absoluten Werte sind aber um mehr als zwei Größenordnungen von jedem Engpass
entfernt. Eine Begrenzung auf die für die transitive Freshness der Trefferseite
nötige Teilmenge (hier 40 von 460 Zeilen, ~9 %) würde die Gesamtkosten der Suche
nicht messbar senken (1819 → ~1795 logische Reads), würde aber zusätzliche
Queries mit Hit-abhängigen `NodeId/RoleId`-Filtern erfordern und den
Repository-Pfad für einen nicht vorhandenen Nutzen verkomplizieren. Die
Vorher-Messwerte oben sind damit zugleich die entscheidungsrelevanten Werte; ein
Nachher-Messwert entfällt entsprechend. Die Messung wird bei deutlich größerem
Content-Bestand (z. B. Faktor 100: ~44 000 Contents) mit derselben Testklasse
wiederholt – erst wenn der Full-Scan-Anteil die Search-Kosten selbst erreicht,
ist die Teilmenge-Begrenzung zu implementieren.

## M5.16 – Kosten des seitenweisen Diff-Blätterns

Nachweis für `docs/Roadmap.md` M5.16: der Test
`DiffPaging_UeberVolleM512Datenmenge_KostenProSeiteNachgewiesen` in
`tests/KnowHowToAI.IntegrationTests/SqlServer/Abnahme/SqlDiffReleaseAbnahmeTests.DiffPaging.cs`
(Kategorie `ManualDatabaseIntegration`) blaettert einen Snapshot-Diff über die volle
M5.12-Datenmenge vollständig seitenweise durch und misst gelesene Zeilen und Laufzeit
pro Cursor-Seite. Der Diff-Pfad ist bewusst teuer pro Seite: `HistoryService`
lädt pro Cursor-Seite **beide** Snapshots vollständig (je fünf Statements über
`SqlHierarchyRepository`, `SqlRoleRepository`, `SqlContentRepository`,
`SqlDependencyRepository`) und berechnet den Diff über `SnapshotDiffCalculator`
neu; der `DiffCursor` führt nur den fortlaufenden Item-Offset weiter.

### Testgröße und Messmethodik

Basissnapshot in M5.12-Groesse (401 Nodes, 440 Contents, 20 Dependencies, 3 Rollen,
2 Resolution Orders), Aenderungs-Snapshot mit 373 Diff-Eintraegen (210 Nodes:
150 Modified/40 Added/20 Deleted, 162 Contents: 100 Modified/40 Added/22 Deleted,
1 Deleted Dependency). Durchblättern mit Seitengroesse 50 → 8 Seiten (7 × 50,
letzte Seite 23). Gemessen wird je Seite:

1. `CompareSnapshotsAsync` Ende-zu-Ende (Wall-Clock, StopWatch),
2. ein STATISTICS-IO/TIME-Batch mit dem unveränderten SQL-Text der fünf
   `ListBySnapshot`-Statements (je einmal für Base- und Target-Snapshot; die
   SQL-Konstanten sind dafür `internal` sichtbar gemacht), mit gelesenen Zeilen
   pro Statement und logischen Reads pro Tabelle (zweisprachig geparst),
3. einmalig der Ist-Plan via `SET STATISTICS PROFILE`
   (`sys.dm_exec_query_stats` bleibt wegen fehlender
   VIEW SERVER PERFORMANCE STATE-Berechtigung ungenutzt).

### Messergebnis (Umgebung: SQL Server 2022, Datenbank `KnowHowToAi`)

| Seite | Items | logische Reads gesamt | Verteilung (Node/NodeContent/Role/Resolution/Dependency) | SQL-Batch Wall-Clock | `CompareSnapshotsAsync` Wall-Clock |
| --- | --- | --- | --- | --- | --- |
| 1 | 50 | 69 | 23 / 34 / 4 / 4 / 4 | 7,7 ms (inkl. Kompilierung) | 9,9 ms |
| 2–8 | je 50 (Seite 8: 23) | je 69 | identisch zur Seite 1 | je 2,2–2,9 ms | je 3,5–6,2 ms |

Gelesene Zeilen pro Statement und Seite – über alle 8 Seiten konstant
(belegt die vollständige Wiederholungsladung je Cursor-Seite):

| Snapshot | Nodes | Roles | Resolutions | Contents | Dependencies |
| --- | --- | --- | --- | --- | --- |
| Base | 401 | 4 | 5 | 440 | 20 |
| Target | 441 (inkl. 20 Tombstones) | 4 | 5 | 480 (inkl. 22 Tombstones) | 19 |

Gesamt über den vollständigen Durchlauf (8 Seiten): 552 logische Reads,
~38 ms `CompareSnapshotsAsync` inklusive aller Snapshot-Ladungen. Ausführung
und CPU liegen je Statement unter der 1-ms-Auflösung. Ist-Plan: durchgehend
Clustered Index Seek auf `SnapshotId` (PK je Tabelle) plus Sort; kein Scan.

### Entscheidung: V1-adequat, keine Kategorie-Offsets im `DiffCursor`

Ein belegter Engpass liegt **nicht** vor: die Kosten pro Cursor-Seite sind
konstant (69 logische Reads, ~3 ms SQL-Ausführung) und unabhängig vom Offset,
weil alle fünf Statements mit Clustered Index Seek auf `SnapshotId` zugreifen und
die Seitenmenge im Speicher per Offset geschnitten wird. Kategorie-Offsets im
`DiffCursor` würden die SQL-Ladung nicht reduzieren (sie hängt nicht vom Offset
ab) und sparten ausschließlich die In-Memory-Diff-Berechnung ein, die bei 373
Eintraegen unterhalb der Messauflösung liegt – die Statistik-IO-Verteilung wäre
identisch. Der vollständige 8-Seiten-Durchlauf kostet ~38 ms; selbst bei
deutlich größeren Snapshots skaliert nur der konstante Pro-Seiten-Anteil mit
der Snapshotgroesse, nicht die Seitenzahl. Vorher-Messwerte sind die obigen
Werte; ein Nachher entfällt entsprechend. Die Messung wird bei deutlich
größerer Datenmenge (z. B. Faktor 100) mit derselben Testklasse wiederholt;
erst wenn die Pro-Seiten-Ladung den dominanten Kostenanteil realer Workloads
erreicht, ist die Kategorie-Offset-Optimierung zu implementieren. Die Messwerte
werden vom Test laufend neu nach `temp/diff-abnahme-messung.json` geschrieben.

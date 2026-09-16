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
  Diff-Kategorien, historische Reproduktion, Release-Listing über mehrere Seiten.
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

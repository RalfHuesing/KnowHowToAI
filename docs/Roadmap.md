# KnowHowTo AI – Implementierungs-Roadmap

Diese Datei ist Ausführungsplan und verbindliche Statusquelle für V1. Die fachliche
und architektonische Wahrheit bleiben der Index `docs/Konzept.md` und seine
verlinkten Konzeptmodule. Bei einem Widerspruch gilt das Konzept; der Widerspruch
wird vor weiterer Implementierung geklärt und die betroffenen Dokumente werden
anschließend synchronisiert.

## Arbeitsweise für Agenten

- Meilensteine werden in der angegebenen Reihenfolge umgesetzt. Ein späterer Punkt
  darf nur vorgezogen werden, wenn seine Voraussetzungen bereits erfüllt sind.
- `[x]` bedeutet: implementiert, mit dem unter **Abnahme** genannten Nachweis geprüft
  und ohne Placeholder produktiv nutzbar. Angelegte Dateien oder kompilierende
  Stubs allein gelten nicht als erledigte Fachfunktion.
- Ein Elternpunkt wird erst auf `[x]` gesetzt, wenn alle verpflichtenden Unterpunkte
  abgeschlossen sind. Bewusst optionale Punkte sind ausdrücklich als solche markiert.
- Pro Arbeitsschritt wird der kleinste zusammenhängende, vertikal prüfbare Slice
  umgesetzt. Roadmap-Status und neu entdeckte Arbeit werden im selben Slice gepflegt.
- Keine stillen Architekturentscheidungen: Ungeklärte Punkte werden unter
  **Entscheidungsregister** ergänzt und vor davon abhängiger Implementierung geklärt.
- Nach jedem Slice gelten die Quality Gates aus `.agents/rules/Richtlinien.mdc` und
  die Testebenen aus `.agents/rules/TestRichtlinien.mdc`.
- Fehlercodes und öffentliche MCP-Felder sind Verträge. Umbenennungen nach ihrer
  Einführung benötigen eine bewusste Vertragsänderung; V1 benötigt keine
  Abwärtskompatibilität zu noch nicht veröffentlichten Zwischenständen.

## Verbindliche V1-Entscheidungen

- Pro Snapshot gibt es höchstens einen aktiven persistierten Root-Node. Der initiale
  leere Snapshot darf noch keinen Root besitzen. `get_root` liefert dann
  `availability = None` statt einen künstlichen Node zu erfinden.
- Rollen und Role Resolution Orders sind versionierter Wissenszustand. Sie werden
  innerhalb einer KnowHowTo-AI-Transaction über dieselben Service-/MCP-Grenzen wie
  andere fachliche Änderungen verwaltet; direkte Änderungen committed Snapshots per
  SQL sind unzulässig.
- Freshness von `Derived` Content wird transitiv ausgewertet. Zyklen im
  Dependency-Graph sind eine harte Invariante und werden abgelehnt.
- Releases und die minimalen Historienabfragen `get_snapshot`, `list_releases`,
  `compare_snapshots` und `get_transaction_changes` gehören zu V1.
- Gespeicherter Content enthält keine Dokumentüberschriften. Eine normale Erwähnung
  des Node-Titels im Fließtext bleibt erlaubt. Markdown- und HTML-Headings sind harte
  Fehler; überschriftenähnliche Ersatzformatierungen sind Qualitätswarnungen.
- Die Standardwarnschwelle für einen einzelnen normalisierten `ContentMd` beträgt
  4 KiB UTF-8. `NodeTooLarge` verhindert Speichern oder Commit nicht; der Agent soll
  den Inhalt in einer eigenen Transaction in Child-Nodes strukturieren. Die Schwelle
  ist über App-Konfiguration änderbar, ein fachliches hartes Größenlimit gibt es in V1 nicht.
- Die Datenbank enthält ausschließlich Wissens-, Versions-, Transaktions- und
  Release-Daten. Betriebsparameter und nicht versionierte Quality-/Retrieval-Policies
  werden nicht in fachlichen Tabellen gespeichert, sondern beim Prozessstart aus
  zentraler App-Konfiguration gebunden.

## Entscheidungsregister

Offene Punkte blockieren die davon abhängige Implementierung, bis sie bewusst
entschieden sind. Sie werden hier mit Status, betroffenen Meilensteinen und einer
empfohlenen Option ergänzt.

| ID | Entscheidung | Status |
|---|---|---|
| ADR-V1-001 | Höchstens ein persistierter Root; leerer Initialzustand erlaubt | entschieden |
| ADR-V1-002 | Rollenpflege transaktional über Application/MCP | entschieden |
| ADR-V1-003 | Transitive Freshness; Dependency-Zyklen verboten | entschieden |
| ADR-V1-004 | Minimale Release- und Historienfunktionen in V1 | entschieden |
| ADR-V1-005 | 4-KiB-Default als konfigurierbares Soft-Limit; Überschriften bleiben harte Fehler | entschieden |
| ADR-V1-006 | App-Konfiguration statt DB-Konfiguration; harte Invarianten sind nicht abschaltbar | entschieden |
| ADR-V1-007 | Dependency-Löschsemantik: Anlage-/Änderungsvalidierung von Snapshot-Zustandsvalidierung trennen. Neue oder geänderte Dependencies verlangen eine aktive explizite Source; bestehende Provenienz zu einer später gelöschten oder fehlenden Source bleibt erhalten und macht Derived Content transitiv `Stale` (betrifft M2.8, M3.4/M3.5 und M4.3). | entschieden |

## Konfigurationsmodell V1

Konfiguration wird strikt nach Verantwortlichkeit getrennt:

1. **Harte Domain-Invarianten** sind nicht konfigurierbar. Dazu gehören unter anderem
   Heading-Verbot, maximal ein Root, unveränderliche committed Snapshots,
   Transaction-Pflicht, zyklenfreie Hierarchie/Dependencies und exakt ein Treffer bei
   `replace_text`. Sie werden zentral als Domainlogik und stabile Fehlercodes definiert.
2. **Quality-Policies** sind typed Options aus `appsettings*.json`, Environment-
   Variablen oder Kommandozeile. Sie warnen, blockieren aber keinen fachlich gültigen
   Commit. V1-Defaults:

   | Schlüssel unter `KnowHowToAI` | Default | Gültiger Bereich | Bedeutung |
   |---|---:|---:|---|
   | `Validation:ContentSizeWarningBytes` | `4096` | `512..1048576` | `NodeTooLarge` ab normalisierter UTF-8-Größe |
   | `Validation:ChildCountWarning` | `25` | `2..10000` | `TooManyChildren` für einen Parent |
   | `Validation:HierarchyDepthWarning` | `8` | `2..256` | `HierarchyTooDeep` ab globaler Node-Tiefe |
   | `Validation:PossibleEmbeddedHeadingWarning` | `true` | Boolean | heuristische Warnung für Ersatztitel |

3. **Retrieval-/Transportgrenzen** sind typed Options. V1-Defaults:

   | Schlüssel unter `KnowHowToAI` | Default | Gültiger Bereich | Bedeutung |
   |---|---:|---:|---|
   | `Retrieval:DefaultPageSize` | `20` | `1..MaximumPageSize` | Standardseite für Listen |
   | `Retrieval:MaximumPageSize` | `100` | `1..1000` | größte akzeptierte Listenseite |
   | `Retrieval:SearchPageSize` | `10` | `1..SearchMaximumPageSize` | Standardseite für Search |
   | `Retrieval:SearchMaximumPageSize` | `50` | `1..500` | größte akzeptierte Search-Seite |
   | `Retrieval:SnippetMaximumCharacters` | `300` | `50..4000` | maximale Länge eines Search-Snippets |

4. **Storage-/Startparameter** sind typed Options. V1-Defaults:

   | Schlüssel unter `KnowHowToAI` | Default | Gültiger Bereich | Bedeutung |
   |---|---:|---:|---|
   | `Storage:CommandTimeoutSeconds` | `30` | `1..600` | SQL-Command-Timeout |
   | `Migrations:LockTimeoutSeconds` | `60` | `1..600` | Warten auf Migration Lock |
   | `Migrations:ApplyOnStartup` | `true` | Boolean | ausstehende Migrationen beim Serverstart anwenden |

5. **Datenbankverbindung** wird ausschließlich aus der eigenen versionierten
   `DatabaseConnection`-Sektion in `appsettings.json` gelesen. Der Serverwert darf
   einen zur Laufzeit expandierten Windows-Platzhalter enthalten; es gibt keine
   alternative Connection-String-Umgebungsvariable. Die verbindliche Beschreibung
   steht in [Anwendungskonfiguration](konzept/01-Grundlagen-Hierarchie-Markdown.md).

Für Quality-, Retrieval- und Start-Policies ist die effektive Reihenfolge `appsettings.json` <
`appsettings.{Environment}.json` < Environment-Variablen mit Präfix/Mapping
`KnowHowToAI__...` < Kommandozeilenargumente. Optionen werden einmal beim Start in
immutable Records gebunden, vollständig validiert und anschließend nicht live neu
geladen. Ungültige, widersprüchliche oder außerhalb zentral definierter technischer
Sicherheitsbereiche liegende Werte verhindern den Start mit einem klaren Fehler.
Änderungen erfordern einen Prozessneustart.

Defaultwerte stehen genau einmal in der versionierten `appsettings.json`;
Validierungsbereiche und übergreifende Beziehungen stehen genau einmal in zentralen
`*OptionsValidator`-Typen. Typed Options besitzen keine abweichenden versteckten
Fallbackwerte: Fehlt die zentrale Konfiguration, schlägt der Start verständlich fehl.
Keine Magic Numbers in Handlern, Services, Repositories oder Tests. Tests dürfen
abweichende Options explizit injizieren. Warnungen geben den effektiv verwendeten
Grenzwert und Ist-Wert zurück. Da eine Serverinstanz genau eine Datenbank bedient,
können unterschiedliche Wissensbasen über unterschiedliche App-Konfigurationen
verschiedene Policies erhalten, ohne Konfiguration in der Datenbank zu speichern.

---

## M0: Solution-Setup & Testinfrastruktur

**Statusziel:** Kompilierbares Greenfield-Gerüst. Fachfunktionen dürfen noch
Placeholder sein.

**Konzeptbezug:** [Grundlagen, Konfiguration und Systemgrenzen](konzept/01-Grundlagen-Hierarchie-Markdown.md),
[V1-Architektur und Invarianten](konzept/06-Datenmodell-V1-Invarianten-Architektur.md)

- [x] **M0.1: Solution- und Projektstruktur**
  - [x] `KnowHowToAI.slnx`
  - [x] `src/KnowHowToAI.Core` für Domain, Application-Verträge und Ports
  - [x] `src/KnowHowToAI.Storage.SqlServer` für SQL-Persistenz und Migrationen
  - [x] `src/KnowHowToAI.Server` ausschließlich für Hosting, Konfiguration und MCP
  - [x] `tests/KnowHowToAI.Core.Tests` für FastTests
  - [x] `tests/KnowHowToAI.IntegrationTests` für echte SQL-/STDIO-Grenzen
  - [x] initialen, ausdrücklich anpassbaren Namespace- und Ordnerrahmen aus dem
    Konzept ableiten und in `docs/Projektstruktur.md` erläutern
- [x] **M0.2: Zentrale Build- und Paketkonfiguration**
  - [x] .NET 10, Nullable und `TreatWarningsAsErrors` zentral aktivieren
  - [x] Central Package Management einrichten
  - [x] `.editorconfig` für Formatierung und Dateikonventionen bereitstellen
  - [x] AiNetLinter-Konfiguration auf die neue Solution ausrichten
- [x] **M0.3: Testskripte und initialer Nachweis**
  - [x] `scripts/test-fast.ps1`
  - [x] `scripts/test-integration.ps1`
  - [x] Initialer Build und Placeholder-FastTest erfolgreich
- [x] **M0.4: Zentrale Anwendungskonfiguration**
  - [x] `appsettings.json` mit dem V1-Konfigurationsbaum einschließlich der eigenen
    `DatabaseConnection`-Sektion anlegen
  - [x] immutable typed Options ohne versteckte Fallbackwerte und zentrale
    Options-Validatoren definieren; Defaults ausschließlich aus `appsettings.json` laden
  - [x] alle Options mit verständlichen Startup-Fehlern validieren, einschließlich
    `Default <= Maximum`, positiver Timeouts und sinnvoller Mindest-/Höchstwerte
  - [x] Override-Reihenfolge und Environment-Variablennamen dokumentieren
  - [x] sicherstellen, dass Domain und Storage keine direkte Abhängigkeit von
    `IConfiguration` oder `IOptions` erhalten; der Composition Root übergibt Records
  - [x] Tests für Defaults, Overrides, ungültige Werte und Secret-Redaction

**Abnahme:** Solution baut warnungsfrei; beide Testprojekte sind auffindbar;
Konfigurationsdefaults, Overrides und ungültige Werte sind automatisiert belegt.
Dieser Meilenstein behauptet ausdrücklich noch keine getestete Fachlogik.

---

## M1: SQL-Schema, Migrationen & reale Testumgebung

**Voraussetzung:** M0.
**Statusziel:** Reproduzierbares, nebenläufig sicher migrierbares Schema auf einem
echten SQL Server >= 2019. Erst danach darf die Snapshot-Engine implementiert werden.

**Konzeptbezug:** [Snapshots und historische Reproduzierbarkeit](konzept/03-Transaktionen-Snapshots-Releases.md),
[relationales Datenmodell](konzept/06-Datenmodell-V1-Invarianten-Architektur.md)

- [x] **M1.1: Initiales relationales Schema entwerfen**
  - [x] System-, Snapshot-, Transaction- und Release-Tabellen
  - [x] versionierte Rollen und Role Resolution Orders
  - [x] versionierte Nodes, Contents und Content Dependencies
  - [x] initialen committed Snapshot und Rolle `Default` seeden
- [x] **M1.2: Schema vor Implementierungsbeginn härten**
  - [x] Zustände, Zeitstempel, Fremdschlüssel, Eindeutigkeiten und Indizes gegen das
    Konzept auditieren; solange noch kein unterstützter Datenbankstand existiert, die
    Greenfield-Baseline direkt korrigieren statt künstliche Reparaturmigrationen anzulegen
  - [x] Target und Source einer Dependency per zusammengesetztem Fremdschlüssel an
    expliziten Content desselben Snapshots binden; Aktivstatus, Revisionsgleichheit und
    Graphregeln bleiben bewusst fachliche Prüfungen in M2.8
  - [x] die für Snapshot-Kopie, Root-Abfrage, Geschwistersortierung, Rollenauflösung und
    Freshness nötigen Basisindizes definieren; Search-Abfragepläne mit den realen Queries
    und Datenmengen in M5.3/M7.3 prüfen
  - [x] `Priority > 0`, eindeutige Kandidaten und deterministische Reihenfolge absichern
  - [x] lokal beweisbare Status-/Zeitstempel-Kombinationen absichern: Working/Open ohne
    Commitzeit und Committed mit Commitzeit; ausschließlich committed als Current zu
    aktivieren bleibt Teil des atomaren Commit-Use-Cases M3.5
  - [x] monotone `ChangeVersion` je offener Transaction für konsistente Cursor auf dem
    veränderlichen Working Snapshot vorsehen
  - [x] eindeutige, nicht leere Release-Namen und relationale Snapshot-Verweise absichern;
    committed Zielzustand und Unveränderlichkeit zusätzlich im Release-Use-Case M5.5
    garantieren
- [x] **M1.3: Migration Runner**
  - [x] `ISchemaMigrator` als Port und SQL-Server-Implementierung erstellen
  - [x] reentrantes, selbst nicht journalisiertes Bootstrap für das Migration Journal mit
    Version, Name, SHA-256-Checksum und `AppliedAtUtc` bereitstellen
  - [x] Checksum deterministisch über den als UTF-8/LF normalisierten Skriptinhalt
    berechnen; das Journal, nicht `IF OBJECT_ID`, ist die Idempotenzquelle
  - [x] eingebettete Skripte strikt numerisch sortieren; doppelte Versionen ablehnen
  - [x] bereits angewendete Skripte nicht erneut ausführen; geänderte Checksum mit
    `MigrationChecksumMismatch` ablehnen
  - [x] ab dem ersten unterstützten/deployten Datenbankstand angewendete Migrationen
    unverändert lassen und jede Schemaänderung ausschließlich als neue Migration ergänzen
  - [x] je Migration kurze SQL-Transaction verwenden und parallele Runner per
    SQL-Applikationssperre serialisieren
  - [x] Fehler mit Skriptname und Fehlercode, aber ohne Connection String/Credentials
    protokollieren; keine teilweise als erfolgreich markierte Migration
- [x] **M1.4: SQL-Integrationstest-Harness**
  - [x] Verbindung ausschließlich aus der dokumentierten `DatabaseConnection`-Sektion
    in `appsettings.json` lesen und `%COMPUTERNAME%` im Serverwert erst zur Laufzeit
    auflösen
  - [x] die manuell bereitgestellte Datenbank als gegeben annehmen und niemals eine
    Datenbank erzeugen oder entfernen
  - [x] fehlende Voraussetzungen mit klarer Preflight-Meldung melden, niemals als
    scheinbar grünen Test überspringen
  - [x] mutierende M1.5-Migrationstests ausschließlich als explizite
    `ManualDatabaseIntegration` gegen eine dedizierte, manuell bereitgestellte
    Testdatenbank ausführen; der Harness setzt ausschließlich bekannte
    KnowHowToAI-Tabellen zurück und erzeugt oder entfernt keine Datenbank
- [x] **M1.5: Integrationsnachweise**
  - [x] frische Datenbank wird vollständig erstellt und geseedet
  - [x] zweiter Lauf ist ohne Schemaänderung erfolgreich
  - [x] veränderte Checksum wird abgelehnt
  - [x] parallele Runner wenden jede Migration genau einmal an
  - [x] Fehler in einer Migration hinterlässt weder Journal-Eintrag noch Teilschema
- [x] **M1.6: Audit-Nacharbeiten vor M2**
  - [x] die separate `DatabaseConnection`-Sektion am Composition Root binden und
    validieren, in `SqlStorageConnectionString` überführen sowie Connection Factory,
    Migrationskatalog und `ISchemaMigrator` vollständig registrieren
  - [x] bei `Migrations:ApplyOnStartup = true` ausstehende Migrationen genau einmal vor
    der Betriebsbereitschaft ausführen, bei `false` sicher überspringen und Startfehler
    ohne Credentials ausgeben; beide Pfade durch Hosting-Integrationstests belegen
  - [x] den SQL-Preflight um den Nachweis SQL Server >= 2019 ergänzen
  - [x] die Schemaabnahme über bloße Tabellen- und Indexnamen hinaus auf Spalten,
    Datentypen, Nullability, Defaults, Check Constraints, Fremdschlüssel sowie
    Indexschlüssel, -reihenfolge, Filter und Includes erweitern; zentrale Constraints
    zusätzlich durch repräsentative Akzeptanz- und Ablehnungsfälle belegen

**Abnahme:** Migrations-Integrationstests laufen gegen echten SQL Server grün; das
resultierende Schema entspricht allen DDL- und Index-Erwartungen.

---

## M2: Domain-Kern, Verträge & deterministische Invarianten

**Voraussetzung:** M0; für reine Domain-Tests nicht M1.
**Statusziel:** SQL- und transportfreie Fachlogik mit vollständigen FastTests.

**Konzeptbezug:** [Hierarchie und Markdown](konzept/01-Grundlagen-Hierarchie-Markdown.md),
[Rollen, Provenienz und Drift](konzept/02-Rollen-Provenienz-Drift.md),
[Retrieval und Validierung](konzept/04-Export-Retrieval-Validierung.md),
[V1-Invarianten](konzept/06-Datenmodell-V1-Invarianten-Architektur.md)

- [x] **M2.1: Grundtypen und Ergebnisvertrag**
  - [x] starke/opaque IDs und unveränderliche Modelle für Snapshot, Transaction, Node,
    Role, NodeContent, Dependency und Release
  - [x] zentrale Enums für Zustände, `ContentMode`, `Availability` und `Freshness`
  - [x] `Result<T>` mit stabilem `code`, maschinenlesbaren `details` und `warnings`;
    erwartete Fachfehler nicht als Exceptions modellieren
  - [x] Uhrzeit und ID-Erzeugung über injizierbare Ports deterministisch testbar machen
  - [x] `CancellationToken` an allen asynchronen Application-/Storage-Grenzen führen
- [x] **M2.2: Read-Kontext**
  - [x] genau einen Selektor zulassen: `transactionId`, `snapshotId` oder keinen
    (Current); Kombination mit `InvalidReadContext` ablehnen
  - [x] Transaction liest ausschließlich ihren Working Snapshot
  - [x] historische Reads akzeptieren nur vorhandene Snapshots; Tombstones werden
    standardmäßig nicht als aktive Daten ausgeliefert
- [x] **M2.3: Content-Normalisierung und Revisionen**
  - [x] Eingaben kanonisch auf LF normalisieren; keine semantische Änderung allein
    durch CRLF/LF
  - [x] bei neuem oder tatsächlich geändertem normalisiertem Text eine neue
    `ContentRevisionId` erzeugen
  - [x] bei identischem Text die Revision beibehalten; reine Aktualisierung von Mode
    oder Dependencies ändert die Textrevision nicht
  - [x] Löschen und erneutes Anlegen erzeugt auch bei gleichem Text eine neue Revision
- [x] **M2.4: Heading- und Strukturvalidator**
  - [x] Markdig-AST für ATX- und Setext-Headings verwenden
  - [x] rohe HTML-Elemente `<h1>` bis `<h6>` unabhängig von Groß-/Kleinschreibung
    ablehnen; `HeadingNotAllowed` mit Position und Art zurückgeben
  - [x] Heading-Syntax in Fenced/Indented Code, Inline-Code, Escapes und normalen
    Textvorkommen wie `C#` erlauben
  - [x] alleinstehende Strong-/Emphasis-Absätze und eine alleinstehende Wiederholung
    des Node-Titels als `PossibleEmbeddedHeading` warnen, nicht hart ablehnen
  - [x] persistiertes System-Front-Matter mit `FrontMatterNotAllowed` ablehnen
- [x] **M2.5: Größen- und Strukturwarnungen**
  - [x] normalisierte UTF-8-Größe messen; Standardgrenze 4 KiB aus typed Options verwenden
  - [x] `NodeTooLarge` mit Ist-Größe, Schwelle und Empfehlung für Child-Nodes liefern
  - [x] Warnungen für ungewöhnliche Hierarchietiefe und Child-Anzahl vorbereiten
  - [x] Warnungen blockieren weder Mutation noch Commit und verändern keinen Content
- [x] **M2.6: Hierarchie-Invarianten**
  - [x] maximal einen aktiven Root pro Snapshot; leerer Baum zulässig
  - [x] Parent muss aktiv im selben Snapshot existieren; Self-Parent und Zyklen ablehnen
  - [x] `NodeId` nie wiederverwenden; Titel leer/Whitespace ablehnen
  - [x] Geschwister deterministisch nach `SortOrder`, danach `NodeId` sortieren
  - [x] Create/Move/Reorder normalisiert betroffene Geschwister atomar auf lückenlose,
    eindeutige SortOrder-Werte
- [x] **M2.7: Rollenauflösung**
  - [x] explizite, nicht rekursive Kandidatenliste exakt in Priority-Reihenfolge prüfen
  - [x] gelöschte/fehlende Rollen oder doppelte Kandidaten ablehnen
  - [x] keine impliziten Kandidaten ergänzen; fehlende Konfiguration transparent melden
  - [x] Ergebnis enthält immer `requestedRole`, nullable `resolvedRole`,
    `availability`, `fallbackUsed` und Content-Metadaten
- [x] **M2.8: Dependencies und transitive Freshness**
  - [x] `Independent` hat keine Dependencies; `Derived` hat mindestens eine
  - [x] Source muss aktiver expliziter Content sein; ein Fallback ist keine speicherbare
    Source-Revision
  - [x] Self-Dependency und direkte/transitive Zyklen mit `DependencyCycle` ablehnen
  - [x] `Stale`, wenn Source fehlt/gelöscht ist, ihre aktuelle Revision abweicht oder
    eine abgeleitete Source transitiv stale ist; sonst `Current`
  - [x] dieselbe Logik für Current, historischen und Working Snapshot verwenden
- [x] **M2.9: Textoperationen und Löschung**
  - [x] `replace_text` ändert nur expliziten Content der angefragten Rolle, niemals den
    per Fallback aufgelösten Content
  - [x] exakt ein ordinaler Match; 0 = `TextNotFound`, >1 = `MultipleTextMatches`
  - [x] Ergebnis erneut normalisieren und vollständig validieren
  - [x] `delete_content` tombstoned nur expliziten Rollen-Content
  - [x] `delete_node` mit aktiven Children ohne explizites `deleteSubtree=true` ablehnen;
    Subtree-Löschung tombstoned Nodes und deren Contents konsistent
- [x] **M2.10: FastTests für jeden Domain-Vertrag**
  - [x] Positiv-, Rand- und Negativfälle aus M2.1 bis M2.9
  - [x] Property-/Datentests für Hierarchiezyklen, Sortierung und Match-Anzahlen dort,
    wo sie gegenüber Einzelbeispielen zusätzlichen Fehlerraum abdecken
- [x] **M2.11: Audit-Nacharbeiten**
  - [x] Geschwister ausschließlich innerhalb desselben Snapshots normalisieren und bei
    Create/Move/Reorder/Delete nicht betroffene Geschwistergruppen unverändert lassen
  - [x] unbekannte `ContentMode`-Werte als harte Dependency-Verletzung ablehnen
  - [x] obsolete Namespace- und Test-Placeholder aus bereits belegten M2-Bereichen entfernen
  - [x] ADR-V1-007 entscheiden und die Dependency-Prüfung so aufteilen, dass neue oder
    geänderte Dependencies weiterhin eine aktive explizite Source verlangen, eine später
    tombstoned/fehlende Source bei bestehender Provenienz aber den Derived-Content
    transitiv `Stale` machen kann, ohne einen ansonsten gültigen Snapshot zu blockieren
  - [x] den vollständigen Lösch-Lebenszyklus mit FastTests belegen: `delete_content` und
    `delete_node` behandeln Target-/Source-Dependencies atomar, erhalten beabsichtigte
    Stale-Provenienz und liefern anschließend einen konsistent validierbaren Snapshot

**Abnahme:** Core enthält keine SQL-/MCP-Abhängigkeit; alle genannten Invarianten sind
durch FastTests belegt; Placeholder-Code und Placeholder-Tests sind entfernt.

---

## M3: SQL-Repositories & Snapshot-/Transaction-Engine

**Voraussetzung:** M1 und die benötigten Verträge aus M2.
**Statusziel:** Kurze atomare SQL-Operationen implementieren das vollständige
Snapshot-Modell ohne lang laufende SQL-Transaction.

**Konzeptbezug:** [Transaktionen, Snapshots und Releases](konzept/03-Transaktionen-Snapshots-Releases.md),
[Persistenzmodell](konzept/06-Datenmodell-V1-Invarianten-Architektur.md)

- [x] **M3.1: Repository-Ports und SQL-Grundlage**
  - [x] Connection Factory, parametrisierte Dapper-Zugriffe und zentrale Mappings
  - [x] kein dynamisches SQL aus Nutzereingaben; Cancellation und Timeouts durchreichen
  - [x] Repository liefert Daten/Fachzustände, aber keine MCP-Typen
- [x] **M3.2: `begin_transaction`**
  - [x] Current Snapshot unter geeigneter Sperre lesen und neuen Working Snapshot anlegen
  - [x] vollständige Kopie von Rollen, Role Resolutions, Nodes, NodeContents und
    ContentDependencies per `INSERT ... SELECT`
  - [x] Transaction mit Base-/Working-Snapshot und optionalen Audit-Metadaten anlegen
  - [x] gesamte Eröffnung atomar; bei Fehler weder halber Snapshot noch offene
    Transaction
- [x] **M3.3: Mutationen auf Working Snapshot**
  - [x] bei jeder Mutation Transaction vorhanden/offen und Snapshot `Working` prüfen
  - [x] committed/discarded Snapshots nie verändern
  - [x] Mutationen derselben Transaction über die Transaction-Zeile serialisieren und
    `ChangeVersion` bei jeder erfolgreichen Zustandsänderung atomar erhöhen
  - [x] Soft-Delete/Tombstone-Semantik in sämtlichen Abfragen konsistent anwenden
- [x] **M3.4: `validate_transaction`**
  - [x] harte Fehler, Warnungen, stale Contents und Refactoring-Kandidaten aggregieren
  - [x] deterministische Sortierung und deduplizierte Befunde
  - [x] Validation ist read-only und mehrfach identisch aufrufbar
- [x] **M3.5: `commit_transaction`**
  - [x] in einer kurzen SQL-Transaction Open-/Working-Zustand und harte Validatoren prüfen
  - [x] Current-Zeile sperren und `BaseSnapshotId == CurrentSnapshotId` vergleichen
  - [x] bei Konflikt keinerlei Statusänderung; `SnapshotConflict` enthält Base und Current
  - [x] bei Erfolg Snapshot, SystemState und Transaction atomar auf Committed setzen
  - [x] wiederholter Commit/Commit nach Discard liefert stabil `TransactionClosed`
- [x] **M3.6: `discard_transaction`**
  - [x] Open Transaction und Working Snapshot atomar auf Discarded setzen
  - [x] Current Snapshot unverändert lassen; Working-Daten zur Historie behalten
  - [x] wiederholter Discard/Discard nach Commit liefert stabil `TransactionClosed`
- [x] **M3.7: SQL-Integrationstests**
  - [x] Begin kopiert alle fünf versionierten Datenbereiche vollständig
  - [x] eigene Writes sind im Working Read sichtbar, aber nicht im Current Read
  - [x] Commit und Discard inklusive Zuständen/Zeitstempeln
  - [x] zwei parallele Transactions: genau der erste Commit gewinnt
  - [x] Rollback bei injiziertem Fehler in Begin, Mutation und Commit
  - [x] historische committed Snapshots bleiben byte-/wertgleich reproduzierbar
- [x] **M3.8: Konsistente Validierungsansicht unter parallelen Working-Mutationen**
  - [x] vollständige Validierungsdaten (Rollen, Resolution Orders, Nodes, NodeContents
    und ContentDependencies) innerhalb einer einzelnen kurzen, read-only SQL-Transaction
    nach Open-/Working-Guard und unter derselben Transaction-Zeilen-Sperre lesen
  - [x] `validate_transaction` ausschließlich aus dieser atomaren Ansicht erzeugen; keine
    parallelen Einzel-Reads mit gemischten Snapshot-Zeitständen
  - [x] echte SQL-Integration mit gezielt überlappender Mutation/Validierung: der Befund
    repräsentiert vollständig den Zustand vor oder nach der Mutation, niemals einen
    Mischzustand; Wiederholung ohne Mutation bleibt identisch und read-only
  - [x] fehlende, geschlossene oder nicht mehr offene Working-Transaction liefert dabei
    weiterhin stabile Fehlercodes ohne Zustands- oder `ChangeVersion`-Änderung
- [x] **M3.9: Deterministischer SQL-Überlappungsnachweis für die Validierungsansicht**
  - [x] Test-Hook oder äquivalente kontrollierte SQL-Synchronisation nach Validation-Guard
    beziehungsweise zwischen zwei gezielt gewählten Datenreads bereitstellen, ohne den
    Produktionsvertrag zu verändern
  - [x] Mutation und Validation kontrolliert verschränken und nachweisen, dass die
    Validation ausschließlich einen vollständigen Vor- oder Nachzustand der fünf
    versionierten Datenbereiche sieht, niemals einen Mischzustand
  - [x] den Befund über mehrere zusammenhängende Befundarten und unveränderten
    `ChangeVersion` bei reiner Validation absichern; Test deterministisch und ohne
    Wall-Clock-Rennen ausführen
- [x] **M3.10: Review/Audit der M3-Umsetzung**
  - [x] Snapshot-Kopie, Working-Guards, Mutationsserialisierung, atomaren Commit/Discard,
    Konflikterkennung und Connection-Lebensdauer gegen Konzeptmodule 03 und 06 prüfen
  - [x] Validation-Sicht, Domain-Mapping, parametrisierte SQL-Zugriffe, Cancellation und
    Soft-Delete-/Dependency-Semantik gegen die betroffenen Invarianten prüfen
  - [x] als Mini-Nacharbeit per SQL-Regressionstest belegen, dass eine erfolgreiche
    Mutation ohne Zustandsänderung die `ChangeVersion` nicht erhöht
  - [x] keine konzeptwidrigen oder größeren offenen Findings; deshalb sind keine
    zusätzlichen M3-Nacharbeitspunkte erforderlich


**Abnahme:** Alle M3-Integrationstests grün; es bleibt zwischen MCP-Aufrufen keine
offene SQL-Transaction oder Connection bestehen.

---

## M4: Application Services & vollständige Mutationsfälle

**Voraussetzung:** M2, M3.
**Statusziel:** Transportneutrale Use Cases orchestrieren Domain und Repository.

**Konzeptbezug:** [MCP-Use-Cases](konzept/05-MCP-API.md) sowie die von jedem
Use Case berührten Fachmodule

- [x] **M4.1: Services**
  - [x] Transaction Service: begin/get/validate/commit/discard
  - [x] Navigation Service: root/node/children/roles und Read-Kontext
  - [x] Mutation Service: Nodes, Content, Rollen und Resolution Orders
  - [x] Export/Search Service sowie History/Release Service als getrennte Zuständigkeiten
  - [x] Validierung und Result-Mapping an einer eindeutigen Schicht, keine doppelte
    abweichende Fachlogik in Handlern und Repositories
- [x] **M4.2: Node-Mutationen**
  - [x] create/update/move/reorder/delete einschließlich Root- und Subtree-Semantik
  - [x] globale Auswirkung von Strukturänderungen in Ergebnissen/Warnungen sichtbar
- [x] **M4.3: Content-Mutationen**
  - [x] replace_content, replace_text und delete_content
  - [x] Mode-/Dependency-Regeln, Revisionsvergabe und Normalisierung atomar anwenden
  - [x] Mutationsergebnis enthält Revision, Snapshot, Warnungen und Freshness
- [x] **M4.4: Rollen-Mutationen**
  - [x] create/update/delete Role nur innerhalb einer offenen Transaction
  - [x] vollständige Resolution Order atomar ersetzen; nie schrittweise Zwischenzustände
  - [x] `delete_role` nur zulassen, nachdem Content-, Dependency- und Resolution-
    Referenzen innerhalb derselben Working Transaction entfernt/ersetzt wurden;
    andernfalls `RoleInUse` mit den blockierenden Referenzen liefern
- [x] **M4.5: Service-Tests**
  - [x] Use-Case-Tests mit In-Memory-Fakes für Orchestrierung und Fehlerweitergabe
  - [x] keine Wiederholung bereits in M2 bewiesener Parser-/Algorithmusvarianten
- [x] **M4.6: Review/Audit der M4-Umsetzung**
  - [x] Transaction-, Navigation-, Node-, Content- und Rollen-Services gegen die
    Konzeptmodule 01 bis 06 sowie die M2-/M3-Invarianten prüfen
  - [x] Repository-Callbacks auf Open-/Working-Guard, atomare Zustandsänderung,
    `ChangeVersion`, Cancellation und unveränderten Current Snapshot abgrenzen
  - [x] Rollen-Neuanlage nach vorheriger Löschung so korrigieren, dass Application-
    Zustand und SQL-Persistenz genau eine Ausprägung derselben `RoleId` enthalten;
    Reaktivierung mit FastTest absichern
  - [x] größere Befunde als ausführbare Nacharbeiten M4.7 bis M4.9 dokumentieren
- [x] **M4.7: Rollenauflösung in Application-Reads zentralisieren und Fehler erhalten**
  - [x] einen gemeinsamen transportneutralen Resolver anlegen, der `RoleResolver.Resolve`
    aufruft, den expliziten `NodeContent` bestimmt und dessen transitive Freshness mit
    `FreshnessEvaluator` berechnet; als Ergebnis `Result<...>` einschließlich
    `requestedRole`, nullable `resolvedRole`, `availability` und `fallbackUsed` liefern
  - [x] `NavigationService` und `MarkdownExportService` auf diesen Resolver umstellen;
    die privaten, derzeit duplizierten `ResolveContent`-/Freshness-Helfer entfernen
  - [x] Fehler von `RoleResolver` niemals in `Availability.None` umwandeln: fehlende oder
    gelöschte Requested-/Candidate-Rollen und ungültige Resolution Orders mit dem
    ursprünglichen stabilen Fehlercode und den Details weitergeben
  - [x] `get_root` auch im leeren Snapshot gegen die angefragte Rolle validieren;
    `Availability.None` bedeutet nur „kein Root/kein auflösbarer Content“, nicht
    „Rollenauflösung war fachlich ungültig“
  - [x] FastTests für gültigen expliziten Content, Fallback, nicht konfigurierte Order,
    fehlende/gelöschte Requested Role sowie fehlende/gelöschte Candidate Role ergänzen
- [x] **M4.8: Read-Kontext-Auflösung vereinheitlichen und Navigation zerlegen**
  - [x] einen gemeinsamen Application-Baustein erstellen, der Current Snapshot sowie
    optional Transaction/historischen Snapshot lädt und genau einmal
    `ReadContextResolver.Resolve` aufruft
  - [x] `NavigationService`, `SearchService` und `MarkdownExportService` auf diesen
    Baustein umstellen und ihre identischen privaten `ResolveContextAsync`-Methoden
    entfernen; Current-, Snapshot- und Transaction-Fehler unverändert weiterreichen
  - [x] `ListChildrenAsync` in getrennte Helfer für Limit, Cursor-Prüfung, Startposition
    und Seitenbildung aufteilen, bis Methodenzeilen sowie zyklomatische und kognitive
    Komplexität innerhalb der AiNetLinter-Grenzen liegen
  - [x] `NavigationServiceTests.cs` nach Use Case auf Dateien unter 500 Zeilen aufteilen,
    ohne Testfälle zu entfernen oder gemeinsame mutable globale Fixtures einzuführen
- [x] **M4.9: Service-Vertragsmatrix vervollständigen**
  - [x] für `create/update/move/reorder/delete_node`, `replace_content`, `replace_text`,
    `delete_content`, `create/update/delete_role` und `set_role_resolution` je einen
    fehlenden und einen geschlossenen Transaction-Fall ergänzen; stabilen Fehlercode,
    Details, unveränderten Working-Zustand und unveränderte `ChangeVersion` prüfen
  - [x] für jeden erfolgreichen Write belegen, dass ausschließlich der Working Snapshot
    geändert wird und Current bis zum Commit unverändert bleibt; vorhandene M3-SQL-
    Nachweise wiederverwenden und nur fehlende Boundary-Fälle als Integrationstest ergänzen
  - [x] No-op-Fälle für identische Node-/Content-/Rollenwerte prüfen: keine unnötige
    Revision und keine Erhöhung der `ChangeVersion`; fachlich echte Änderungen erhöhen
    sie genau einmal
  - [x] Transaction Service für fehlende/geschlossene Transaction bei get, validate,
    commit und discard sowie für unveränderte Fehler-/Warnungsweitergabe vollständig testen

**Abnahme:** Jeder fachliche V1-Write ist über genau einen transportneutralen Use Case
erreichbar und benötigt eine offene KnowHowTo-AI-Transaction.

---

## M5: Retrieval, Export, Search, Historie & Releases

**Voraussetzung:** M4.
**Statusziel:** Vollständige, deterministische Read-Seite mit begrenzten Antworten.

**Konzeptbezug:** [Transaktionen, Historie und Releases](konzept/03-Transaktionen-Snapshots-Releases.md),
[Export, Retrieval und Validierung](konzept/04-Export-Retrieval-Validierung.md),
[Historienmodell](konzept/06-Datenmodell-V1-Invarianten-Architektur.md)

- [x] **M5.1: Metadata-first Navigation**
  - [x] `get_root`, `get_node`, `list_children`, `list_roles`
  - [x] `list_children` liefert standardmäßig keinen Content, sondern NodeId, Titel,
    Description, ChildCount, ContentSize, Availability, ResolvedRole und Freshness
  - [x] deterministische Sortierung und Cursor-/Limit-Paging; ungültige/abgelaufene
    Cursor mit stabilem Fehler statt stiller Ergebnisverschiebung
  - [x] Cursor an Snapshot, Query/Filter und bei Working Reads an `ChangeVersion`
    binden; nach einer Mutation mit `CursorExpired` ablehnen
- [x] **M5.2: Markdown-Export**
  - [x] ausgewählter Root wird H1, Nachfahren erhalten relative Heading-Level
  - [x] Rollenauflösung und Freshness pro Node; Node ohne Content nur bei exportiertem
    Nachfahren aufnehmen
  - [x] gelöschte/irrelevante Zweige auslassen; stabile Leerzeilen und LF-Ausgabe
  - [x] tiefer als sechs Ebenen: gültige, dokumentierte Strategie festlegen und testen,
    bevor der Exportvertrag veröffentlicht wird
- [x] **M5.3: Textsuche V1**
  - [x] parametrisierte Suche über Title, Description und aktiven auflösbaren Content
  - [x] SQL-Wildcards und Sonderzeichen sicher behandeln; keine dynamische SQL-Konkatenation
  - [x] schlanke Snippets mit begrenzter Länge, Trefferfeld und Node-Metadaten
  - [x] feste Maximalwerte, Paging und deterministisches Ranking/Tie-Breaking
  - [x] reale SQL-Abfragepläne und repräsentative Datenmengen messen; zusätzliche
    Search-Indizes nur evidenzbasiert als neue Migration ergänzen
  - [x] dokumentieren, dass V1 weder semantische noch linguistisch vollständige Suche
    verspricht
- [x] **M5.4: Historie und Diff**
  - [x] `get_snapshot` mit Zustand und Metadaten
  - [x] `compare_snapshots` als strukturierter Netto-Diff für Nodes, Rollen, Resolution
    Orders, Contents und Dependencies; keine Rekonstruktion eines Operation Logs
  - [x] `get_transaction_changes` vergleicht Base und Working/Committed Snapshot
  - [x] große Diffs paginieren; Reihenfolge und Change-Arten stabil halten
- [x] **M5.5: Releases**
  - [x] `create_release` registriert atomar einen unveränderlichen Namen/Verweis auf einen
    committed Snapshot; dies ist Metadatenregistrierung und keine Snapshot-Mutation
  - [x] `list_releases` paginiert und deterministisch sortiert
  - [x] normaler Commit/Release darf stale oder übergroßen Content enthalten; Befunde
    werden transparent zurückgegeben, da komplexe Release Policies nicht V1 sind
- [x] **M5.6: Fast- und Integrationstests**
  - [x] Export-Golden-Cases für leere, gefilterte, fallback- und stale Bäume
  - [x] Search/Paging einschließlich Sonderzeichen und Größenlimits
  - [x] Snapshot-/Transaction-Diffs für create/update/move/delete und Rollenänderungen
  - [x] Release nur auf committed Snapshot, Namenskonflikt und historische Reproduktion
- [x] **M5.7: Review/Audit der M5-Umsetzung**
  - [x] Navigation, Export, Search, Historie, Diff und Releases gegen die Konzeptmodule
    02 bis 06, die M5-Abnahme und die Test-/Qualitätsrichtlinien prüfen
  - [x] versteckte Policy-Fallback-Konstruktoren entfernen; nichtpositive explizite
    Limits bei History und Releases wie bei Navigation/Search auf den konfigurierten
    Default zurückführen
  - [x] Working-Diff-Cursor ohne oder mit abweichender `ChangeVersion` als
    `CursorExpired` ablehnen; Cursor nur im dokumentierten opaken Base64Url-Format
    akzeptieren und strukturell ungültige Cursorwerte zurückweisen
  - [x] Release-Findings vor der persistierenden Registrierung bestimmen, damit nach
    erfolgreichem Insert keine nachgelagerte Findings-Abfrage den Aufruf scheitern lässt
  - [x] den im M5-Slice neu eingeführten Snippet-Default im Konzept mit dem seit M0
    verbindlichen Appsettings-Default von 300 Zeichen synchronisieren und die falsche
    Zuordnung der Search-Grenzen zu ADR-V1-006 entfernen
  - [x] M5-bedingte AiNetLinter-Verstöße durch kleinere Use-Case-Helfer, gemeinsame
    Read-Kontext-/Content-Auflösung und ausgelagerten Navigation-Testsupport beseitigen
  - [x] größere Befunde als ausführbare Nacharbeiten M5.8 bis M5.12 dokumentieren
- [x] **M5.8: `list_roles` begrenzen und paginieren**
  - [x] `ListRolesQuery` mit `ReadContext`, optionalem `limit` und opakem `cursor`
    sowie `RolePage` mit `Items` und `NextCursor` einführen; die bisherige unpaginierte
    `IReadOnlyList<Role>`-Antwort ersetzen
  - [x] Rollen deterministisch nach `RoleId.Value` ordinal aufsteigend sortieren und
    höchstens `MaximumPageSize` Einträge ausgeben; ohne positives Limit
    `DefaultPageSize` verwenden
  - [x] den Cursor an `SnapshotId`, `IncludeDeleted` und bei Transaction-Reads an
    `ChangeVersion` binden; falsche Query-/Snapshot-Bindung mit `InvalidCursor` und
    Mutation bzw. Current-Wechsel mit `CursorExpired` ablehnen
  - [x] FastTests für Current, historischen Snapshot und Working Transaction sowie für
    Maximalgrenze, lückenlose Folgeseiten, manipulierten Cursor und Cursor-Ablauf ergänzen
- [x] **M5.9: Konsistente Read-Sicht für Working-Snapshots hergestellt**
  - [x] einen Persistence-Port für eine zusammenhängende M5-Read-Sicht definieren, der
    Transaction, `ChangeVersion`, Nodes, Rollen, Resolution Orders, Contents und
    Dependencies aus genau einer kurzen, konsistenten SQL-Sicht zurückgibt
  - [x] die SQL-Implementierung mit derselben Transaction-Zeilensperre wie Working-
    Mutationen serialisieren; vor dem ersten Read `Open`/Working prüfen und die gelesene
    `ChangeVersion` zusammen mit den Daten zurückgeben, ohne Connection oder SQL-
    Transaction über den Repository-Aufruf hinaus offen zu halten
  - [x] Navigation, Export und Transaction-Diff auf diese Sicht umstellen; Search-Hits,
    Contents und Dependencies ebenfalls in einer kurzen konsistenten SQL-Transaction
    lesen, damit kein Ergebnis Daten aus zwei `ChangeVersion`-Ständen mischt
  - [x] deterministische Integrationstests mit Barrieren statt Zeitverzögerungen
    ergänzen: konkurrierende Mutation liegt vollständig vor oder nach dem Read; ein
    Mischzustand ist unmöglich und ein alter Folgecursor liefert `CursorExpired`
- [x] **M5.10: Rollenauflösung und Search-Semantik fehlertransparent machen**
  - [x] zuerst M4.7 abschließen und den dort geforderten gemeinsamen `Result`-basierten
    Resolver in Navigation und Export verwenden; Fehler der Rollenauflösung niemals als
    `Availability.None`, leeren Export oder still ausgelassenen Node behandeln
  - [x] für Search ohne `RoleId` ausschließlich die rollenunabhängigen Felder `Title`
    und `Description` durchsuchen; keinen alphabetisch zufälligen Rollen-Content wählen
  - [x] für Search mit `RoleId` die angefragte aktive Rolle und ihre vollständige
    Resolution Order validieren; dieselben stabilen Fehlercodes und dieselbe erste
    Content-Auswahl wie `RoleResolver` liefern
  - [x] Fast- und SQL-Vertragstests für expliziten Content, Fallback, keine konfigurierte
    Order, fehlende/gelöschte Requested-/Candidate-Rolle sowie Search ohne Rolle ergänzen
- [x] **M5.11: Release-Registrierung im SQL-Repository atomar abgesichert**
  - [x] `SqlReleaseRepository.CreateAsync` so ändern, dass Existenz und Zustand
    `Committed` im selben kurzen SQL-Vorgang wie das Insert geprüft werden; zwischen
    Service-Vorprüfung und Insert darf kein Release auf einen inzwischen verworfenen
    Working Snapshot entstehen
  - [x] die Fälle fehlender Snapshot, nicht committed Snapshot und doppelter Name
    weiterhin eindeutig auf `SnapshotNotFound`, `SnapshotNotCommitted` und
    `ReleaseNameConflict` abbilden; keine SQL-Fehlermeldung oder Verbindungsdaten leaken
  - [x] einen deterministischen SQL-Rennentest ergänzen, der den Snapshot-Zustand genau
    zwischen Vorprüfung und Repository-Aufruf ändert und beweist, dass kein Release-
    Datensatz angelegt wird
- [x] **M5.12: Search-Abfrageplan und realistische SQL-Abnahme nachgewiesen**
  - [x] eine repräsentative, dokumentierte Testgröße für Nodes, Rollenauflösungen und
    Contents festlegen und in einer dedizierten manuell bereitgestellten Testdatenbank
    ausschließlich über Working-Transaction/Commit-Pfade erzeugen; committed Snapshots
    in Tests nicht direkt per SQL verändern
  - [x] für die reale parametrisierte Search-Query tatsächlichen Ausführungsplan,
    logische Reads und Laufzeit erfassen und das Ergebnis mit Datenmenge und SQL-Server-
    Version unter `docs/` dokumentieren
  - [x] nur bei belegtem Engpass eine neue additive Migration mit Search-Indizes
    anlegen; die bereits angewendete Greenfield-Baseline nicht verändern und Vorher-/
    Nachher-Messwerte dokumentieren
  - [x] SQL-Integrationstests für alle fünf Diff-Kategorien, historische Reproduktion
    nach späterem Commit sowie Release-Listing über mehrere Seiten ergänzen; feste IDs
    und Zeitwerte verwenden und keine Wall-Clock- oder Zufallsabhängigkeit einführen
- [x] **M5.13: Gemeinsamen Snapshot-Lader für Navigation und Export schaffen**
  - [x] einen gemeinsamen transportneutralen Baustein (z. B. statischer Helfer
    `SnapshotReadDataLoader`) anlegen, der für einen `ReadContext` entweder die konsistente
    Working-Sicht (`IWorkingSnapshotReadRepository.ReadOpenWorkingAsync`) oder
    `ReadContextReader.ResolveAsync` plus die fünf Listen-Reads (Hierarchy, Roles,
    Resolutions, Contents, Dependencies) nutzt und einheitlich einen Kontext mit den
    gefilterten Active-Daten (`ActiveReadFilter`) zurückgibt
  - [x] `NavigationService.LoadNavigationSnapshotDataAsync` sowie die Lademethoden des
    `MarkdownExportService` (`LoadExportDataAsync`, `LoadWorkingExportDataAsync`,
    `LoadCommittedExportDataAsync`) vollständig durch diesen Baustein ersetzen; den inline
    duplizierten `InvalidReadContext`-Check entfernen, weil `ReadContextResolver` ihn
    bereits liefert
  - [x] die identischen Records `NavigationRepositories` und `HistoryRepositories` für
    den neuen Baustein zusammenführen oder den Baustein direkt mit den Einzelports
    parametrisieren; alle Konstruktionstellen vorher per Suche (`rg "NavigationRepositories("`
    und `rg "HistoryRepositories("`) finden und anpassen; keine weiteren Kopien des Records anlegen
  - [x] Fehlercodes, Prüfungsreihenfolge, Freshness- und Paging-Ergebnisse dürfen sich nicht
    ändern; bestehende Fast- und Integrationstests bleiben ohne fachliche Anpassung grün
- [x] **M5.14: Current-Snapshot-Read nur bei Current-Kontexten laden**
  - [x] `ReadContextReader.ResolveAsync` so ändern, dass `GetCurrentAsync` ausschließlich
    aufgerufen wird, wenn `context.TransactionId` und `context.SnapshotId` beide `null`
    sind; in den anderen Fällen `ReadContextCandidates.CurrentSnapshotId` mit einem neutralen
    Platzhalter befüllen, den `ReadContextResolver` nur im Current-Zweig liest
  - [x] mit einem FastTest belegen, dass Transaction- und Snapshot-Reads ohne
    Current-Abruf funktionieren und Current-Reads unverändert bleiben
- [x] **M5.15: Freshness-Ladekosten der Search messen und nur bei Engpass begrenzen**
  - [x] `SqlSearchAbnahmeTests` um eine Messvariante mit Derived-Content-Treffern ergänzen,
    die die kompletten Kosten von `SqlRetrievalRepository.SearchAsync` einschließlich des
    Ladens aller Contents und Dependencies (Freshness-Bewertung) erfasst; Messwerte unter
    `docs/Search-Abnahme.md` dokumentieren
  - [x] nur bei belegtem Engpass die geladenen Contents/Dependencies auf die für die
    transitive Freshness der Trefferseite benötigte Teilmenge begrenzen und Vorher-/Nachher-
    Messwerte dokumentieren; ohne Engpass die Entscheidung explizit festhalten
- [x] **M5.16: Kosten des seitenweisen Diff-Blätterns messen**
  - [x] in `SqlDiffReleaseAbnahmeTests` einen Diff über die volle M5.12-Datenmenge
    (mehrere hundert Einträge) vollständig seitenweise durchblättern und dabei gelesene
    Zeilen und Laufzeit pro Seite messen, weil `HistoryService` pro Cursor-Seite beide
    Snapshots vollständig lädt und den Diff neu berechnet
  - [x] nur bei belegtem Engpass optimieren (z. B. Kategorie-Offsets im `DiffCursor`
    fortführen), sonst die Messung als V1-adequat dokumentieren; Ergebnis unter `docs/`
    festhalten
- [x] **M5.17: Direktes SQL-Seeding gegen committed Snapshots in Repo-Tests entfernen**
  - [x] `SqlRetrievalRepositoryTests` von direkten INSERTs in den committed Current
    Snapshot (`GetCurrentSnapshotIdAsync` plus `InsertNodeAsync`/`InsertRoleAsync`/
    `InsertContentAsync`) auf Seeding über offene Working Transactions und
    `commit_transaction` umstellen, wie es `TestSupport/WorkingTransactionSession` bereits
    bereitstellt
  - [x] vorhandene Seeding-Helfer wiederverwenden statt sie zu duplizieren; Suchsemantik,
    Treffer-Reihenfolge und Fehlercodes der Tests bleiben unverändert

**Abnahme:** Kein Listen-/Such-/Diff-Tool liefert unkontrolliert den Gesamtbestand;
Export ist die ausdrücklich angeforderte Ausnahme für potenziell große Ausgabe.

---

## M6: MCP-Server über STDIO

**Voraussetzung:** M4 und die jeweils veröffentlichten M5-Use-Cases.
**Statusziel:** Dünner Adapter mit stabilen Schemas und sauberem STDIO-Protokoll.

**Konzeptbezug:** [Transportgrenzen](konzept/01-Grundlagen-Hierarchie-Markdown.md),
[MCP-API und Tool-Verträge](konzept/05-MCP-API.md)

- [x] **M6.1: Hosting und Konfiguration**
  - [x] Generic Host, DI und validierte Connection-/Validator-Konfiguration
  - [x] Schema-Migration gemäß `Migrations:ApplyOnStartup` kontrolliert ausführen
  - [x] Logs ausschließlich nach `stderr` oder Datei; `stdout` ist exklusiv MCP
  - [x] Secrets und vollständige Content-Payloads nicht protokollieren
  - [x] Shutdown, Cancellation und defektes Client-Pipe-Verhalten sauber behandeln
- [x] **M6.2: Gemeinsamer Tool-Vertrag**
  - [x] JSON-Feldnamen, Nullability, Limits und Beispiele vor Handlercode festlegen
  - [x] einheitliche Success-/Error-Struktur mit `code`, `message`, `details`, `warnings`
  - [x] IDs aus Antworten ohne Umformatierung als Folgeparameter verwendbar
  - [x] `transactionId` und `snapshotId` gegenseitig ausschließen
- [x] **M6.3: Transaction- und Validation-Tools**
  - [x] `begin_transaction`, `get_transaction`, `validate_transaction`
  - [x] `commit_transaction`, `discard_transaction`
- [x] **M6.4: Navigation-, Search- und Export-Tools**
  - [x] `get_root`, `get_node`, `list_children`, `list_roles`, `search`
  - [x] `export_tree`
- [x] **M6.5: Struktur-, Content- und Rollen-Tools**
  - [x] `create_node`, `update_node`, `move_node`, `reorder_node`, `delete_node`
  - [x] `replace_content`, `replace_text`, `delete_content`
  - [x] `create_role`, `update_role`, `delete_role`, `set_role_resolution`
- [ ] **M6.6: Historien- und Release-Tools**
  - [ ] `get_snapshot`, `compare_snapshots`, `get_transaction_changes`
  - [ ] `create_release`, `list_releases`
- [ ] **M6.7: Vertrags- und STDIO-Integrationstests**
  - [ ] jedes Tool-Schema mit gültigem Minimalrequest und repräsentativen Fehlern
  - [ ] echter Serverprozess über STDIO: Initialize, Tool Call, Response, Shutdown
  - [ ] beweisen, dass Startup-/SQL-/Logging-Ausgaben `stdout` nicht verunreinigen
  - [ ] unbekannte Felder/Tools, ungültige JSON-Typen, Cancellation und Serverfehler
    liefern protokollkonforme Antworten ohne Prozessabsturz

**Abnahme:** Ein externer MCP-Client kann den vollständigen V1-Workflow ausschließlich
über STDIO durchführen; Handler enthalten nur Mapping und Delegation.

---

## M7: End-to-End-Härtung & V1-Abschluss

**Voraussetzung:** M1 bis M6.
**Statusziel:** Nachweis, dass die Einzelverträge als Gesamtsystem funktionieren.

**Konzeptbezug:** [V1-Invarianten und Gesamtmodell](konzept/06-Datenmodell-V1-Invarianten-Architektur.md),
[Referenzabläufe und Architekturentscheidungen](konzept/07-Referenzablaeufe-Entscheidungen.md)

- [ ] **M7.1: Referenzworkflow**
  - [ ] Rollen Consultant, Developer und EndUser samt Resolution Orders transaktional
    anlegen
  - [ ] Consultant-Wissen erfassen und committen
  - [ ] Developer-Content daraus ableiten/ergänzen und mehrfach versionieren
  - [ ] EndUser-Content mit Dependencies erzeugen, Source ändern und transitive
    Stale-Erkennung nachweisen
  - [ ] EndUser synchronisieren, Freshness `Current` nachweisen und Release erstellen
  - [ ] historischen Release exportieren und unverändert reproduzieren
- [ ] **M7.2: Konkurrenz- und Wiederanlauftests**
  - [ ] konkurrierende Commits verlieren keine Daten
  - [ ] Serverneustart während offener KnowHowTo-AI-Transaction lässt Daten lesbar und
    die Transaction weiter explizit commit-/discard-fähig
  - [ ] parallele Migration/Serverstarts beschädigen das Schema nicht
- [ ] **M7.3: Qualitäts- und Lastgrenzen**
  - [ ] 4-KiB-Default sowie mindestens ein abweichender konfigurierter Grenzwert im
    Tool-Workflow mit Refactoring-Hinweis nachweisen
  - [ ] große Child-Listen, Search-Treffer und Diffs bleiben gepaged
  - [ ] repräsentativen Snapshot-Copy-/Search-Umfang messen und dokumentieren; keine
    unbelegte Performanceoptimierung oder V1-Copy-on-write einführen
- [ ] **M7.4: Betriebsnachweis**
  - [ ] Konfigurationsbeispiel ohne Secrets, SQL-Berechtigungen und Startkommando
    dokumentieren
  - [ ] Release/Publish des Servers und Smoke-Test des veröffentlichten Artefakts
  - [ ] bekannte V1-Grenzen aus dem Konzeptindex und allen Pflichtmodulen gegen die
    Implementierung prüfen
- [ ] **M7.5: Abschlussgate**
  - [ ] `dotnet build KnowHowToAI.slnx` warnungsfrei
  - [ ] AiNetLinter `verify(..., scope: "solution")`: pass, Score 10.0, 0 Violations
  - [ ] vollständige FastTests grün
  - [ ] vollständige SQL-/MCP-Integrationstests grün
  - [ ] `git diff --check` und finale manuelle Diff-/Dokumentationsprüfung

**Abnahme:** Der Referenzworkflow ist ausschließlich über veröffentlichte MCP-Tools
reproduzierbar, alle Gates sind grün und kein Placeholder verbleibt.

---

## Stabiler Fehlercode-Katalog für V1

Dieser Katalog wird bei der Implementierung zentral als Code/Vertrag angelegt. Neue
Codes dürfen ergänzt, bestehende nach Veröffentlichung nicht beiläufig umbenannt werden.

- Kontext/Zustand: `InvalidReadContext`, `SnapshotNotFound`, `SnapshotNotCommitted`,
  `TransactionNotFound`, `TransactionClosed`, `SnapshotConflict`, `InvalidCursor`,
  `CursorExpired`
- Struktur/Rollen: `NodeNotFound`, `RootAlreadyExists`, `ParentNodeNotFound`,
  `InvalidHierarchy`, `NodeHasChildren`, `RoleNotFound`, `RoleInUse`,
  `RoleResolutionNotConfigured`, `InvalidRoleResolution`
- Content: `ExplicitContentNotFound`, `HeadingNotAllowed`, `FrontMatterNotAllowed`,
  `TextNotFound`, `MultipleTextMatches`, `InvalidDependency`, `DependencyCycle`
- Migration/Release: `MigrationChecksumMismatch`, `MigrationFailed`,
  `ReleaseNotFound`, `ReleaseNameConflict`

Warncodes wie `NodeTooLarge`, `PossibleEmbeddedHeading`, `TooManyChildren`,
`HierarchyTooDeep`, `LargeContentReplace` und `StaleDerivedContent` sind keine Fehler.

## Bewusst außerhalb von V1

HTTP-MCP, REST, UI/Admin-Oberfläche, Authentifizierung/ACL, Mandantenmodell innerhalb
einer Instanz, rollenabhängige Präsentationshierarchien, semantische/Vektorsuche,
automatisches Merge/Rebase, Copy-on-write, permanenter Auto-Sync, Unified-Diff als
Kernoperation, vollständiges Operation Log, Event Sourcing und harte Release Policies.

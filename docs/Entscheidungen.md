# Entscheidungen

Dieses Dokument begründet die tragenden Architekturentscheidungen und grenzt den
V1-Umfang ab. Änderungen an den Kerninvarianten aus [Invarianten](Invarianten.md)
sind bewusst als Architekturentscheidung zu behandeln.

## Architekturentscheidungen

### Markdown ist Content-Serialisierungsformat, nicht Dokumentstruktur

```text
Markdown = Content Serialization Format
```

und nicht:

```text
Markdown = Document Structure Storage
```

Die Dokumentstruktur liegt im relationalen Modell. Diese Entscheidung darf bei
späteren Arbeiten nicht aufgeweicht werden.

### Vollständige, unveränderliche Snapshots statt Event Sourcing

```text
vollständige, unveränderliche Snapshots + Working Snapshot pro Transaction
```

und nicht Event Sourcing, Git Commits, Delta Chains oder Patch Chains. Die
Entscheidung priorisiert Verständlichkeit, Sicherheit, Reproduzierbarkeit und
einfache Agenteninteraktion gegenüber maximaler Speichereffizienz. Der höhere
Speicherbedarf vollständiger Kopien ist bewusst akzeptiert; interne
Optimierungen (Copy-on-write, deduplizierte oder inkrementelle physische
Speicherung) sind später möglich, ohne die fachliche Snapshot-Semantik oder die
MCP-/Domain-API zu ändern.

### Provenienz statt permanenter Synchronisation

```text
Provenienz + ContentRevision + Stale-Erkennung
```

und nicht permanente automatische Synchronisation aller Zielgruppen. Reale
Entwicklungsarbeit ist iterativ und vorübergehend inkonsistent; das System
kontrolliert den Drift, statt ihn künstlich vollständig verhindern zu wollen
([Intention](Intention.md)).

### Deterministischer Server, entscheidender Agent

Der Server ist deterministisch und fachlich konservativ; Interpretation und
Formulierung liegen beim Agenten (Trennung im Detail:
[Intention](Intention.md)).

### Getroffene V1-Entscheidungen (ADR-Register)

| ID | Entscheidung |
|---|---|
| ADR-V1-001 | Höchstens ein persistierter Root; leerer Initialzustand erlaubt (`get_root` liefert `availability = None`) |
| ADR-V1-002 | Zielgruppenpflege transaktional über Application/MCP; keine direkten SQL-Änderungen an committed Snapshots |
| ADR-V1-003 | Transitive Freshness; Dependency-Zyklen verboten |
| ADR-V1-004 | Minimale Release- und Historienfunktionen in V1 (`get_snapshot`, `list_releases`, `compare_snapshots`, `get_transaction_changes`, `create_release`) |
| ADR-V1-005 | 4-KiB-Default als konfigurierbares Soft-Limit; Überschriften bleiben harte Fehler |
| ADR-V1-006 | App-Konfiguration statt DB-Konfiguration; harte Invarianten sind nicht abschaltbar |
| ADR-V1-007 | Dependency-Löschsemantik: Anlage-/Änderungsvalidierung (aktive explizite Source erforderlich) getrennt von Snapshot-Zustandsvalidierung; bestehende Provenienz zu später gelöschter Source bleibt erhalten und macht Derived Content transitiv `Stale` |
| ADR-V1-008 | MCP ausschließlich als stateless Streamable HTTP unter `/mcp` in derselben Server-EXE wie die Blazor-Shell (ein Kestrel-Origin); Legacy-SSE deaktiviert, kein anderer MCP-Transport und kein Fallback |

Messgestützte Performance-Entscheidungen (Suchindizes, Freshness-Ladung,
Diff-Paging) stehen mit ihren Begründungen in [Retrieval](Retrieval.md).

## Bewusst nicht in V1

Folgendes ist aktuell nicht implementiert:

- REST-API
- grafische Administration / Zielgruppen-Administrationsoberfläche
- Benutzerverwaltung, Berechtigungs-/ACL-System, Mandantenmodell innerhalb einer
  Instanz
- zielgruppenspezifische Hierarchien oder Präsentations-Views
- automatisches Merge oder Rebase konkurrierender Transactions
- Copy-on-write-Snapshots
- Vektordatenbank, zwingende Embeddings, semantische Suche
- automatische permanente Zielgruppensynchronisation, automatisches Refactoring
  während normaler Writes
- komplexes Unified-Diff-Patching, lokale Temp-Datei-Workflows
- Git als notwendiger Storage, LLM-Logik innerhalb des MCP-Servers
- harte Release Policies
- vollständiges Operation Log / Event Sourcing

## Für später architektonisch berücksichtigt

Folgende Erweiterungen sind nicht umgesetzt, dürfen durch das V1-Design aber
nicht unnötig verhindert werden:

- **Weitere Transporte**: REST API als allgemeine HTTP-Fassade; der MCP-Transport
  (Streamable HTTP) ist festgelegt.
- **Admin-Oberfläche**: Verwaltung von Zielgruppen, Resolution Orders, Releases,
  Snapshots, Transactions und Knowledge-Struktur; nicht versionierte Validator-
  und Betriebsparameter bleiben auch dann Anwendungskonfiguration.
- **Präsentations-Views**: unterschiedliche Hierarchie- oder Navigationsdarstellungen
  für Zielgruppen auf Basis derselben canonical NodeIds.
- **Semantic Search**: Embeddings, Vektorsuche, Hybrid Search.
- **Snapshot-Optimierung**: Copy-on-write, deduplizierte oder inkrementelle
  physische Speicherung ohne Änderung der fachlichen Snapshot-Semantik.
- **Merge/Rebase**: spätere Unterstützung paralleler Agentenänderungen.
- **Operation Log**: detailliertes Audit aller einzelnen Mutationen innerhalb
  einer Transaction.
- **Release Policies**: automatisierte Regeln wie „EndUser-Release darf keinen
  stale Derived Content enthalten".
- **Security**: separates Modell für Authentifizierung, Autorisierung,
  Mandantenfähigkeit und Benutzerrechte; Content-Zielgruppen bleiben davon getrennt.

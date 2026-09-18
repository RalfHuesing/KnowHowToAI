# Intention

## Zweck

KnowHowToAI ist eine **hierarchische, versionierte und rollenabhängige
Wissensbasis für Agenten und Menschen**. Das System unterstützt einen
KMU-Arbeitsalltag, in dem beispielsweise Consultant, Entwickler und Endanwender
mit demselben fachlichen Wissen arbeiten:

1. Ein Consultant erarbeitet fachliches Wissen oder ein Umsetzungskonzept.
2. Ein Entwickler verwendet dieses Wissen bei der agentischen Softwareentwicklung.
3. Während der Entwicklung entsteht zusätzlich technisches Wissen.
4. Die Implementierung ändert sich während der Entwicklung mehrfach.
5. Erst wenn ein sinnvoller Stand erreicht ist, wird daraus beispielsweise
   Endanwender-Dokumentation erzeugt oder aktualisiert.
6. Änderungen an einer Wissensquelle machen sichtbar, dass davon abgeleitete
   Dokumentation möglicherweise veraltet ist.

Das System unterstützt diese Arbeitsweise, ohne bei jeder technischen Iteration
sämtliche Dokumentationsvarianten neu generieren zu müssen.

Zentrale Ziele:

- Wissen für Agenten gezielt und token-effizient verfügbar machen.
- Wissen strukturiert statt als große monolithische Markdown-Dateien speichern.
- unterschiedliche Darstellungen desselben fachlichen Sachverhalts für verschiedene
  Rollen ermöglichen.
- unnötige Inhaltsduplikate vermeiden.
- strukturellen und inhaltlichen Drift kontrollierbar machen.
- sämtliche Änderungen versioniert und nachvollziehbar halten.
- Agenten sichere, kleine und deterministische Änderungsoperationen anbieten.
- unabhängig davon funktionieren, ob ein Agent lokalen Dateizugriff besitzt.
- als Grundlage sowohl für lokale als auch später zentrale Nutzung dienen.

## Kein Dateisystem für Markdown

KnowHowToAI ist **kein Dateisystem für Markdown-Dateien**. Markdown ist das
Inhaltsformat einzelner Wissenselemente. Struktur, Versionierung, Rollen und
Beziehungen liegen im relationalen Datenmodell
([Wissenshierarchie](Wissenshierarchie.md), [Datenmodell](Datenmodell.md)).
Alle Lese- und Schreibvorgänge laufen direkt über die
[MCP-API](McpApi.md); es gibt keinen Workflow über lokale temporäre Dateien.

## Rollenverteilung zwischen Agent und Server

Der MCP-Server ist deterministisch und fachlich konservativ. Diese Trennung ist
beabsichtigt:

Der **Agent** entscheidet:

- wie Wissen formuliert wird,
- welcher Node sinnvoll ist und ob neue Nodes benötigt werden,
- wie ein zu großer Node aufgeteilt wird,
- wie Consultant-Wissen technisch umgesetzt wird,
- wie Endanwender-Dokumentation formuliert wird.

Der **Server** entscheidet:

- ob die Transaction gültig ist,
- welcher Snapshot gelesen wird,
- ob die Hierarchie gültig ist,
- ob Markdown-Headings verboten sind,
- ob eine Rollenauflösung gültig ist,
- ob Dependencies gültig sind,
- ob ein Commit kollidiert,
- wie Daten persistiert und versioniert werden.

Der Server enthält keine komplexe LLM-Logik. Er stellt sichere Primitive,
Versionierung, Validierung und Persistenz bereit; Interpretieren, Umformulieren,
Aufteilen und Zusammenführen übernimmt der Agent.

## Drift-Prinzip

Das System verhindert Drift zwischen Rolleninhalten während laufender Arbeit nicht
vollständig. Stattdessen gilt:

> Drift darf temporär entstehen, aber er darf nicht unbemerkt bleiben.

Das System erkennt jederzeit, dass abgeleiteter Content nicht mehr auf den
aktuellen Quellen basiert
([Rollen und Content](Rollen-und-Content.md)). Das ist wichtiger als permanente
automatische Synchronisation.

## Dokumentationssynchronisation ist ein eigener Workflow

Endanwender-Dokumentation wird nicht bei jedem Entwicklungsschritt automatisch
mitgeschrieben. Der unterstützte Ablauf ist:

```text
Consultant-Wissen
        ↓
Entwicklung
        ↓
mehrere technische Iterationen
        ↓
stabiler Implementierungsstand
        ↓
Endanwender-Dokumentation aktualisieren
```

Ein Synchronisations-Agent erhält zu einem selbstgewählten Zeitpunkt die
Informationen über stale abgeleitete Inhalte, neue relevante Nodes, fehlende
Inhalte und aktuelle Quellinhalte, und aktualisiert die abgeleitete Dokumentation
gezielt in einer eigenen Transaction mit neuen Source-Revisions
(Provenienz: [Rollen und Content](Rollen-und-Content.md)).

## Typischer Arbeitsablauf

1. **Consultant**: fachliches Konzept erstellen, Wissen aktualisieren, Transaction
   committen.
2. **Entwicklung**: Consultant-Wissen lesen, implementieren, Developer-Wissen
   ergänzen; Inhalte mehrfach verändern; mehrere Transactions und Snapshots sind
   möglich. Abhängige Rollen-Inhalte werden dabei stale, aber nicht automatisch
   geändert.
3. **Dokumentationssynchronisation**: zu einem späteren, bewussten Zeitpunkt
   werden stale Inhalte aktualisiert.

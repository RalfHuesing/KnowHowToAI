# End-to-End-Härtung & V1-Abschluss (M7)

**Voraussetzung:** M1 bis M6.
**Statusziel:** Nachweis, dass die Einzelverträge als Gesamtsystem funktionieren.

**Doku-Bezug:** [Datenmodell](../../docs/Datenmodell.md), [Invarianten](../../docs/Invarianten.md),
[Entscheidungen](../../docs/Entscheidungen.md)

## Aufgaben

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

# Orchestrator-Prompt: Audience-Hard-Cut vollständig umsetzen

```text
Setze die vollständige Roadmap unter

tasks/audience-hard-cut/roadmap/01-repositoryweiter-audience-hard-cut/

bis einschließlich Abschlussaudit um.

Du bist der Hauptorchestrator. Du verantwortest Reihenfolge, Scopekontrolle,
Integration, Reviews, Git-Historie und Abschlusskommunikation. Implementiere
substantielle Repository-Arbeit grundsätzlich nicht selbst, sondern delegiere
sie an Subagenten mit:

- Modell: gpt-5.6-luna
- Reasoning: high

## Verbindliche Grundlagen

Lies vor Arbeitsbeginn vollständig:

- AGENTS.md
- tasks/README.md
- tasks/audience-hard-cut/README.md
- tasks/audience-hard-cut/roadmap/01-repositoryweiter-audience-hard-cut/planning.md
- tasks/audience-hard-cut/roadmap/01-repositoryweiter-audience-hard-cut/roadmap.md
- tasks/audience-hard-cut/roadmap/01-repositoryweiter-audience-hard-cut/audit.md
- die durch AGENTS.md und den jeweiligen Leaf-Task vorgeschriebenen Regeln,
  Konzepte und Ist-Dokumente

Der Begriffsvertrag in planning.md ist entschieden. Öffne keine neuen Produkt-
oder Architekturentscheidungen, solange kein echter Widerspruch zum aktuellen
Repositoryzustand belegt ist.

Die Umsetzung ist ein Greenfield-Hard-Cut:

- technisch ausschließlich Audience
- sichtbare deutsche UI ausschließlich „Zielgruppe“
- keine Legacy-Aliasse, Redirects, Doppelverträge, Compatibility Views,
  alten Browserzustandsleser oder parallelen MCP-/JSON-Verträge
- keine Forward-Migration; die SQL-Greenfield-Baseline wird direkt korrigiert
- keine Datenbankobjekte löschen: Der Benutzer hat sämtliche Tabellen in allen
  betroffenen KnowHowToAI-Entwicklungs- und Testdatenbanken bereits manuell
  gelöscht
- keine Datenbanknamen raten und niemals Credentials ausgeben

Die neuere Benutzerangabe zum bereits manuell hergestellten leeren
Datenbankzustand hat Vorrang vor abweichenden Reset-Aussagen in planning.md,
roadmap.md, audit.md oder M1.1-T2. Der Agent korrigiert diese Planungsstellen im
Rahmen von M1.1-T2, führt aber selbst keinen Reset aus.

## Ausführungsreihenfolge

Bearbeite die Leaf-Tasks strikt in dieser Reihenfolge:

1. M1.1-T1
2. M1.1-T2
3. M1.2-T1
4. M1.3-T1
5. M1.3-T2
6. M1.4-T1
7. M1.5-T1
8. erst danach den manuellen Abschlussaudit aus audit.md

Überspringe keinen offenen Leaf-Task. Beginne den nächsten Leaf erst, wenn der
vorherige vollständig umgesetzt, geprüft, dokumentiert, in roadmap.md abgehakt
und committed ist.

## Delegation je Leaf-Task

Verwende genau einen schreibenden Luna-High-Subagenten pro Leaf-Task. Keine
parallelen Schreibagenten im gemeinsamen Worktree. Builds, Tests,
Datenbankmigrationen und Commits laufen seriell.

Der Auftrag an jeden Subagenten muss mindestens enthalten:

- exakte Leaf-ID und Pfad der Leaf-Datei
- Ziel und ausdrücklich erlaubter Scope
- Nicht-Ziele und Hard-Cut-Grenzen
- Pflichtlektüre aus Leaf, AGENTS.md und docs/README.md
- Auftrag, vor Änderungen `git status` zu prüfen und fremde Änderungen zu erhalten
- Auftrag, den aktuellen Codezustand zu untersuchen statt blind Plantext umzusetzen
- Auftrag, AiNetLinter MCP proaktiv zu verwenden
- verlangtes Testbudget und Abschlussgates des Leafs
- Pflicht, Ist-Dokumentation im selben Slice zu aktualisieren
- Pflicht, Leaf- und Parent-Checkboxen erst nach belegter Abnahme zu schließen
- Pflicht, einen kleinen atomaren Commit mit deutscher Conventional-Commit-
  Botschaft zu erstellen
- erwartete Rückgabe:
  - Commit-Hash
  - geänderte/verschobene Dateien
  - ausgeführte AiNetLinter-Abfragen
  - Build-/Testnachweise
  - Roadmap-Aktualisierung
  - Abweichungen oder Blockaden

Warte nach jeder Delegation auf den Subagenten. Starte währenddessen keinen
weiteren schreibenden Agenten.

## Proaktiver AiNetLinter-MCP-Workflow

Prüfe zu Beginn, ob der für dieses Projekt konfigurierte AiNetLinter-MCP-Server
erreichbar ist. Bei jedem C#-Leaf muss der ausführende Subagent ihn aktiv
verwenden, nicht nur am Ende.

Vor Änderungen:

- relevante Symbole mit `find_symbol` ermitteln
- `get_feature_context` für die zentrale Verantwortung verwenden
- kanonische Handoff-IDs übernehmen
- Aufrufer und Auswirkungen mit `find_references`, `get_impact`,
  `get_call_tree`, `find_implementations` oder `get_type_hierarchy` prüfen
- vorhandene Implementierungen und Tests vor neuen Typen/Abstraktionen suchen

Während und nach Änderungen:

- nach jedem kohärenten C#-Slice `verify(targetPath)` ausführen
- Verstöße ursächlich beheben; keine Suppressions, Schwellenwertänderungen oder
  kosmetischen Umgehungen
- beim Gesamtabschluss ausschließlich
  `verify(targetPath, scope: "solution")`
- das Abschlussgate gilt nur bei:
  - verdict=pass
  - score=10.0
  - violationCount=0

Textsuche mit `rg` bleibt für SQL, Markdown, JSON, Razor, CSS, Strings,
Dateinamen und die Terminologieinventur erforderlich. Sie ersetzt bei
C#-Semantik nicht den AiNetLinter-MCP-Workflow.

Ist AiNetLinter nicht verfügbar, versuche die konfigurierte Verbindung einmal
gezielt zu diagnostizieren. Kann sie nicht hergestellt werden, stoppe vor der
C#-Änderung und melde die konkrete Blockade. Arbeite nicht still mit einer
reinen Textsuche weiter.

## Prüfung und Commit nach jedem Leaf

Nach Rückgabe eines Subagenten prüfst du als Orchestrator risikobasiert:

- `git status`
- den vollständigen Commit-Diff
- Einhaltung von Scope und Nicht-Zielen
- korrekte Datei- und Namespaceverschiebungen
- fehlende Altverträge oder versehentliche Kompatibilitätsbrücken
- aktualisierte Ist-Dokumentation
- korrekte Leaf- und Parent-Checkboxen
- tatsächlich ausgeführte vorgeschriebene Gates
- sauberen atomaren Commit

Akzeptiere keinen Leaf mit roten, fehlenden oder nur behaupteten Nachweisen.

Wenn vor dem nächsten Leaf ein konkreter Fehler auffällt, sende dem zuständigen
Luna-High-Agenten einen präzisen Folgeauftrag. Der Agent behebt ausschließlich
diesen Befund, wiederholt die betroffenen Gates und erstellt einen eigenen
atomaren Korrekturcommit. Fahre erst danach fort.

Nicht amendieren, nicht rebasen, keine Historie umschreiben und nicht pushen.

## Builds und Tests

Befolge exakt die Testbudgets der Leaf-Dateien und die Repositoryregeln.

Insbesondere:

- Builds nur über:
  `pwsh -NoProfile -File scripts/build.ps1`
- FastTests über:
  `pwsh -NoProfile -File scripts/test-fast.ps1`
- IntegrationTests gemäß Leaf und Abschlussbudget über:
  `pwsh -NoProfile -File scripts/test-integration.ps1`
- manueller SQL-Lauf, wenn vom Leaf gefordert:
  `pwsh -NoProfile -File scripts/test-integration.ps1 -Filter Category=ManualDatabaseIntegration`
- keine parallelen Builds oder Tests
- keine festen Sleeps in BrowserTests
- Chrome Stable ausschließlich headless
- fehlgeschlagene eigene Gates vor dem Fortfahren beheben und vollständig
  wiederholen
- belegbar fremde Fehler nicht ungefragt reparieren; klar abgrenzen und bei
  echter Blockade stoppen

## Datenbank-Ausgangszustand

Neuere Benutzeranweisung, die abweichende Aussagen der Roadmap ersetzt:

Der Benutzer hat bereits sämtliche Tabellen in allen betroffenen
KnowHowToAI-Entwicklungs- und Testdatenbanken manuell gelöscht.

Deshalb gilt:

- Kein Subagent darf Tabellen, Schemas, Datenbanken oder andere Datenbankobjekte
  löschen.
- Kein Reset-, Drop-, Cleanup- oder Bereinigungsschritt wird ausgeführt.
- Insbesondere niemals `DROP DATABASE`, `DROP TABLE`, `CREATE DATABASE` oder
  einen destruktiven Test-Harness-Aufruf verwenden.
- M1.1-T2 behandelt die Datenbanken als bereits manuell geleerte
  Greenfield-Ziele.
- Vor dem Neuaufbau ausschließlich read-only prüfen:
  - die drei konfigurierten Connection-Sektionen sind erreichbar;
  - Server- und Datenbanknamen werden ohne Credentials dokumentiert;
  - identische Server-/Datenbankkombinationen werden dedupliziert;
  - es existieren keine `KnowHowToAI_`-Tabellen mehr;
  - SQL-Server-Systemdatenbanken sind nicht betroffen.
- Falls entgegen der Benutzerangabe noch eine `KnowHowToAI_`-Tabelle oder ein
  unerwarteter Schema-/Journalrest existiert: nichts löschen, sondern stoppen
  und den exakten Server-/Datenbanknamen sowie die gefundenen Objekte melden.
- Sind die Ziele leer, die direkt korrigierte Audience-Greenfield-Baseline über
  den normalen Migration Runner anwenden und den `Default`-Seed prüfen.
- Es entsteht keine Forward-Migration und kein Datenübernahmepfad.

Der ausführende Agent aktualisiert in M1.1-T2 außerdem `planning.md`,
`roadmap.md`, `audit.md` und die Leaf-Datei M1.1-T2 so, dass sie den tatsächlich
bereits manuell hergestellten leeren Datenbankzustand beschreiben und keinen
auszuführenden Reset mehr verlangen. Diese Dokumentationskorrektur gehört in
denselben atomaren Commit wie M1.1-T2.

## Terminologie-Abschlussgate

M1.5-T1 muss Text und tracked Dateipfade breit prüfen.

Zulässige Resttreffer sind ausschließlich:

1. standardisierte WAI-ARIA-Attribute/APIs und Playwright-Locators wie
   `role="status"`, `GetByRole` und `AriaRole`;
2. der nachvollziehbare Entscheidungs- und Ausführungsplan unter
   `tasks/audience-hard-cut/`.

Jeder verbleibende Treffer außerhalb dieser Kategorien ist ein Fehler.
Pauschale Ordnerausnahmen sind verboten. Jeder erlaubte Resttreffer wird mit
Pfad, Zeile und Kategorie dokumentiert.

Accessibility-Semantik darf nicht entfernt oder durch fragile Selektoren
ersetzt werden, nur um weniger Texttreffer zu erhalten.

## Abschlussaudit – ausschließlich ganz am Ende

Starte den Audit erst, wenn:

- M1.1 bis M1.5 vollständig geschlossen sind;
- jeder Leaf mindestens einen zugeordneten atomaren Commit besitzt;
- alle Abschlussgates von M1.5 grün sind;
- der Working Tree sauber ist.

Spawne dann drei parallele, ausschließlich lesende Luna-High-Auditoren:

### Auditor A – Architektur und Verträge

Prüft:

- Begriffsvertrag über Core, Storage, MCP und Web
- Hard Cut und Abwesenheit von Alias-/Kompatibilitätspfaden
- Schichtengrenzen und öffentliche Verträge
- Fehlercodes, IDs, JSON, URLs, Queryparameter und Browserzustand
- Übereinstimmung von Code und Ist-Dokumentation

### Auditor B – SQL, Tests und Betriebssicherheit

Prüft:

- reine Audience-Greenfield-Baseline
- fehlende Forward-/Legacy-Migrationen
- ausschließlich read-only bestätigten leeren Datenbank-Ausgangszustand
- Abwesenheit agentisch ausgeführter Reset-/Drop-/Cleanup-Schritte
- Schema-, Snapshot-, Transaction- und Seed-Invarianten
- Vollständigkeit und Aussagekraft der Build-, Linter- und Testnachweise
- erkennbare Testlücken oder abgeschwächte Assertions

### Auditor C – Web, Accessibility und Planungsbestand

Prüft:

- Zielgruppen-Wording, Route, Query, Storage-Key und UI-Zustände
- WAI-ARIA, Fokus, Tastatur, Reconnect und Dirty-State
- Browser-Seeds und Playwright-Locators
- Dateinamen, Test-IDs, CSS-Klassen, Doku, Regeln und fremde Roadmaps
- Markdownlinks und Terminologie-Resttreffer

Alle Auditoren liefern ausschließlich konkrete Findings mit:

- Priorität
- Datei und Zeile
- verletztem Vertrag oder Akzeptanzkriterium
- reproduzierbarem Nachweis
- kleinstmöglicher Korrekturgrenze

Sie dürfen keine Dateien ändern und keine neuen Produktanforderungen erfinden.
Warte auf alle drei Auditoren und konsolidiere/dedupliziere ihre Findings.

## Findings beheben

Wenn der Abschlussaudit Findings liefert:

- delegiere jede unabhängige Finding-Gruppe an einen Luna-High-Subagenten;
- verwende bei überlappenden Dateien ausschließlich serielle Schreibagenten;
- gib jedem Agenten eine konkrete, begrenzte Korrekturaufgabe mit Dateien,
  Akzeptanzkriterien und Testbudget;
- verlange pro Korrekturaufgabe einen atomaren deutschen Conventional Commit;
- lasse betroffene Gates erneut ausführen;
- lasse anschließend die zuständigen Auditoren die Korrekturen erneut
  ausschließlich lesend prüfen.

Es gibt höchstens eine gezielte Audit-Korrekturrunde. Falls danach noch ein
wesentlicher Befund besteht oder eine neue Produkt-/Architekturentscheidung
notwendig wäre, stoppe und berichte die Blockade, statt den Scope auszuweiten.

Wenn keine Findings verbleiben, delegiere die reine Auditdokumentation an einen
letzten Luna-High-Agenten:

- `audit.md` mit geprüften Commits, Nachweisen und Ergebnis aktualisieren;
- Audit-Checkbox und M1-Abschluss in `roadmap.md` schließen;
- keine Produktionsänderung vornehmen;
- `git diff --check` und Linkprüfung ausführen;
- atomaren Commit für den bestandenen Audit erstellen.

## Abschlussbericht

Beende erst nach dem dokumentierten Audit. Berichte kompakt:

- umgesetzte Leaf-Tasks
- Commit-Hashes je Leaf und Korrektur
- Audit-Commit
- Ergebnis der Terminologiesuche
- read-only bestätigter leerer Datenbank-Ausgangszustand und anschließend neu
  aufgebautes Audience-Schema
- Build-, AiNetLinter-, FastTest-, Integration- und Browsernachweise
- Auditfindings und deren Behebung
- verbleibende Abweichungen oder „keine“
- finalen `git status`

Nicht pushen. Keine weitere Roadmap automatisch starten.
```

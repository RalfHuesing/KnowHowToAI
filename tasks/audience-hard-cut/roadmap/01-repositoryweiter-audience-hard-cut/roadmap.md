# M1 – Repositoryweiter Audience-Hard-Cut

[Vorhaben-Index](../../README.md) · [Begriffsvertrag](planning.md)

- [ ] **M1 abschließen**

## Ziel

Das inhaltliche Adressatenmodell heißt in allen technischen Schichten
`Audience` und in der sichtbaren deutschen Oberfläche „Zielgruppe“. SQL,
Domain, Application, MCP, Web, Tests, Ist-Dokumentation, Regeln und aktive
Planungsartefakte bilden denselben Vertrag ab. Es existiert kein Legacy-Alias
und keine parallele alte Schnittstelle; fachliches Verhalten und Daten bleiben
unverändert.

## Abhängigkeiten und Ausführungsfreigabe

- Voraussetzungen: aktueller grüner Hauptstand; keine parallele Arbeit an den
  betroffenen Core-, SQL-, MCP-, Web-, Test-, Doku- oder Taskpfaden.
- Freigabe: [M1.0](planning.md) ist abgeschlossen; M1.1–M1.5 sind zur strikt
  sequenziellen Agentenausführung freigegeben.
- Ausführungsgrenze: genau ein Leaf-Task je Agent und Commit; der Benutzer
  startet die Umsetzung manuell.

## Verbindliche Leitplanken

- Der [Begriffsvertrag](planning.md#verbindlicher-begriffsvertrag) ist vollständig
  und darf im Implementierungsslice nicht neu entschieden werden.
- Kein Alias, keine Weiterleitung und keine Doppelunterstützung alter und neuer
  Namen. Compilerbedingte direkte Aufruferkorrekturen sind keine zweite API.
- `Audience` bleibt Contentadressat und wird nicht mit Berechtigungen verbunden.
- Der Benutzer hat die bekannten konfigurierten Entwicklungs-/Testdatenbanken
  bereits manuell geleert. Die SQL-Baseline wird direkt auf Audience korrigiert;
  kein Agent löscht Tabellen, Schemas, Datenbanken oder sonstige DB-Objekte.
  Vor dem Neuaufbau erfolgt ausschließlich eine read-only Leerstandsprüfung;
  danach läuft der normale Migration Runner.
- Jeder Code-Slice aktualisiert seine Ist-Dokumentation im selben Commit.
- Accessibility-`role` und Playwrights rollenbasierte Locators bleiben erhalten;
  sie sind keine Fachterminologie.

## M1.1 – Fachkern und Persistenz

- [x] **M1.1 abschließen**
  - [x] [M1.1-T1 – Domain und Application auf Audience umstellen](tasks/M1.1-T1.md)
  - [x] [M1.1-T2 – SQL-Schema und Persistence auf Audience umstellen](tasks/M1.1-T2.md)

## M1.2 – MCP-Hard-Cut

- [x] **M1.2 abschließen**
  - [x] [M1.2-T1 – MCP-Tools und Transportverträge auf Audience umstellen](tasks/M1.2-T1.md)

## M1.3 – Web-Hard-Cut

- [x] **M1.3 abschließen**
  - [x] [M1.3-T1 – Webzustand, Route und Zielgruppenoberfläche umstellen](tasks/M1.3-T1.md)
  - [x] [M1.3-T2 – Browser-Seeds und Ende-zu-Ende-Abläufe umstellen](tasks/M1.3-T2.md)

## M1.4 – Regeln und Planungsbestand

- [ ] **M1.4 abschließen**
  - [ ] [M1.4-T1 – Regeln und ausführbare Planungsartefakte konsolidieren](tasks/M1.4-T1.md)

## M1.5 – Repositoryabschluss

- [ ] **M1.5 abschließen**
  - [ ] [M1.5-T1 – Restterminologie beseitigen und Gesamtnachweis führen](tasks/M1.5-T1.md)

## Milestone-Abnahme

- [ ] Neu aufgebaute Entwicklungs-/Testschemas, C#-Fachmodell und sämtliche
      fachlichen Bezeichner verwenden `Audience`; Snapshot-Semantik und Verhalten
      sind erhalten.
- [ ] Ausschließlich die neuen MCP-Tools und JSON-Felder sind registriert; alte
      Toolnamen und Felder sind nicht mehr verfügbar.
- [ ] Ausschließlich `/audiences`, `audienceId` und der neue Browserzustand werden
      verwendet; die deutsche UI spricht durchgängig von „Zielgruppe“.
- [ ] Ist-Dokumentation, Regeln und alle weiterhin ausführbaren Roadmaps führen
      keinen Agenten zurück zur alten Terminologie.
- [ ] Die Terminologiesuche enthält nur die in M1.0 einzeln erlaubten Standards-,
      Migrations- und Entscheidungsnachweise; keine pauschale Ausnahme bleibt.
- [ ] Build, Solution-Linter, FastTests und IntegrationTests einschließlich
      BrowserTests sind gemäß Abschlussbudget grün.

## Audit

- [ ] [Begrenzten Abschlussaudit](audit.md) gegen Ziel, Begriffsvertrag,
      Invarianten und Nachweise durchführen
- Ergebnis: offen

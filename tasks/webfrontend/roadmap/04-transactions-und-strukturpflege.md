# M4 – Transactions und Strukturpflege

[Roadmap-Index](../Roadmap.md)

- [ ] **M4 abschließen**

Abhängigkeit: [M3](03-read-only-wissenscockpit.md)

Ziel: Benutzer können Working Transactions sicher führen und die Node-Struktur visuell ändern.

Referenzen: [Transaction-Arbeitsbereich](../konzept/02-bedienkonzept-und-ui.md#transaction-arbeitsbereich), [Wissensbaum](../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [DI-Grenzen](../konzept/05-architektur-api-und-mcp.md#di--und-zustandsgrenzen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M4.1 – Transaction-Arbeitskontext

- [ ] **M4.1 abschließen**

  - [ ] **M4.1-T1 – Transaction beginnen, auflisten und fortsetzen**
    - Voraussetzung: O-007 zum Transaction-`Actor` ohne Auth ist durch den Benutzer entschieden.
    - Umfang: Beginnen mit Optionen, alle offenen Transactions auflisten, explizit auswählen und als Working Read Context öffnen. Ohne Auth existiert kein belastbares „eigene Transactions“.
    - UI-Regel: pro Browserarbeitskontext genau eine aktive Transaction; Wechsel ist bewusst und sichtbar.
    - Tests: neue/vorhandene Transaction, leere Liste, ungültige ID, Refresh und Reconnect.
    - Abnahme: Working Context bleibt nach Navigation rekonstruierbar.

  - [ ] **M4.1-T2 – Transaction-State und Navigationsschutz implementieren**
    - Umfang: globaler Status, Base Snapshot, Änderungsindikator und Schutz bei Kontextwechsel, Tabnavigation oder Circuit-Verlust.
    - Abgrenzung: bereits persistierte Änderungen gehen nicht verloren; rein flüchtiger Formzustand wird separat behandelt.
    - Tests: Refresh, Browsernavigation, Reconnect und Prozessneustart.
    - Abnahme: Benutzer weiß jederzeit, in welcher Transaction Änderungen landen.

## M4.2 – Validierung und Abschluss

- [ ] **M4.2 abschließen**

  - [ ] **M4.2-T1 – Transaction validieren und Findings darstellen**
    - Umfang: serverseitige Validierung auslösen, Fehler/Warnungen gruppieren, zu betroffenen Nodes navigieren und veraltete Ergebnisse kennzeichnen.
    - Tests: valide, Fehler, Warnungen, gemischte Findings und Änderung nach Validierung.
    - Abnahme: Findings sind vor Commit verständlich und handlungsorientiert.

  - [ ] **M4.2-T2 – Transaction-Diff vor Commit darstellen**
    - Umfang: strukturierte Änderungen der Working Transaction gegenüber ihrer Base anzeigen.
    - Prüfen: Erstellen, Ändern, Verschieben, Löschen und leere Transaction; große Diffs paginieren.
    - Abnahme: Benutzer kann den geplanten Commit vollständig prüfen.

  - [ ] **M4.2-T3 – Commit und Discard implementieren**
    - Umfang: bestätigter Commit, bestätigtes Verwerfen, Erfolgs-/Fehlerzustand und Wechsel auf resultierenden Current Snapshot.
    - Schutz: Doppelaktion, stale `ChangeVersion` und ungültiger Status.
    - Tests: Erfolg, Validierungsfehler, Konflikt, Wiederholung und Discard.
    - Abnahme: Transaction-Lebenszyklus ist vollständig ohne MCP bedienbar.

  - [ ] **M4.2-T4 – Release aus committed Snapshot anlegen**
    - Umfang: Release-Name und optionale Beschreibung erfassen, einen committed Snapshot explizit auswählen und `ReleaseService.CreateReleaseAsync` aufrufen.
    - Regeln: keine Transaction erforderlich; Working oder nicht vorhandene Snapshots bleiben unzulässig; Warnungen blockieren das Release nicht.
    - Tests: Erfolg, Namenskonflikt, fehlender/uncommitted Snapshot, Findingsanzeige und Navigation zum erzeugten Release.
    - Abnahme: der bestehende Release-Use-Case ist vollständig ohne MCP nutzbar.

## M4.3 – Node-Pflege

- [ ] **M4.3 abschließen**

  - [ ] **M4.3-T1 – Nodes erstellen und Stammdaten bearbeiten**
    - Umfang: Node unter gewähltem Parent erstellen sowie Titel und Description bearbeiten.
    - Regeln: Änderungen nur in aktiver Transaction; serverseitige Normalisierung/Validierung bleibt maßgeblich.
    - Tests: gültige Werte, Duplikate, Grenzlängen, ungültiger Parent und gleichzeitige Aktualisierung.
    - Abnahme: Ergebnis ist unmittelbar im Working Tree sichtbar.

  - [ ] **M4.3-T2 – Nodes kontrolliert löschen**
    - Umfang: Löschaktion, Auswirkungsübersicht, Bestätigung und serverseitige Fehlerdarstellung.
    - Prüfen: Teilbaum, Referenzen/Dependencies, bereits gelöschter Node und Rootschutz gemäß Ist-Regeln.
    - Tests: Erfolgs- und Ablehnungsfälle sowie Diffdarstellung.
    - Abnahme: keine Löschung erfolgt ohne sichtbare Ziel- und Auswirkungsprüfung.

  - [ ] **M4.3-T3 – Nodes per Drag-and-drop verschieben und sortieren**
    - Umfang: Move/Reorder im Tree mit Zielvorschau, Einfügeposition und anschließender Working-Tree-Aktualisierung.
    - Regeln: UI optimiert nur die Interaktion; serverseitige Hierarchievalidierung entscheidet.
    - Prüfen: Zyklus, ungültiges Ziel, Root, Paging-Grenze, gleiches Ziel und Tastaturalternative.
    - Tests: Komponenten-, Application- und Browserfälle.
    - Abnahme: komplexe Strukturänderungen sind präzise und nachvollziehbar möglich.

## M4.4 – Konflikte

- [ ] **M4.4 abschließen**

  - [ ] **M4.4-T1 – `SnapshotConflict` verständlich behandeln**
    - Umfang: Base und Current vergleichen, Konflikt erklären und geführtes manuelles Reapply in eine neue Transaction ermöglichen.
    - Nicht enthalten: automatisches Merge oder Rebase.
    - Tests: paralleler UI-/MCP-Commit, konfliktfreie neue Transaction und verworfener Reapply.
    - Abnahme: Konflikt führt weder zu Datenverlust noch zu einem impliziten Merge.

## Milestone-Abnahme

- Transaction-Lebenszyklus und vollständige Node-Strukturpflege funktionieren ohne Agent.
- Jede Mutation verwendet eine explizite Transaction und erscheint in Validierung sowie Diff.
- Navigation, Reconnect, Neustart und parallele MCP-Änderungen sind sicher behandelt.

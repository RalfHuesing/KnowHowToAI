# M4 – Transactions und Strukturpflege

[Roadmap-Index](../Roadmap.md)

- [ ] **M4 abschließen**

Abhängigkeit: [M3](03-read-only-wissenscockpit.md)

Verbindliche M0-Basis: Strukturpflege erweitert denselben nativen, 100er-cursorpaginierten Knowledge Tree aus M3. Es wird keine Tree-Komponente gesucht oder ersetzt. `Parent`, `Before` und `After` sind die einzigen Move-Zielpositionen und müssen per Drag-and-drop sowie über fokussierbare Aktionsbuttons denselben Mutationseinstieg verwenden. Komponenten- und Browsernachweise bleiben bei bUnit `2.11.3`/xUnit v3 `3.2.2` beziehungsweise Microsoft.Playwright .NET `1.62.0` mit Chrome Stable `152.0.7977.83`, `Channel = "chrome"`, `Headless = true`.

Ziel: Benutzer können Working Transactions sicher führen und die Node-Struktur visuell ändern.

Referenzen: [Transaction-Arbeitsbereich](../konzept/02-bedienkonzept-und-ui.md#transaction-arbeitsbereich), [Wissensbaum](../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [DI-Grenzen](../konzept/05-architektur-api-und-mcp.md#di--und-zustandsgrenzen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M4.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M4.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M3; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Transaction-Actor (O-007), Zusammenarbeit mehrerer Clients (O-025), Lebensdauer offener Transactions (O-026), Undo-Grenzen (O-027) und konkrete Sicherheitsdialoge für Strukturmutationen.
  - Prüfen: reale Lese-UX aus M3, bestehende Mutationsverträge, `ChangeVersion`, Konfliktverhalten sowie Paging-/Cachegrenzen des nativen Trees gegen die bisherigen Entwurfstasks. Tree-Variante und Paginggröße sind keine offenen Entscheidungen.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M4-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M4.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M4.1 – Transaction-Arbeitskontext

- [ ] **M4.1 abschließen**

  - [ ] **M4.1-T1 – Transaction beginnen, auflisten und fortsetzen**
    - Voraussetzung: O-007 zum Transaction-`Actor`, O-025 zur Zusammenarbeit in derselben Transaction und O-026 zur Transaction-Lebensdauer sind durch den Benutzer entschieden.
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
    - Voraussetzung: O-025 zur Zusammenarbeit in derselben Transaction und O-027 zum Undo-Umfang sind durch den Benutzer entschieden.
    - Umfang: Node unter gewähltem Parent erstellen sowie Titel und Description bearbeiten.
    - Regeln: Änderungen nur in aktiver Transaction; serverseitige Normalisierung/Validierung bleibt maßgeblich.
    - Tests: gültige Werte, Duplikate, Grenzlängen, ungültiger Parent und gleichzeitige Aktualisierung.
    - Abnahme: Ergebnis ist unmittelbar im Working Tree sichtbar.

  - [ ] **M4.3-T2 – Nodes kontrolliert löschen**
    - Voraussetzung: O-025 zur Zusammenarbeit in derselben Transaction und O-027 zum Undo-Umfang sind durch den Benutzer entschieden.
    - Umfang: Löschaktion, Auswirkungsübersicht, Bestätigung und serverseitige Fehlerdarstellung.
    - Prüfen: Teilbaum, Referenzen/Dependencies, bereits gelöschter Node und Rootschutz gemäß Ist-Regeln.
    - Tests: Erfolgs- und Ablehnungsfälle sowie Diffdarstellung.
    - Abnahme: keine Löschung erfolgt ohne sichtbare Ziel- und Auswirkungsprüfung.

  - [ ] **M4.3-T3 – Nodes per Drag-and-drop verschieben und sortieren**
    - Voraussetzung: O-025 zur Zusammenarbeit in derselben Transaction und O-027 zum Undo-Umfang sind durch den Benutzer entschieden.
    - Umfang: Move/Reorder im nativen Tree mit den Zielpositionen `Parent`, `Before` und `After`, sichtbarer Zielvorschau und anschließender Working-Tree-Aktualisierung. Native HTML-Drag-Ereignisse und die drei fokussierbaren Aktionsbuttons rufen denselben UI-Movevertrag auf.
    - Regeln: UI optimiert nur die Interaktion; serverseitige Hierarchievalidierung entscheidet.
    - Prüfen: Zyklus, ungültiges Ziel, Root, Source/Target auf verschiedenen 100er-Seiten, Cache-Eviction während der Auswahl, gleiches Ziel und vollständige Tastaturalternative.
    - Tests: bUnit-Komponentenfälle für alle drei Zielpositionen und Serverablehnung; Application-Tests für fachliche Varianten; je ein headless Playwright-Ablauf per Drag-and-drop und Aktionsbuttons. Beide Wege müssen identische Mutationseingaben und dieselbe bestätigte Serveraktualisierung erzeugen.
    - Abnahme: alle drei Zielpositionen sind per Maus und Tastatur präzise ausführbar; bei Ablehnung wird die betroffene Seite aus dem Serverzustand neu geladen und kein optimistischer Phantomzustand bleibt sichtbar.

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

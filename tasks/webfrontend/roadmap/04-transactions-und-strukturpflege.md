# M4 – Transactions und Strukturpflege

[Roadmap-Index](../Roadmap.md)

- [ ] **M4 abschließen**

Abhängigkeit: [M3](03-read-only-wissenscockpit.md)

Verbindliche M0-Basis: Strukturpflege erweitert denselben nativen, 100er-cursorpaginierten Knowledge Tree aus M3. Es wird keine Tree-Komponente gesucht oder ersetzt. `Parent`, `Before` und `After` sind die einzigen Move-Zielpositionen und müssen per Drag-and-drop sowie über fokussierbare Aktionsbuttons denselben Mutationseinstieg verwenden. Komponenten- und Browsernachweise verwenden bUnit mit xUnit v3 beziehungsweise Microsoft.Playwright .NET mit der installierten aktuellen Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Benutzer können Working Transactions sicher führen und die Node-Struktur visuell ändern.

Referenzen: [Transaction-Arbeitsbereich](../konzept/02-bedienkonzept-und-ui.md#transaction-arbeitsbereich), [Wissensbaum](../konzept/02-bedienkonzept-und-ui.md#wissensbaum), [DI-Grenzen](../konzept/05-architektur-api-und-mcp.md#di--und-zustandsgrenzen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M4.0 – Manuelle Planung und Konzeptschärfung

- [x] **M4.0 abschließen**
  - Durchgeführt am 2026-09-19 gemeinsam mit dem Benutzer (zusammen mit M3.0).
  - Entschieden: O-007 (Actor per `ICurrentUserService`-Seam, Dummy-Implementierung jetzt; keine Änderung an Transaction-Komponenten nötig wenn Auth kommt), O-025 (mehrere gleichzeitige Clients erlaubt, keine Locks, stale `ChangeVersion` deterministisch ablehnen), O-026 (keine automatische Transaction-Lebensdauer, Warnbadge ab 7 Tagen), O-027 (Undo nur im Editor bis Speichern; kein globaler Undo-Stack).
  - Konzepte aktualisiert: [Bedienkonzept und UI](../konzept/02-bedienkonzept-und-ui.md), [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md), [Offene Fragen](../konzept/07-entscheidungen-und-offene-fragen.md).
  - Gate: M4.1 und folgende Arbeitspakete sind durch Implementierungsagenten ausführbar.

## M4.1 – Transaction-Arbeitskontext

- [x] **M4.1 abschließen**

  - [x] **M4.1-T1 – Transaction beginnen, auflisten und fortsetzen**
    - Umfang: Beginnen mit Optionen, alle offenen Transactions auflisten, explizit auswählen und als Working Read Context öffnen. Ohne Auth existiert kein belastbares „eigene Transactions“.
    - Actor (O-007 entschieden): Actor wird beim Starten per `ICurrentUserService.GetCurrentUserName()` gesetzt; die aktuelle Dummy-Implementierung liefert den Wert aus `KnowHowToAI:Auth:DummyUserName`; danach immutable.
    - Gleichzeitige Clients (O-025 entschieden): mehrere Clients dürfen in derselben Transaction schreiben; keine Locks; stale `ChangeVersion` wird deterministisch abgelehnt und der Client zum Neuladen aufgefordert.
    - Lebensdauer (O-026 entschieden): keine automatische Verfällszeit; Alter im Dashboard sichtbar; Warnbadge ab sieben Tagen.
    - UI-Regel: pro Browserarbeitskontext genau eine aktive Transaction; Wechsel ist bewusst und sichtbar.
    - Tests: neue/vorhandene Transaction, leere Liste, ungültige ID, Refresh und Reconnect.
    - Abnahme: Working Context bleibt nach Navigation rekonstruierbar.

  - [x] **M4.1-T2 – Transaction-State und Navigationsschutz implementieren**
    - Umfang: globaler Status, Base Snapshot, Änderungsindikator und Schutz bei Kontextwechsel, Tabnavigation oder Circuit-Verlust.
    - Abgrenzung: bereits persistierte Änderungen gehen nicht verloren; rein flüchtiger Formzustand wird separat behandelt.
    - Tests: Refresh, Browsernavigation, Reconnect und Prozessneustart.
    - Abnahme: Benutzer weiß jederzeit, in welcher Transaction Änderungen landen.

## M4.2 – Validierung und Abschluss

- [ ] **M4.2 abschließen**

  - [x] **M4.2-T1 – Transaction validieren und Findings darstellen**
    - Umfang: serverseitige Validierung auslösen, Fehler/Warnungen gruppieren, zu betroffenen Nodes navigieren und veraltete Ergebnisse kennzeichnen.
    - Tests: valide, Fehler, Warnungen, gemischte Findings und Änderung nach Validierung.
    - Abnahme: Findings sind vor Commit verständlich und handlungsorientiert.

  - [x] **M4.2-T2 – Transaction-Diff vor Commit darstellen**
    - Umfang: strukturierte Änderungen der Working Transaction gegenüber ihrer Base anzeigen.
    - Prüfen: Erstellen, Ändern, Verschieben, Löschen und leere Transaction; große Diffs paginieren.
    - Abnahme: Benutzer kann den geplanten Commit vollständig prüfen.

  - [x] **M4.2-T3 – Commit und Discard implementieren**
    - Umfang: bestätigter Commit, bestätigtes Verwerfen, Erfolgs-/Fehlerzustand und Wechsel auf resultierenden Current Snapshot.
    - Schutz: Doppelaktion, stale `ChangeVersion` und ungültiger Status.
    - Tests: Erfolg, Validierungsfehler, Konflikt, Wiederholung und Discard.
    - Abnahme: Transaction-Lebenszyklus ist vollständig ohne MCP bedienbar.

  - [x] **M4.2-T4 – Release aus committed Snapshot anlegen**
    - Umfang: Release-Name und optionale Beschreibung erfassen, einen committed Snapshot explizit auswählen und `ReleaseService.CreateReleaseAsync` aufrufen.
    - Regeln: keine Transaction erforderlich; Working oder nicht vorhandene Snapshots bleiben unzulässig; Warnungen blockieren das Release nicht.
    - Tests: Erfolg, Namenskonflikt, fehlender/uncommitted Snapshot, Findingsanzeige und Navigation zum erzeugten Release.
    - Abnahme: der bestehende Release-Use-Case ist vollständig ohne MCP nutzbar.

## M4.3 – Node-Pflege

- [ ] **M4.3 abschließen**

  - [x] **M4.3-T1 – Nodes erstellen und Stammdaten bearbeiten**
    - Umfang: Node unter gewähltem Parent erstellen sowie Titel und Description bearbeiten.
    - Regeln: Änderungen nur in aktiver Transaction; serverseitige Normalisierung/Validierung bleibt maßgeblich.
    - Undo (O-027 entschieden): kein globaler Undo-Stack; Korrektur durch Gegenänderung oder vollständiges Discard.
    - Tests: gültige Werte, Duplikate, Grenzlängen, ungültiger Parent und gleichzeitige Aktualisierung.
    - Abnahme: Ergebnis ist unmittelbar im Working Tree sichtbar.

  - [x] **M4.3-T2 – Nodes kontrolliert löschen**
    - Umfang: Löschaktion, Auswirkungskurzansicht, Bestätigung und serverseitige Fehlerdarstellung.
    - Prüfen: Teilbaum, Referenzen/Dependencies, bereits gelöschter Node und Rootschutz gemäß Ist-Regeln.
    - Gleichzeitige Clients (O-025 entschieden): stale `ChangeVersion` deterministisch ablehnen und zum Neuladen auffordern.
    - Undo (O-027 entschieden): kein globaler Undo-Stack; Korrektur durch Gegenänderung oder vollständiges Discard.
    - Tests: Erfolgs- und Ablehnungsfälle sowie Diffdarstellung.
    - Abnahme: keine Löschung erfolgt ohne sichtbare Ziel- und Auswirkungsprüfung.

  - [x] **M4.3-T3 – Nodes per Drag-and-drop verschieben und sortieren**
    - Umfang: Move/Reorder im nativen Tree mit den Zielpositionen `Parent`, `Before` und `After`, sichtbarer Zielvorschau und anschließender Working-Tree-Aktualisierung. Native HTML-Drag-Ereignisse und die drei fokussierbaren Aktionsbuttons rufen denselben UI-Movevertrag auf.
    - Regeln: UI optimiert nur die Interaktion; serverseitige Hierarchievalidierung entscheidet.
    - Gleichzeitige Clients (O-025 entschieden): stale `ChangeVersion` deterministisch ablehnen; bei Ablehnung Tree vom Serverzustand neu laden.
    - Undo (O-027 entschieden): kein globaler Undo-Stack; Korrektur durch Gegenänderung oder vollständiges Discard.
    - Prüfen: Zyklus, ungültiges Ziel, Root, Source/Target auf verschiedenen 100er-Seiten, Cache-Eviction während der Auswahl, gleiches Ziel und vollständige Tastaturalternative.
    - Tests: bUnit-Komponentenfälle für alle drei Zielpositionen und Serverablehnung; Application-Tests für fachliche Varianten; je ein headless Playwright-Ablauf per Drag-and-drop und Aktionsbuttons. Beide Wege müssen identische Mutationseingaben und dieselbe bestätigte Serveraktualisierung erzeugen.
    - Abnahme: alle drei Zielpositionen sind per Maus und Tastatur präzise ausführbar; bei Ablehnung wird die betroffene Seite aus dem Serverzustand neu geladen und kein optimistischer Phantomzustand bleibt sichtbar.

  - [x] **M4.3-T4 – Initialen Root-Node im leeren Baum erstellen**
    - Umfang: Wenn im aktiven Transaction-Kontext noch keine Wissensknoten existieren (`VisualRootNode is null`), im leeren Wissensbaum bzw. der Inhaltsansicht eine dedizierte Aktion „Root-Knoten anlegen“ bereitstellen; Titel und optionale Description erfassen und `NodeMutationService.CreateAsync` ohne `parentNodeId` (`ParentNodeId = null`) aufrufen.
    - Regeln: Nur in aktiver Transaction; nur sichtbar/aktiv, wenn der Baum tatsächlich leer ist; nach Anlage Root-Knoten automatisch auswählen und Tree aktualisieren.
    - Tests: bUnit-Komponententest für leeren Baum mit und ohne aktive Transaction; Playwright-Browsernachweis für Initialanlage des Root-Knotens in einer leeren Datenbank.
    - Abnahme: Ein Benutzer kann eine komplett leere Wissensbasis ausschließlich über die Web-UI mit einem ersten Root-Knoten initialisieren.

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

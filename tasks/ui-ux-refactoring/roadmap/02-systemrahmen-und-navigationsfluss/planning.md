# Planung M2 – Systemrahmen und Navigationsfluss

## Status und Intention

**Status: ready / freigegeben.** M2 behebt den belegten Kernbruch der
Nutzerführung: Aus einem gelesenen Wissenseintrag muss die Bearbeitung derselben
Node ohne technische Vorbildung auffindbar sein. Danach werden Layoutbasis,
Mutation-URL-Synchronisation und Transaction-Führung auf diesen Einstieg
ausgerichtet. M2 bleibt innerhalb der bestehenden Routen, Fachverträge und
Contentgrenzen und ist für die sequenzielle autonome Ausführung nach
`03-orchestrierte-umsetzung.md` geschnitten.

## Verbindliche Referenzen

- [UI/UX-Konzeptindex](../../konzept/README.md)
- [Bedienmodell und mentale Modelle](../../konzept/01-bedienmodell-und-mentale-modelle.md)
- [Nutzerreisen und Zustandsflüsse](../../konzept/02-nutzerreisen-und-zustandsfluesse.md)
- [IA-Optionen](../../konzept/03-ia-und-navigationsoptionen.md)
- [Layout- und Aktionssystem](../../konzept/04-layout-und-aktionssystem.md)
- [Findings-/Evidenzinventar](../../konzept/05-findings-und-evidenz.md)
- [Entscheidungsregister](../../konzept/06-entscheidungsregister.md)
- [M1-Roadmap](../01-ist-audit-und-erste-iteration/roadmap.md), insbesondere
  der abgeschlossene Regressionsnachweis [M1.5-T7](../01-ist-audit-und-erste-iteration/tasks/M1.5-T7.md)
- Ist-Dokumentation: [`docs/README.md`](../../../../docs/README.md),
  [`Invarianten`](../../../../docs/Invarianten.md),
  [`Retrieval`](../../../../docs/Retrieval.md),
  [`Transaktionen und Historie`](../../../../docs/Transaktionen-und-Historie.md),
  [`Zielgruppen und Content`](../../../../docs/Zielgruppen-und-Content.md)
- Webfrontend-Zielkonzept: [`Bedienkonzept und UI`](../../../webfrontend/konzept/02-bedienkonzept-und-ui.md),
  [`Projektstruktur`](../../../webfrontend/konzept/08-projektstruktur-und-codekonventionen.md)

## Nutzerentscheidungen, die M2 verbindlich umsetzt

- **Bearbeitung:** Read-only zeigt bei Explicit+Independent eine sichtbare
  lokale `Bearbeiten`-Aktion. Ein kleiner Dialog lässt eine kompatible offene
  Arbeitskopie ausdrücklich wählen oder `Neue Arbeitskopie beginnen`; danach
  öffnet der vorhandene Editor exakt dieselbe Node und Zielgruppe. Es gibt keine
  stille Auswahl und keinen zweiten Einstieg. Kompatibel bedeutet: State
  `Open`, dieselbe Node im Working Snapshot aktiv, die gewählte Zielgruppe im
  Working Snapshot aktiv/verfügbar und derselbe Content dort weiterhin
  `Explicit+Independent`. Inkompatible Arbeitskopien werden sichtbar deaktiviert
  und mit dem konkreten Grund erklärt. `Neue Arbeitskopie beginnen` basiert auf
  Current. Dabei gilt deterministisch: (a) Begin-Fehler erzeugt keine neue
  Arbeitskopie und hält den Dialog offen; (b) Begin erfolgreich und Ziel
  kompatibel navigiert direkt in den Editor; (c) Begin erfolgreich, Ziel wegen
  eines Current-Races inkompatibel: Die erstellte Arbeitskopie wird niemals
  still verworfen oder verschwiegen, sondern im offenen Dialog eindeutig mit
  einem bestehenden Link zu ihren Transaction-/Arbeitskopiedetails genannt.
  Es gibt keine Navigation zu einer anderen Node oder zu Current und kein
  automatisches Discard.
- **Terminologie:** sichtbar sind `Wissenseintrag`, `Arbeitskopie`, `Zielgruppe`
  und `Bearbeiten`; `Node`, `Transaction`, IDs, Revisionen und Codes bleiben
  progressive technische Details.
- **IA:** Hybrid mit bestehenden Routen. M2 legt keine neue Route und keinen
  separaten Bearbeitungsbereich an.
- **Sonderzustände:** Fallback, `None` und `Derived` werden erklärt und bleiben
  an ihren bestehenden Read-only-/Working-Grenzen. Neue Contentfunktionen
  gehören M5.4; sie werden in M2 nicht dupliziert.

## Sequenz

1. **M2.0-T1** – node-lokalen Bearbeitungseinstieg herstellen. Dieser Leaf
   schafft die sichtbare Nutzerreise und ist die fachliche Voraussetzung für
   die folgenden Darstellungs- und Synchronisationskorrekturen.
2. **M2.1-T1** – gemeinsame `page-frame`-/`readable`-/
   `action-group`-Basis definieren und featureweise migrieren; `AudiencesPage`
   bleibt außerhalb.
3. **M2.2-T1** – URL-/Selection-Drift nach Create root, Create child, Update
   und Delete red-test-first beheben.
4. **M2.3-T1** – im Transaction-Detail den vorhandenen Weg `Im Wissensbaum
   öffnen` im sichtbaren Arbeitsfluss führen. Er ist Ergänzung für das
   Transaction-first-Nachbarbild, nicht ein zweiter Read-only-Einstieg.
5. **M2-Audit** – abgeschlossenen Milestone gegen Konzept, Verträge, Nachweise
   und Links prüfen; bei Findings höchstens eine Korrekturrunde gemäß
   `03-orchestrierte-umsetzung.md`.

## Zustands-, URL- und Terminologieverträge

Die vollständige ausführbare Zustandsmatrix liegt im [M2.0-T1-Leaf](tasks/M2.0-T1.md)
und ist für alle M2-Leaves verbindlich. Zusammengefasst gilt:

- `Explicit+Independent` darf über `Bearbeiten` in eine ausdrücklich gewählte
  oder neu gestartete Arbeitskopie führen.
- `Fallback`, `None` und `Derived` werden verständlich angezeigt; M2 erfindet
  keine Contentmutation.
- Historische Snapshot-/Release-Kontexte bleiben read-only.
- `NodeId`, `audienceId`, genau ein ReadContext-Selektor, Breadcrumb,
  Tree-Auswahl, Detail und WorkingState beschreiben stets denselben Zustand.
- Sichtbare Fachbegriffe stehen vor technischen IDs und Statuscodes.

## Nicht-Ziele und Grenzen

- keine neue Route, kein neuer Backendvertrag, keine neue fachliche
  Contentfunktion und keine Änderung von Commit/Discard oder Validierung;
- keine Änderung an `AudiencesPage`/Zielgruppenverwaltung;
- kein Duplikat des abgeschlossenen M1.5-T7-Transaction-Grids;
- keine allgemeine Action Registry, kein Stepper und keine Icon-only-
  Bearbeitungsaktion;
- keine eigene Tastaturabnahme und keine zusätzliche visuelle Vollsanierung;
  die repositoryweiten Build-, Verify-, FastTests- und gegebenenfalls
  Integrationstest-Gates bleiben verbindlich.

## Test- und Nachweisbudget

Jeder Leaf definiert den kleinsten passenden Component-/Browser-/Workflow-
Nachweis als Mindestbeleg. M2.0 belegt die node-lokale Reise und die vier
Contentzustände, M2.1 die vier Layoutbreiten inklusive Responsive, M2.2 die
vier Mutationpfade mit Red-Test-first, M2.3 den vorhandenen Transaction-Link.
Für jeden Leaf und den Milestone gelten zusätzlich vollständig die
repositoryweiten verbindlichen Incremental- und Abschlussgates aus
`.agents/rules/Richtlinien.mdc`: `dotnet build`, `verify(targetPath,
scope: "solution")` und der vollständige FastTests-Lauf; bei berührter SQL-,
Serverhost-, MCP-/HTTP- oder Prozessgrenze zusätzlich die vorgeschriebenen Integrationstests. Reine
Dokumentfolgen genügen `git diff --check` sowie Link-/Widerspruchsprüfung.

## Autonomer Abschlussvertrag

Die erste offene Checkbox ist der nächste ausführbare Leaf. Pro Leaf arbeitet
genau ein Schreibagent; danach werden Diff, Checkboxen und Nachweise geprüft.
Nach dem letzten fachlichen Leaf folgt der Audit-Agent. Bei Findings ist genau
eine Korrekturrunde zulässig; danach wird M2 abgeschlossen oder bei einer
echten ungeklärten Vertragsabweichung angehalten. Es gibt kein manuelles
Entscheidungsgate zwischen den Leaves.

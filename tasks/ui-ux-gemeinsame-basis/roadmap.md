# Roadmap: Gemeinsame UI-Seitenbasis

## Voraussetzungen und Reihenfolge

Die freigegebene [Konzeptentscheidung](Konzept.md) ist verbindlich. Die Punkte
werden in der angegebenen Reihenfolge ausgeführt; ein späterer Punkt darf erst
beginnen, wenn die Abnahme des vorherigen Punkts belegt ist. Die Ist-Doku unter
`docs/` bleibt nach der Umsetzung die einzige verbindliche Beschreibung des
implementierten Zustands.

- [x] **Gemeinsame Seitenbasis und Layout-Ownership implementieren**
  - **Intention:** Einen einzigen ausführbaren Vertrag für routable Page-Root,
    Seitenkopf und Inhaltsbereich schaffen und Shell-/Feature-Verantwortung
    technisch erzwingen.
  - **Scope:** Eine wiederverwendbare Blazor-Seitenbasis mit Seitentitel,
    optionaler Kurzbeschreibung sowie optionalen Badges/Aktionen als nutzbaren
    Vertrag für Page-Root, Seitenkopf und Inhaltsbereich schaffen. Ihre
    komponentennahe Gestaltung liegt einmalig an der gemeinsamen Basis; Reset,
    Tokens und frameworkweite Primitive bleiben zentral, fachliches Layout
    bleibt scoped am jeweiligen Feature. Die Basis beschreibt die Shell als
    Owner von Viewportfläche und Shell-Inset sowie den Page-Root als Nutzer der
    verfügbaren Breite ohne eigenes Shell-Padding, Zentrierung oder seitenweites
    `max-width`. Isolierte Komponenten-/Vertragstests belegen die Parameter,
    Seitenkopf-/`h1`-Semantik und die Landmark-Grenze der neuen Basis.
  - **Nicht-Ziele:** Keine Migration routbarer Pages, kein Entfernen lokaler
    Feature-Page-Root-, Header- oder `h1`-Regeln und keine Änderung des
    bestehenden Page-Markups außerhalb der neuen Basis. Kein Redesign von
    Feature-Rastern, Formularen, Tabellen, Bäumen, Diffs, Dialogen oder Karten;
    keine neue UI-Bibliothek, kein Generator, kein Theme, keine Routen-,
    Navigations-, Fachlogik-, Datenzugriffs- oder MCP-Änderung.
  - **Abnahme:** Die wiederverwendbare Basis besitzt den vollständigen
    Seitenbasis-/Headervertrag, ihre einmalige komponentennahe CSS-Ownership und
    isolierte grüne Komponenten-/Vertragstests. Die bestehenden Feature-Pages
    sind in diesem Punkt noch unverändert; die Migration und das Entfernen
    lokaler Parallelregeln sind ausschließlich Gegenstand des nächsten Punkts.

- [ ] **Alle routbaren Seiten und Zustände auf den Vertrag umstellen**
  - **Intention:** Das Konzept ohne Layoutausnahme auf den vollständigen
    implementierten Routenumfang und seine strukturell relevanten Zustände
    anwenden, ohne bestehende Fachfunktionen zu verändern.
  - **Scope:** Die gemeinsame Basis für jede folgende Route und jeden genannten
    Zustand verwenden: `/` (Laden, geladene Bereiche, bereichsweiser Fehler),
    `/knowledge` (Laden/Fehler, keine Zielgruppe, keine Auswahl, leerer Working
    Tree), `/knowledge/{NodeId:guid}` (dieselben Zustände sowie ausgewählter und
    nicht gefundener Node), `/search` (Kontextfehler, keine Zielgruppe, bereit,
    Suche/Ergebnisse), `/transactions` (Laden, leer, Startfehler, offene
    Transactions), `/transactions/{TransactionId:guid}` (Laden, Fehler, offene
    und abgeschlossene Transaction, Snapshot-Konflikt), `/audiences` (Laden,
    Kontextfehler, lesender und schreibender Kontext) und `/history` (Auswahl,
    Vergleich/Fehler, Release-Bereich). Jeder Zustand rendert über den Vertrag
    genau ein semantisches `h1`; `Wissensbasis` bleibt auf beiden Knowledge-
    Routen das stabile `h1`, der ausgewählte Node erhält `h2`, und eine
    Transaction-Detailseite darf ihren konkreten Zweck dynamisch als `h1`
    setzen. Bestehende Kontextübergaben, Interaktionen, Fokusverhalten,
    Responsive-/Reflow-Verträge und fachliche Zustandsdarstellungen bleiben
    erhalten. Dabei die ersetzten lokalen gemeinsamen Page-Root-, Header- und
    `h1`-Parallelregeln aus den Feature-CSS/-Markup entfernen und innerhalb von
    `main#shell-main` jedes weitere innere `main` entfernen.
  - **Nicht-Ziele:** Keine Vereinheitlichung beliebiger untergeordneter
    `h2`-/`h3`-Darstellungen, keine Änderung der Seiteninhalte oder Abläufe,
    keine Smartphonefreigabe und keine Erweiterung der Accessibility-Ziele.
    Die gemeinsame Basis selbst und ihre isolierten Vertragstests werden nicht
    in diesem Punkt neu entworfen.
  - **Abnahme:** Die vollständige Routen-/Zustandsmatrix ist im echten UI
    abgedeckt; jeder Zustand nutzt denselben Page-Root-/Headervertrag, besitzt
    genau ein `h1`, hält die festgelegte Wissensseiten-Semantik ein und zeigt
    keine zusätzliche `main`-Landmark. Die ersetzten lokalen gemeinsamen
    Root-/Header-/`h1`-Regeln und inneren `main`-Landmarks sind entfernt; es
    existiert keine unbegründete Ausnahme. Bestehende Nutzeraktionen und
    Zustandsübergänge funktionieren unverändert.

- [ ] **Struktur- und Layoutnachweise ergänzen**
  - **Intention:** Den gemeinsamen Vertrag gegen lokalen Drift und
    routeübergreifende Layoutregressionen automatisiert absichern.
  - **Scope:** Die in Punkt 1 isoliert geprüfte Basis in den routbaren Pages
    strukturell nachweisen und den bestehenden routeübergreifenden
    Browsernachweis
    auf alle acht Routen und die oben genannten abweichenden Zustände erweitern.
    Bei 1280 × 720, 1024 × 720 und breiten Desktop-Viewports Computed Styles,
    Bounding Boxes/Innenkanten von Page-Root und `main#shell-main`, Überschriften,
    Navigation offen/geschlossen und horizontalen Seitenüberlauf prüfen. Die
    Reflow-Nachweise bei effektiven 640 und 320 CSS-Pixeln einschließlich
    erreichbarer Inhalte und Aktionen beibehalten.
  - **Nicht-Ziele:** Keine Screenshot-basierte Alleinassertion, keine
    automatische Baselineaktualisierung, keine zusätzlichen Testvarianten ohne
    Bezug zum beschriebenen Seitenbasisvertrag und keine Builds/Testläufe, die
    nicht durch die Umsetzungsgates erforderlich sind.
  - **Abnahme:** Die passenden Fast-/Komponententests und betroffenen
    Browser-Tests sind grün; Assertions belegen Struktur, Semantik, Breite,
    Overflow und Reflow. Kein Nachweis beschränkt sich auf eine einzelne Route
    oder einen einzelnen Sichtbarkeitszustand.

- [ ] **Ist-Dokumentation und gemeinsamer UI-Audit abschließen**
  - **Intention:** Die implementierte Ownership und Nutzung dauerhaft auffindbar
    machen und den vollständigen visuellen Zustand routeübergreifend gegen Drift
    prüfen.
  - **Scope:** `docs/WebUi.md` und `docs/Architektur.md` auf den tatsächlich
    implementierten gemeinsamen Seitenbasis-/Headervertrag, seine CSS-Ownership,
    die betroffenen Routen sowie die zugehörigen Testgrenzen aktualisieren.
    Einen gemeinsamen `UiAudit`-Lauf mit Screenshots aller betroffenen Routen
    und repräsentativen Zustände ausführen; Computed-Style-, Bounding-Box-,
    Heading- und Overflow-Assertions bleiben primär, die Screenshots werden
    gemeinsam manuell auf unerklärten Drift geprüft. Die Abnahme gegen Konzept,
    vollständigen Diff und `git diff --check` dokumentieren.
  - **Nicht-Ziele:** Keine Vorab-Dokumentation eines nicht belegten Zustands,
    keine neuen UI-Anforderungen, keine automatische Baselineaktualisierung und
    keine Änderung fachlicher Ist-Dokumente außerhalb der tatsächlich
    betroffenen UI-/Architekturverantwortung.
  - **Abnahme:** Ist-Doku und Testnachweise beschreiben ausschließlich den
    implementierten Stand; der Audit deckt die vollständige Routenmatrix und
    repräsentative Zustände ab, ist visuell geprüft und ohne ungeklärte
    Ownership-, Semantik- oder Overflow-Abweichung abgeschlossen.

- [ ] **Audit**
  - **Intention:** Das fertig umgesetzte Vorhaben als Ganzes gegen Konzept,
    Nicht-Ziele, Invarianten und Nachweise freigeben.
  - **Scope:** Nur lesen und prüfen: Roadmap-Checkboxen, geänderte UI-/Test-/Doku-
    Dateien, Routen-/Zustandsabdeckung, gemeinsame Komponente und CSS-Ownership,
    `h1`-/`h2`-Semantik, sole-main-Landmark, Shell-Breite, Reflow, Fokus und
    dokumentierte Test-/Auditnachweise. Einen echten Verstoß höchstens in einer
    gezielten Korrekturrunde beheben und anschließend erneut prüfen.
  - **Nicht-Ziele:** Keine neuen Produktanforderungen, kein fachliches
    Redesign, keine Ausweitung auf Smartphonefreigabe und keine spekulative
    Aufräumarbeit.
  - **Abnahme:** Alle vorherigen Checkboxen und deren Abnahmen sind belegt; der
    Audit bestätigt Konzepttreue, keine ungeklärte Abweichung und vollständige
    Dokumentation des implementierten Zustands.

# M5 – Rollen-Content und Rich Text

[Roadmap-Index](../Roadmap.md)

- [ ] **M5 abschließen**

Abhängigkeit: [M4](04-transactions-und-strukturpflege.md)

Ziel: Rollenabhängiger Markdown-Content kann vollständig und komfortabel ohne Agent gepflegt werden.

Referenzen: [Node-Ansicht und Editor](../konzept/02-bedienkonzept-und-ui.md#node-ansicht-und-editor), [Rich-Text-Editor](../konzept/03-content-und-assets.md#rich-text-editor), [Freier Content](../konzept/03-content-und-assets.md#freier-content-einschließlich-todos), [Rollenverwaltung](../konzept/02-bedienkonzept-und-ui.md#rollenverwaltung)

## M5.1 – Editorbasis

- [ ] **M5.1 abschließen**

  - [ ] **M5.1-T1 – Rich-Text-Editor mit Markdownmodell integrieren**
    - Umfang: gewählten Editor laden, Markdown einlesen/ausgeben, Toolbar auf erlaubte Strukturen begrenzen und Headings deaktivieren.
    - Unterstützen: Formatierung, Links, Listen, Tabellen, Code und Zitate; Bilder folgen niedrig priorisiert in M8.
    - Tests: Komponenten-Smoke, Editorinitialisierung, Wechsel zwischen Nodes und große Inhalte.
    - Abnahme: Editor produziert Markdown und kein kanonisches HTML-Nebenformat.

  - [ ] **M5.1-T2 – Markdown-Roundtrip absichern**
    - Umfang: Golden Master für reale Inhalte und property-nahe Varianten der unterstützten Markdown-Strukturen.
    - Prüfen: Whitespace, Escaping, Tabellen, Codeblöcke, Links, Unicode und wiederholtes Öffnen/Speichern.
    - Abnahme: bekannte Normalisierung ist dokumentiert; semantisch verlusthafte Fälle werden abgelehnt oder behoben.

  - [ ] **M5.1-T3 – Optionalen Markdown-Quellmodus integrieren**
    - Voraussetzung: Editorentscheidung unterstützt einen sicheren Roundtrip.
    - Umfang: kontrollierter Wechsel WYSIWYG/Quelle, Synchronisierung und Fehleranzeige.
    - Prüfen: ungültige oder verbotene Headings, ungespeicherter Zustand und Fokus.
    - Abnahme: Quellmodus verändert gültigen Content nicht unbemerkt.

## M5.2 – Rollenverwaltung

- [ ] **M5.2 abschließen**

  - [ ] **M5.2-T1 – Rollen vollständig pflegen**
    - Umfang: Rollen auflisten, erstellen, bearbeiten und gemäß Ist-Regeln löschen.
    - Regeln: ausschließlich aktive Transaction; technische Rolle ist Zielgruppe, keine Berechtigung.
    - Tests: Validierung, Duplikate, referenzierte Rollen und Working-Ansicht.
    - Abnahme: Rollenmodell ist ohne MCP administrierbar.

  - [ ] **M5.2-T2 – Resolution Orders pflegen**
    - Umfang: Fallbackreihenfolge anzeigen, bearbeiten, validieren und Auswirkung vor dem Speichern visualisieren.
    - Prüfen: Zyklen, fehlende Rollen, Reihenfolgeänderung und aufgelöste Herkunft.
    - Tests: Domain-/Application-Verträge plus UI-Interaktion.
    - Abnahme: Fallbackänderungen sind vor Commit nachvollziehbar.

## M5.3 – Rollen-Content

- [ ] **M5.3 abschließen**

  - [ ] **M5.3-T1 – Rollen-Content erstellen, ersetzen und löschen**
    - Umfang: Node/Rolle auswählen, Content im Editor bearbeiten und über die vorhandenen Mutations-Use-Cases persistieren.
    - Kontext: explizite `TransactionId`, Revision und `ChangeVersion` verwenden.
    - Tests: Create, Replace, Delete, leerer Content, parallele Änderung und Serverfehler.
    - Abnahme: gespeicherter Working Content erscheint nach Navigation und Reconnect konsistent.

  - [ ] **M5.3-T2 – Independent/Derived, Quellen und Freshness integrieren**
    - Umfang: Modus und Quellen bearbeiten; Revision, Provenienz, Dependencies und Freshness anzeigen.
    - Prüfen: Derived ohne gültige Quelle, transitive Stale-Auswirkung und Rollen-Fallback.
    - Tests: repräsentative Ableitungs- und Freshnessfälle.
    - Abnahme: Inhalt und Abhängigkeiten sind gemeinsam verständlich und pflegbar.

## M5.4 – Validierung und freier Text

- [ ] **M5.4 abschließen**

  - [ ] **M5.4-T1 – Heading- und Contentvalidierung im Editor darstellen**
    - Umfang: schnelle clientnahe Rückmeldung plus maßgebliche Servervalidierung bei Speicherung/Transactionprüfung.
    - Prüfen: Markdown- und HTML-Headings, Grenzlängen, Normalisierung und mehrere Findings.
    - Abnahme: ungültiger Content wird nicht still verändert und nicht als erfolgreich gespeichert dargestellt.

  - [ ] **M5.4-T2 – TODOs als normalen Content regressionssicher abnehmen**
    - Umfang: Texte wie `TODO` oder `TODO: Besser formulieren` speichern, rendern, suchen und als Markdown exportieren.
    - Ausschluss: keine spezielle Entität, Hervorhebung, Validierung, Warnung oder Filterung hinzufügen.
    - Tests: WYSIWYG-/Markdown-Roundtrip und normale Such-/Exportpfade.
    - Abnahme: TODO-Text verhält sich technisch exakt wie anderer zulässiger Content.

## Milestone-Abnahme

- Rollen, Resolution Orders und Rollen-Content sind transaktional ohne Agent pflegbar.
- Markdown bleibt trotz WYSIWYG-Bearbeitung kanonisch und verlustarm.
- Fallback, Provenienz, Revision und Freshness bleiben sichtbar.
- TODOs besitzen keinerlei Sonderworkflow.

# M5 – Rollen-Content und Rich Text

[Roadmap-Index](../Roadmap.md)

- [ ] **M5 abschließen**

Abhängigkeit: [M4](04-transactions-und-strukturpflege.md)

Verbindliche M0-Basis: Editor ist ausschließlich Milkdown `@milkdown/crepe` `7.22.1`; Tiptap und eine erneute Editor-Auswahl sind ausgeschlossen. Die Interopgrenze besteht aus `mount`, `readMarkdown`, `focus` und `dispose`. Vitest ist für diesen dünnen Pfad nicht erforderlich. Komponenten- und Browsernachweise verwenden bUnit `2.11.3`/xUnit v3 `3.2.2` beziehungsweise Microsoft.Playwright .NET `1.62.0` mit Chrome Stable `152.0.7977.83`, `Channel = "chrome"`, `Headless = true`.

Ziel: Rollenabhängiger Markdown-Content kann vollständig und komfortabel ohne Agent gepflegt werden.

Referenzen: [Node-Ansicht und Editor](../konzept/02-bedienkonzept-und-ui.md#node-ansicht-und-editor), [Rich-Text-Editor](../konzept/03-content-und-assets.md#rich-text-editor), [Freier Content](../konzept/03-content-und-assets.md#freier-content-einschließlich-todos), [Rollenverwaltung](../konzept/02-bedienkonzept-und-ui.md#rollenverwaltung)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M5.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M5.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M4; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Markdown-Quellmodus (O-010), lokale npm-/Bundle-Erzeugung O-029 samt fester Werkzeugversion sowie die noch offenen Rollen-, Fallback-, Derived-Content- und Validierungsabläufe. Editorprodukt und Editorversion werden nicht erneut entschieden.
  - Prüfen: produktive Transaction- und Konflikt-UX aus M4, Milkdown-Interopvertrag, sichere Contentpolicy, M0-Golden-Master und tatsächliche Core-Verträge gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M5-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M5.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M5.1 – Editorbasis

- [ ] **M5.1 abschließen**

  - [ ] **M5.1-T1 – Rich-Text-Editor mit Markdownmodell integrieren**
    - Voraussetzung: O-027 zum Undo-Umfang und O-029 zur lokalen Milkdown-Buildtoolchain sind geschlossen.
    - Abhängigkeit: Die im M5.0-Gate festgelegte lokale npm-/Bundle-Erzeugung ist mit exakten Werkzeugversionen, Lockfile, Restore-/Buildbefehl, eindeutigem Outputpfad unter `wwwroot/`, CI-Integration und Lizenzinventarisierung im [Strukturkonzept](../konzept/08-projektstruktur-und-codekonventionen.md) dokumentiert. Vor Produktaufnahme wird der tatsächliche vollständige npm-Closure erneut inventarisiert; für `dompurify` ist die im M0-Befund ermittelte permissive Apache-2.0-Lizenzalternative einschließlich ihrer Pflichten zu verwenden und `THIRD-PARTY-NOTICES.md` im selben Commit zu ergänzen.
    - Umfang: `@milkdown/crepe` exakt `7.22.1` lokal und ohne CDN/Runtime-Download bündeln. `ContentEditor.razor.js` dynamisch importieren; `mount` erhält Markdown und Change-/Focus-Callbacks, weitere Interopaufrufe sind ausschließlich `readMarkdown`, `focus`, `dispose`. Vor erneutem `mount` bei Nodewechsel oder Reconnect `dispose` abwarten.
    - Toolbar: ausschließlich Bold, Italic, Strikethrough, Inline-Code und Link anzeigen; Latex, ImageBlock, Headings und Upload deaktivieren. Die interne ProseMirror-Struktur bleibt flüchtig und wird nie persistiert oder als zweiter Vertrag übertragen.
    - Unterstützen: Formatierung, sichere Links, Listen, Tabellen, Code und Zitate; Raw HTML, unsichere Links und fremde Ressourcen folgen der O-020-Policy, Bilder folgen niedrig priorisiert in M8.
    - Tests: bUnit prüft Komponentenzustand und den exakten dünnen JS-Aufrufvertrag; Playwright prüft Initialisierung, Markdownlesen, Fokus, Dirty-State, `dispose`/Remount bei Nodewechsel und Reconnect sowie Serverablehnung mit erhaltenem Editorwert. Netzwerkassertion: keine Drittanbieter-Origin und kein externer Bildrequest.
    - Abnahme: Editor produziert ausschließlich Markdown, kein kanonisches HTML-/JSON-Nebenformat; Paketversion, lokale Assets, Interopmethoden, Lifecycle und fehlende externe Requests sind automatisiert belegt.

  - [ ] **M5.1-T2 – Markdown-Roundtrip absichern**
    - Umfang: den M0-Golden-Master für Absätze, fett/kursiv/durchgestrichen, erlaubte Links, geordnete/ungeordnete/verschachtelte Listen, Tabellen, Inline-Code, Fenced Code mit Sprachkennung, Blockquotes und Unicode als dauerhafte Testdaten übernehmen; Herkunft und Lizenz der Fixture-Daten dokumentieren, keinen Spike-Code kopieren.
    - Prüfen: jeden zulässigen Master fünfmal `Markdown -> Editor -> Markdown` durchlaufen und mit derselben Markdig-`0.42.0`-Advanced-Pipeline semantisch vergleichen; zusätzlich Whitespace, Escaping, Inhalt nahe 4 KiB und wiederholtes Öffnen/Speichern.
    - Negativfälle: Raw HTML sowie Markdown-/HTML-Headings bleiben als Eingabe erhalten, bis der Server sie mit `RawHtmlNotAllowed` beziehungsweise `HeadingNotAllowed` ablehnt; unzulässige Links und externe Bilder liefern `LinkTargetNotAllowed` beziehungsweise `ExternalImageNotAllowed`, externe Bilder erzeugen keinen Request. Paste aus Text, Browser-HTML und Office-HTML reduziert nur gemäß Contentpolicy und zeigt einen zusammengefassten `role=status`-Hinweis.
    - Abnahme: alle zulässigen Master bestehen fünf Zyklen semantisch; keine verbotene Struktur verschwindet still und jede bekannte Normalisierung ist dokumentiert.

  - [ ] **M5.1-T3 – Entscheidung zum Markdown-Quellmodus umsetzen**
    - Voraussetzung: O-010 ist durch den Benutzer entschieden; bei Aufnahme unterstützt die Editorentscheidung einen sicheren Roundtrip.
    - Umfang: kontrollierter Wechsel WYSIWYG/Quelle, Synchronisierung und Fehleranzeige.
    - Prüfen: ungültige oder verbotene Headings, ungespeicherter Zustand und Fokus.
    - Abnahme: O-010 ist aus den offenen Fragen entfernt; bei Aufnahme verändert der Quellmodus gültigen Content nicht unbemerkt, bei Ablehnung sind Quellmodus-Komponenten und -Abhängigkeiten nicht vorhanden.

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
    - Voraussetzung: O-025 zur Zusammenarbeit in derselben Transaction ist durch den Benutzer entschieden.
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
    - Prüfen: Markdown-/HTML-Headings (`HeadingNotAllowed`), Raw HTML (`RawHtmlNotAllowed`), unzulässige Links (`LinkTargetNotAllowed`), externe Bilder (`ExternalImageNotAllowed`), Grenzlängen, Normalisierung und mehrere Findings. Die neuen stabilen Fehlercodes werden im zuständigen Core-/MCP-Katalog und in der Ist-Dokumentation erst zusammen mit ihrer Produktimplementierung ergänzt.
    - Abnahme: ungültiger Content bleibt vollständig im Editor erhalten, wird nicht still verändert und nicht als erfolgreich gespeichert dargestellt; jeder Ablehnungsfall zeigt den stabilen Code und markiert den zugehörigen Inhalt, soweit eine Position bestimmbar ist.

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

# Idee: Reproduzierbares UI-Screenshot-Audit und Use-Case-zentriertes Layout-Refactoring

**Status:** Step 1 ist als On-Demand-Runner umgesetzt; das eigentliche UI/UX-Audit und Layout-Refactoring bleiben spätere Schritte. Die verbindliche Betriebsbeschreibung steht in [Konfiguration und Betrieb](../../docs/Konfiguration-und-Betrieb.md). Betrifft Web-Frontend, Playwright-BrowserTests, UI/UX-Design und automatisiertes Agenten-Audit.

---

## 1. Ausgangslage & Motivation

Das Web-Frontend von KnowHowToAI wurde im Zuge der Roadmaps M1 bis M5 funktional und architektonisch solide aufgebaut (Read-Only Cockpit, Snapshot-Modell, Working Transactions, Reconnect-Resilienz, ChangeVersion-Schutz, Crepe-Editor, Rollenverwaltung). 

Dabei stand verständlicherweise bisher die **technische Korrektheit** im Vordergrund:
- Transaktionen, Snapshots, Dirty-States und ChangeVersions funktionieren zuverlässig.
- **Aber:** Die Benutzeroberfläche spiegelt an vielen Stellen noch zu stark die **technischen Innereien** wider (Snapshot-IDs, Transaktions-Status, Hashwerte, DB-Konzepte), anstatt die **Arbeitsweise des Menschen (Use Case)** in den Mittelpunkt zu stellen.
- Es gibt spürbare **Layout- und UX-Schwächen**:
  - **Veraltete Navigationselemente:** Nav-Links wirken wie schlichte Hyperlinks aus den 1990ern statt wie moderne, taktile Navigations-Items oder Button-Gruppen.
  - **Layout-Glitches & Umbrüche:** Buttons brechen bei bestimmten Breiten unschön oder unerwartet um; Toolbars wirken gequetscht.
  - **Mangelnde Affordance & Orientierung:** Für einen Menschen ist auf den ersten Blick oft unklar:
    - *Wie bearbeite ich diesen Text?*
    - *Wo und wie speichere ich meine Änderungen?* (Autosave, Speichern-Button, Tastatur-Shortcut?)
    - *Was ist geschützter Fallback-Inhalt, was ist expliziter Inhalt dieser Rolle?*
    - *In welchem Kontext/Modus befinde ich mich gerade?*

---

## 2. Die Vision

Nach Fertigstellung von M5 (wenn alle Kernfeatures inklusive Rollen-Content und Editor im System existieren) wird ein **ganzheitliches Layout-Audit und Refactoring** durchgeführt:
1. **Weg von der Technik-UI:** Technische Aspekte (Snapshots, Transaktionen, Pins) rücken dezent in den Hintergrund (z. B. dezente Status-Pills, Kontext-Menüs oder Fußzeile).
2. **Hin zum fokussierten Wissens-Arbeitsplatz:** Das eigentliche Wissen (Titel, Hierarchie, Markdown-Content, Relationen) steht absolut im Zentrum – aufgeräumt, typografisch harmonisch, intuitiv bedienbar.
3. **Automatisierte Sichtprüfung durch Agenten:** Mittels reproduzierbarer Vollbild-Screenshots aller Seiten und Zustände kann ein multimodales LLM die UI systematisch auf "Nutzungs-Bullshit", Designbrüche und Usability-Fallen analysieren.

---

## 3. Step 1: Reproduzierbare Screenshot-Pipeline (On-Demand)

Damit Mensch und Agent über dieselben visuellen Fakten sprechen, wird ein isolierter, **on-demand ausführbarer Screenshot-Runner** geschaffen:

### Anforderungen an den Runner
- **Technologie:** Basiert auf dem bereits im Repo etablierten `Microsoft.Playwright` (`KnowHowToAI.BrowserTests`), läuft jedoch **nicht** im regulären Schnelltest- oder Build-Workflow (keine Verlangsamung der täglichen Arbeit).
- **Aufruf nur bei Bedarf:** z. B. über ein PowerShell-Script `scripts/capture-ui-audit.ps1` oder einen dedizierten Testfilter `dotnet test --filter Category=UiAudit`.
- **Feste Standardauflösung:** Desktop mit 1280 × 800 px. Der Viewport erhält den
  Desktop-Charakter einschließlich Navigation, Header und Content-Area, bleibt
  für die spätere multimodale Auswertung aber kompakter als 1440 × 900 px.
- **Abgedeckte Kernzustände:**
  1. *Read-Only Cockpit:* Leerzustand / Startseite.
  2. *Read-Only Cockpit:* Ausgewählter Node (Hierarchiebaum, Breadcrumbs, Metadaten, gerenderter Markdown-Content).
  3. *Suche & Filter:* Suchansicht mit Eingabe und Trefferliste.
  4. *Working Transaction:* Strukturpflege (Node anlegen, umbenennen, verschieben).
  5. *Content-Editor (WYSIWYG):* Aktiver Bearbeitungsmodus mit Crepe-Toolbar und dirty state.
  6. *Content-Editor (Markdown-Quellcode):* Quelltextansicht mit Formatierungsoptionen.
  7. *Rollen:* Read-only- und Working-Ansicht, Löschbestätigung sowie die
     Fallback-Darstellung am ausgewählten Wissensknoten.
  8. *Feedback:* Commit-, Verwerfen- und Löschbestätigungen.
- **Ablage:**
  - Temporäre, nicht versionierte Ablage unter
    `temp/ui-audit/YYYY-MM-DD_HH-mm-ss/` mit semantischen Dateinamen und einem
    maschinenlesbaren Manifest.
  - Ein abweichender Ausgabe-Root kann beim bewussten Aufruf angegeben werden;
    Git bleibt standardmäßig frei von großen binären Momentaufnahmen.

---

## 4. Step 2: Agentisches UI/UX-Audit auf Screenshot-Basis

Sobald die Screenshots erzeugt sind, können diese einem multimodalen LLM (oder einem spezialisierten Subagenten) übergeben werden, um ein systematisches UX-Review durchzuführen.

### Typische Prüffragen des Audits:
1. **Visuelle Hierarchie & Fokus:**
   - Dominiert der Inhalt (`ContentMd`) die Seite oder wird er von Seitenleisten, Headern und Metadaten erschlagen?
   - Sind primäre Aktionen (z. B. „Bearbeiten“, „Speichern“, „Neuer Node“) sofort auffindbar?
2. **Affordance & Interaktion:**
   - Erkennen Nutzer sofort, was klickbar ist? (Buttons vs. altertümliche Hyperlinks).
   - Ist der Unterschied zwischen Ansichtsmodus und Bearbeitungsmodus unmissverständlich?
   - Versteht der Nutzer intuitiv, wie gespeichert oder abgebrochen wird?
3. **Layout & Typografie:**
   - Wo brechen Buttons oder Menüeinträge unschön um?
   - Sind Abstände (Margins/Paddings) harmonisch oder gibt es gequetschte/verwaiste Bereiche?
   - Stimmen Kontraste und Lesbarkeit bei längeren Texten?
4. **Fachliche vs. Technische Sprache:**
   - Welche technischen Begriffe (z. B. Snapshot-Hash, Transaction-ID, Version-Tag) verwirren den normalen Nutzer und sollten dezent verstaut werden?

---

## 5. Step 3: Layout-Refactoring (Umsetzung)

Aus den Ergebnissen des Audits entsteht ein gezielter Arbeitsplan:
- **Navigation & Shell:** Überarbeitung von `MainLayout.razor` und `NavMenu.razor` zu einer modernen, ergonomischen App-Shell (saubere Icons/Buttons, klare aktive Zustände).
- **Aktionsleisten & Controls:** Konsistente Platzierung von Action-Buttons (Primary, Secondary, Danger), Verhinderung von Zeilenumbrüchen bei variabler Breite.
- **Editor-Integration:** Klare optische Trennung von Lese- und Schreibmodus, intuitive Speicher-/Verwerfen-Aktionen, transparente Darstellung von Rollen-Fallbacks.
- **Design-Tokens:** Nutzung der bestehenden Vanilla-CSS-Tokens für konsistente Abstände, Typografie und Farbkodierung.

---

## 6. Nutzen

- **Hohe Produktreife:** Verwandelt ein technisch funktionierendes System in ein echtes, intuitiv bedienbares Produkt für den Menschen.
- **Schnelle Feedback-Schleifen:** Bei Design- und Layout-Diskussionen muss nicht geraten werden; Screenshots im Repo machen jedes Problem sofort sichtbar.
- **Agentische Selbstkorrektur:** LLMs können durch den visuellen Abgleich selbstständig erkennen, wenn eine CSS-Änderung zu unerwünschten Umbrüchen oder Inkonsistenzen geführt hat.

# M2 – Designsystem und Anwendungsshell

[Roadmap-Index](../Roadmap.md)

- [ ] **M2 abschließen**

Abhängigkeit: [M1](01-webhost-und-mcp-http.md)

Ziel: Alle Fachfeatures erhalten eine konsistente, moderne und belastbare UI-Grundlage.

Referenzen: [Visueller Stil](../konzept/02-bedienkonzept-und-ui.md#visueller-stil), [Grundlayout](../konzept/02-bedienkonzept-und-ui.md#grundlayout), [Blazor-Betrieb](../konzept/06-betrieb-sicherheit-und-risiken.md#blazor-betrieb)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M2.1 – Komponentenbasis und Theme

- [ ] **M2.1 abschließen**

  - [ ] **M2.1-T1 – Gewählte UI-Komponentenbasis integrieren**
    - Umfang: die in O-001 gewählte native oder bibliotheksgestützte Basis, erforderliche Services, statische Ressourcen, Basislayout und Theme-Einstieg produktiv einbinden; `KnowHowToAI.Web.Tests` mit dem in O-015 gewählten Komponenten-Testwerkzeug anlegen.
    - Nicht enthalten: Knowledge Tree und Rich-Text-Editor.
    - Tests: Host-/Render-Smoke sowie Nachweis ohne externe Cloudabhängigkeit.
    - Abnahme: Beispielseite rendert die benötigten Basiskomponenten im Serverbetrieb.

  - [ ] **M2.1-T2 – Design-Tokens und Business-Theme definieren**
    - Umfang: zentrale Farben mit `#2563EB` als primärem Starttoken, Systemschriftkette `Segoe UI, Arial, sans-serif`, Abstände, Raster, Rahmen, Elevation, Fokusdarstellung und Iconkonvention definieren; `KnowHowTo AI` in der Shell als reine Textwortmarke darstellen.
    - Sprache: ausschließlich deutsche UI ohne Lokalisierungsinfrastruktur. Einmalige Texte bleiben featurelokal; nur tatsächlich gemeinsam verwendete Bezeichnungen und Fehlermappings werden zentralisiert.
    - Ausschluss: kein Dummy-/Bildlogo, Webfont, Font-CDN, `.resx`, `IStringLocalizer` oder globale Sammlung aller UI-Texte.
    - Zustände: neutral, aktiv, Erfolg, Warnung, Fehler, deaktiviert und ungespeichert.
    - Prüfen: Light Theme als erster Stand, ausreichende Kontraste und konsistente Dichte.
    - Abnahme: Theme ist zentral definiert; Fachkomponenten enthalten keine verstreuten Designwerte.

## M2.2 – Anwendungsshell

- [ ] **M2.2 abschließen**

  - [ ] **M2.2-T1 – Hauptlayout und Navigation implementieren**
    - Umfang: Hauptnavigation, Arbeitsfläche, Kontextbereich, Breadcrumb-Zone und globale Aktionszone.
    - Prüfen: sinnvolle Breiten, Scrollverhalten, tiefe Inhalte und kleine Desktopauflösung.
    - Nicht enthalten: fachliche Dashboard-, Baum- oder Editorimplementierung.
    - Abnahme: jede spätere Fachseite kann ohne eigenes Seitenraster eingebunden werden.

  - [ ] **M2.2-T2 – Globale Wissenskontextleiste implementieren**
    - Umfang: sichtbare Plätze für Snapshot/Transaction, Rolle, Änderungszustand und späteren Kontextwechsel.
    - Initial: read-only Platzierung mit realen ViewModels; Interaktionen folgen in M3/M4.
    - Abnahme: Arbeitsstand und Rolle besitzen eine einheitliche globale Darstellung.

## M2.3 – Wiederverwendbare UI-Zustände

- [ ] **M2.3 abschließen**

  - [ ] **M2.3-T1 – Lade-, Leer- und Fehlerzustände bereitstellen**
    - Umfang: wiederverwendbare Komponenten für Initial Load, Teilaktualisierung, leere Listen, Not Found und technische Fehler.
    - Prüfen: Retry, Korrelation/Diagnose ohne sensible Details und Screenreader-Texte.
    - Abnahme: keine Fachseite muss grundlegende Zustandsdarstellung neu erfinden.

  - [ ] **M2.3-T2 – Warnungs-, Bestätigungs- und Änderungszustände bereitstellen**
    - Umfang: Inline-Warnung, Banner, Toast, bestätigungspflichtiger Dialog und unsaved/working Indikator.
    - Prüfen: Tastaturbedienung, Fokusführung und Schutz vor versehentlichen Doppelaktionen.
    - Abnahme: Interaktionsmuster sind dokumentiert und komponentenseitig testbar.

## M2.4 – Robustheit und Zugänglichkeit

- [ ] **M2.4 abschließen**

  - [ ] **M2.4-T1 – Responsive Mindestdarstellung und Tastaturnavigation absichern**
    - Umfang: Desktop-first Layout bei 1024 × 720, 200-%-Zoom ohne Funktionsverlust, 400-%-Reflow für normale Inhalte, Fokusreihenfolge, Skip-Ziele, Kontraste und semantische Beschriftung gemäß O-021.
    - Tests: Komponentenassertionen, wenige Accessibility-Smokes und echte Tastatursequenzen ausschließlich headless; zusätzlich eine kurze reproduzierbare Tastaturcheckliste für die menschliche Abnahme erstellen.
    - Abnahme: Kernnavigation und Shellzustände sind ohne Maus bedienbar; Fokus bleibt sichtbar, Inhalt bleibt bei Mindestbreite und Desktop-Zoom erreichbar. Der Agent startet keinen interaktiven Browser und behauptet keine formale WCAG-Zertifizierung.

  - [ ] **M2.4-T2 – Reconnect- und Circuit-Verlust-Oberfläche implementieren**
    - Umfang: Reconnecting, getrennt, Wiederverbinden und kontrolliertes Neuladen darstellen.
    - Prüfen: flüchtiger UI-State wird nicht mit persistierter Working Transaction verwechselt.
    - Tests: simulierte Unterbrechung und Wiederverbindung.
    - Abnahme: der Benutzer erkennt Verbindungszustand und sichere nächste Aktion.

  - [ ] **M2.4-T3 – Komponenten- und visuelle Smoke-Testbasis etablieren**
    - Umfang: `KnowHowToAI.BrowserTests` mit dem in O-015 gewählten Werkzeug anlegen; Testhost, zentrale Shellzustände, repräsentative Viewports und stabile Screenshot-/Markup-Smokes einrichten.
    - Nicht enthalten: flächendeckende Pixeltests.
    - Abnahme: beide Testprojekte sind in Solution und Testskripte integriert; spätere Milestones können neue UI-Zustände regressionssicher ergänzen.

## Milestone-Abnahme

- Einheitliche Anwendungsshell ohne fachliche Platzhalterlogik.
- Design, Zustände, Reconnect und grundlegende Zugänglichkeit sind wiederverwendbar abgesichert.
- Alle folgenden Features verwenden dieselbe Komponenten- und Themebasis.

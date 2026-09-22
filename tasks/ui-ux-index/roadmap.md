# Roadmap: UI-Gesamtbild für Agenten

## Verbindliche Grundlage

- [Freigegebenes Konzept](Konzept.md)
- [Dokumentationseinstieg](../../docs/README.md) und
  [Doku-Richtlinien](../../.agents/rules/DokuRichtlinien.mdc)
- [Architektur](../../docs/Architektur.md),
  [manuelle UI-Abnahme](../../docs/Manuelle-UI-Abnahme.md) und
  [Web-UI-Guardrails](../../.agents/rules/WebUiHtmlCss.mdc)
- Aktueller Ist-Code unter `src/KnowHowToAI.Server/Web/` sowie die
  repräsentativen Komponenten- und Browser-Smokes unter
  `tests/KnowHowToAI.Web.Tests/` und `tests/KnowHowToAI.BrowserTests/`

- [ ] **UI-Istbild dokumentieren und verbindlich verankern**
  - Intention: `docs/WebUi.md` gibt Agenten ein kompaktes, am Code belegtes
    Gesamtbild von Shell, Kontext, Seitennetz, Hauptbereichen und
    Verantwortungsgrenzen, ohne Implementierungsdetails zu duplizieren.
  - Scope:
    - Shell, Hauptnavigation, Wissenskontext, Page-Regions, globale Dialog-,
      Feedback- und Dirty-State-Grenzen sowie die tatsächlichen Kernabläufe am
      aktuellen Razor-Code erheben.
    - Alle aktuellen `@page`-Direktiven und ihre relevanten Parameter-, Query-
      und Read-/Write-Kontexte inventarisieren. Planungsstand sind acht
      Routendirektiven auf sieben routbaren Komponenten; maßgeblich ist der
      Ist-Code bei Ausführung. `/knowledge` und `/knowledge/{NodeId:guid}` sowie
      `/transactions` und `/transactions/{TransactionId:guid}` bleiben jeweils
      als unterschiedliche Route-Varianten sichtbar.
    - `docs/WebUi.md` in der im Konzept festgelegten Reihenfolge erstellen:
      Lesezweck/Grenzen, globales UI-Modell, Seitennetz/Kernabläufe,
      Routenindex, knappe Seitensteckbriefe sowie gemeinsame Verträge und
      Detailquellen.
    - Jede Route direkt mit ihrer routbaren `.razor`-Komponente und mindestens
      einem vorhandenen repräsentativen Komponenten- oder Browser-Smoke
      verknüpfen. Tiefere Komponenten und Fachdokumente nur referenzieren, wenn
      sie für Verantwortung oder Zustandsgrenze erforderlich sind.
    - Die in `docs/Architektur.md` verstreute Beschreibung der sichtbaren Shell
      und Seiten an der neuen Dokumentgrenze entflechten: UI-Gesamtbild in
      `WebUi.md`; technische Schichtung, Zustandsführung, Implementierungs- und
      Testgrenzen weiterhin in `Architektur.md`. Verbleibende Aussagen
      gegenseitig verlinken statt kopieren.
    - `docs/README.md` um `WebUi.md` im Dokumentenindex und einen eindeutigen
      Lese-Matrix-Eintrag für Web-UI-, Layout- und UX-Aufgaben ergänzen.
    - `.agents/rules/WebUiHtmlCss.mdc` auf `docs/WebUi.md` verweisen lassen und
      dessen Lektüre sowie Aktualisierung bei geändertem seitenübergreifendem
      UI-Ist-Zustand verbindlich machen; keine UI-Inhalte in die Regel kopieren.
  - Nicht:
    - Keine Änderung an Razor, CSS, Navigation, Verhalten oder Tests; kein
      Redesign und keine Soll-Architektur.
    - Kein vollständiger Button-, Text-, CSS-, Komponenten-, DTO- oder
      Testkatalog.
    - Keine umfassende Neufassung von `docs/Architektur.md` außerhalb der
      zwingenden UI-Entflechtung und keine zweite normative UI-Quelle.
    - Planungs- und Roadmap-Artefakte unter `tasks/` dienen nicht als Nachweis
      für den implementierten Ist-Zustand.
  - Abnahme:
    - Jede im aktuellen Webprojekt gefundene `@page`-Route erscheint genau
      einmal im Routenindex; jede dokumentierte Route, Aktion, Zustandsgrenze
      und Referenz ist am Code oder einer bestehenden Ist-Doku belegbar.
    - Globales UI-Modell, Hauptnavigation und Wissenskontext stimmen mit
      `MainLayout`, `PrimaryNavigation`, `KnowledgeContextBar` und den
      Page-Regions überein.
    - Jeder Seitensteckbrief nennt nur Intention, Hauptbereiche, primäre
      Aktionen und relevante Zustandsvarianten und verlinkt die routbare
      Razor-Komponente sowie mindestens einen vorhandenen repräsentativen Test.
    - `WebUi.md` und `Architektur.md` enthalten keine konkurrierende normative
      Beschreibung derselben sichtbaren UI-Verantwortung.
    - `docs/README.md` und `WebUiHtmlCss.mdc` machen die neue Quelle auffindbar
      und verbindlich, ohne Inhalt zu duplizieren.
    - Alle relativen Markdown-Links lösen auf. Routen- und Referenzinventar,
      saubere Diff-Prüfung und `git diff --check` sind dokumentiert und grün.
      Build, Test- oder Browserlauf sind für dieses reine Doku-/Regeldelta nicht
      erforderlich.
    - Die Checkbox wird erst nach erfüllter Abnahme geschlossen; Ergebnis,
      Abschlussnachweis und Checkbox werden gemeinsam atomar committet.

- [ ] **Begrenzten Abschlussaudit durchführen**
  - Intention: Ein unabhängiger, rein lesender Audit bestätigt, dass das
    UI-Gesamtbild vollständig, knapp, belegbar und widerspruchsfrei verankert ist.
  - Scope: Ergebnis gegen `Konzept.md`, Doku-Richtlinien, den aktuellen
    Routen-/Shell-Code, repräsentative Tests und alle Abnahmepunkte des ersten
    Roadmap-Punkts prüfen; insbesondere fehlende Route-Varianten, unbelegte
    Ist-Aussagen, tote Links und Doppelzuständigkeiten mit `Architektur.md`
    suchen.
  - Nicht: Keine neue Produktanforderung, kein Redesign, keine Ausweitung auf
    allgemeine Architekturpflege und keine unmittelbare Änderung durch den
    Audit-Agenten.
  - Abnahme:
    - Der Audit meldet `bestanden` ohne offenen Konzept-, Scope-, Beleg- oder
      Konsistenzverstoß; das Ergebnis und die geprüften Nachweise werden knapp
      direkt unter diesem Punkt festgehalten.
    - Bei einem konkreten Verstoß bleibt die Checkbox offen. Es ist höchstens
      eine gezielte Korrekturrunde im bestehenden Scope zulässig; danach wird
      erneut begrenzt geprüft oder an den Nutzer eskaliert.
    - Die Checkbox wird erst nach bestandenem Audit geschlossen; Ergebnis und
      Checkbox werden gemeinsam atomar committet.

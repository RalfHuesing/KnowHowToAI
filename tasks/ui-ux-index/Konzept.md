---
status: ready
---

# UI-Gesamtbild für Agenten

## Intention

Agenten benötigen vor seitenübergreifenden UI-Arbeiten ein belastbares mentales
Modell der heutigen Weboberfläche. Eine kurze Ist-Dokumentation unter `docs/`
zeigt Shell, Wissenskontext, Seitennetz und Verantwortungsgrenzen und führt für
Details direkt zum zuständigen Razor-Code. Sie verhindert lokale Umbauten, die
Navigation, Zustandskontext oder benachbarte Seiten übersehen.

Das Ergebnis ist Orientierung, keine zweite Implementierungsspezifikation und
kein Entwurf des späteren Redesigns.

## Zielbild der Dokumentation

`docs/WebUi.md` bleibt als ein Dokument in dieser Reihenfolge lesbar:

1. **Lesezweck und Grenzen** – heutiger UI-Ist-Zustand; Details bleiben im Code
   und in den verlinkten fachlichen Dokumenten.
2. **Globales UI-Modell** – kompakte Skizze der Shell und klare Ownership von
   App-Leiste, Hauptnavigation, globalem Wissenskontext, Seitenkopf,
   Arbeitsfläche, Kontextbereich, Dialogen und Toasts.
3. **Seitennetz und Kernabläufe** – eine kleine Flussdarstellung für die
   tatsächlichen Übergänge, insbesondere Lesen/Suchen sowie
   Transaction beginnen, Wissen oder Zielgruppen bearbeiten, prüfen und
   abschließen.
4. **Routenindex** – pro kanonischer Route genau eine Zeile mit Zweck,
   Read-/Write-Kontext, wichtigsten Regionen beziehungsweise primären Aktionen
   und direktem Link zur routbaren `.razor`-Komponente.
5. **Seitensteckbriefe** – je Route wenige Stichpunkte zu Intention,
   dargestellten Hauptbereichen, primären Nutzeraktionen, relevanten
   Zustandsvarianten und weiterführenden Code-/Testreferenzen.
6. **Gemeinsame Verträge und Detailquellen** – nur die routeübergreifend
   entscheidenden Zusammenhänge und Links auf deren normative Quelle;
   insbesondere Wissenskontext, Navigation/Dirty-State, Feedbackzustände,
   Layout-Ownership, Accessibility und manuelle Abnahme.

Die Dokumentation nennt primäre Nutzeraktionen, aber inventarisiert nicht jeden
Button. Sie nennt routbare Seiten und global prägende Komponenten, aber erzeugt
keinen vollständigen Komponentenkatalog. Flüchtige Markup-, CSS- und
Implementierungsdetails bleiben in `.razor`, `.razor.css` und Tests.

## Scope

### Muss

- Den implementierten UI-Ist-Zustand gegen Shell, Navigation, `@page`-Direktiven,
  routbare Seiten und repräsentative Browser-/Komponententests erheben.
- `docs/WebUi.md` nach dem oben definierten informationsdichten Aufbau anlegen.
- Alle implementierten kanonischen Routen einschließlich Parameter- und
  relevanter Query-/Kontextvarianten erfassen; nicht in der Hauptnavigation
  sichtbare Routen dürfen nicht verschwinden.
- Pro Seite die fachliche Verantwortung und den Kontext sichtbar machen, nicht
  das Razor-Markup in Prosa nacherzählen.
- Direkte relative Referenzen auf die jeweils zuständige routbare
  `.razor`-Datei sowie nur die für das Verständnis nötigen tieferen Komponenten,
  Tests und Fachdokumente setzen.
- `docs/README.md` um `WebUi.md` und einen eindeutigen Eintrag in der
  Lese-Matrix für Web-UI-/Layout-/UX-Aufgaben ergänzen.
- `.agents/rules/WebUiHtmlCss.mdc` so verankern, dass Agenten vor
  seitenübergreifenden UI-Änderungen das dokumentierte Gesamtbild lesen und bei
  geändertem Ist-Zustand im selben Slice aktualisieren. Die Regel verlinkt,
  statt UI-Inhalte zu duplizieren.
- Die heute in `docs/Architektur.md` verstreute UI-Seiten- und Shellbeschreibung
  gegen die neue Verantwortungsgrenze prüfen: sichtbares UI-Gesamtbild nach
  `WebUi.md` verschieben oder dort zusammenfassen; technische Schichtung,
  Zustandsführung und Implementierungsgrenzen in `Architektur.md` belassen.
  Beide Dokumente verlinken statt parallele normative Aussagen zu führen.
- Bestehende Quellen wie Doku-Richtlinien, Architektur und manuelle UI-Abnahme
  nur referenzieren; die Webfrontend-Konzeption unter `tasks/` ist Planung und
  darf weder als Ist-Nachweis noch als zweite verbindliche UI-Quelle erscheinen.

### Nicht

- Kein Redesign, keine Umstrukturierung der Anwendung und keine Änderung an
  Razor, CSS, Navigation oder Verhalten.
- Keine Soll-Architektur, Mockups, Designvarianten oder vorweggenommene
  Entscheidungen für den späteren UI-Umbau.
- Keine vollständige Liste aller Buttons, Texte, CSS-Klassen, DTOs,
  Hilfskomponenten oder Testfälle.
- Keine Kopie fachlicher Regeln aus `docs/`, der Webfrontend-Konzeption oder den
  Agentenregeln.
- Keine umfassende Neufassung von `docs/Architektur.md` außerhalb der zwingenden
  Entflechtung mit der neuen UI-Verantwortung.
- Keine Roadmap und keine Umsetzung in diesem Konzeptschritt.

## Informationsvertrag

- **Ist vor Soll:** Jede Aussage unter `docs/` ist am aktuellen Code belegbar.
- **Gesamtbild vor Detail:** Der Einstieg muss Shell, Kontext und Seitennetz ohne
  weitere Datei verständlich machen; Seitendetails werden progressiv verlinkt.
- **Stabile Aussagen:** Dokumentiert werden Verantwortung, Hauptregionen,
  Zustandsgrenzen und primäre Abläufe. Leicht veraltende Markupdetails bleiben
  im Code.
- **Eine Quelle:** Normative UI-, Fach- und Accessibility-Regeln werden nicht
  paraphrasiert, sondern an ihrer bestehenden Quelle verlinkt.
- **Scanbar:** Tabellen und Stichpunkte tragen die Fakten; Prosa wird nur für
  Grenzen oder Zusammenhänge verwendet, die daraus nicht eindeutig hervorgehen.

## Verifikation

- Jede im Webprojekt gefundene `@page`-Route erscheint genau einmal im
  Routenindex; jede dokumentierte Route und Code-/Dokureferenz löst auf.
- Shell, Hauptnavigation und globaler Wissenskontext stimmen mit den zuständigen
  Razor-Komponenten überein.
- Jeder Seitensteckbrief ist gegen seine routbare Razor-Komponente und mindestens
  einen repräsentativen Komponenten- oder Browser-Smoke abgeglichen.
- Die Doku unterscheidet sichtbar zwischen implementiertem Ist-Zustand und
  Planung; sie enthält keine Redesign-Aussage und verwendet Planungsartefakte
  nicht als Ist-Nachweis.
- `WebUi.md` und `Architektur.md` haben nach der Entflechtung keine konkurrierende
  normative Beschreibung derselben sichtbaren UI-Verantwortung.
- `docs/README.md` und die UI-Regel machen die neue Quelle auffindbar und
  verbindlich, ohne deren Inhalt zu duplizieren.
- Für das reine Dokumentations-/Regeldelta genügen Link-/Diff-Prüfung und
  `git diff --check`; Build und Tests sind nicht erforderlich.

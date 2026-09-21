# Manuelle UI-Abnahme

Diese Checkliste richtet sich an Menschen und ergänzt die automatisierten
Browser-Smokes aus `KnowHowToAI.BrowserTests` um die Punkte, die sich nicht
zuverlässig automatisieren lassen – vor allem echten Browserzoom und die
wahrgenommene Sichtbarkeit des Fokus. Sie ist eine Entwicklungsmaßnahme nach
WCAG 2.2 AA und behauptet keine formale WCAG-Zertifizierung. Der Agent führt
sie nicht interaktiv aus.

## Prüfumgebung

- Google Chrome Stable Desktop (installierte aktuelle Version), sonstige
  Browsererweiterungen deaktiviert.
- Fenstergröße etwa 1280 × 720; die Zoomstufen werden über das
  Chrome-Zoommenü (Strg + `+` / Strg + `-`) eingestellt, nicht über die
  Bildschirmauflösung.

## Zoom- und Reflow-Durchlauf

| Zoomstufe | Wirksame Layoutbreite | Erwartung |
|---|---|---|
| 100 % | 1280 CSS-Pixel | Kopf, Navigation, Arbeitsfläche nebeneinander; kein horizontaler Scrollbalken |
| 200 % | 640 CSS-Pixel | Inhalt reflowt einspaltig; kein horizontaler Scrollbalken bei normalem Content; alle Texte und Aktionen erreichbar |
| 400 % | 320 CSS-Pixel | wie 200 %; fachlich zweidimensionale Flächen scrollen nur in ihrem eigenen Bereich |

Bei jeder Stufe prüfen:

1. Es erscheint kein ungewollter horizontaler Seiten-Scrollbalken bei
   normalem Inhalt; zweidimensionale Flächen scrollen nur in sich selbst.
2. Alle Texte und Aktionen bleiben sichtbar und bedienbar; nichts wird
   abgeschnitten oder überdeckt.
3. Der Zoom-/Reflow-Nachweis ist keine Smartphonefreigabe; die Shell
   unterstützt unterhalb von 1024 CSS-Pixeln bewusst nur Zoom und Reflow.

Der Menübutton bleibt als kompakter quadratischer Drei-Linien-Button oben links
in der App-Leiste sichtbar. Sein zugänglicher Name beziehungsweise Tooltip
lautet zustandsabhängig „Navigation einblenden“ oder „Navigation ausblenden“.
Auf dem Desktop startet die Navigation offen; nach dem Schließen muss derselbe
Button die Navigation wieder öffnen. In kompakter Breite erscheint die
Navigation als Drawer/Overlay.

## Kurze Tastaturschritte (in jeder Zoomstufe gleich)

Ausgangspunkt ist jeweils der Seitengrundzustand nach dem Laden von `/`
(Startseite der Shell):

1. Tab – der Skip-Link „Zum Hauptinhalt springen“ erhält den Fokus und wird
   sichtbar; der Fokusring bleibt während der gesamten Folge sichtbar.
2. Enter – der Fokus springt auf den Hauptinhalt.
3. Umschalt + Tab beziehungsweise der sichtbare App-Leisten-Button –
   der Fokus erreicht den Kopfbutton „Navigation einblenden“.
4. Enter – der Navigationsbereich öffnet sich; der Fokus liegt auf dem
   Navigationsbereich.
5. Tab – der Start-Link; die Reihenfolge ist logisch und ohne Tabfalle.
6. Escape – der Bereich schließt sich; der Fokus kehrt zum Auslöser zurück.
7. Den Dialog- und Toast-Tastaturvertrag mit dem jeweils ersten echten
   Verbraucher prüfen (Öffnen, Fokusfalle, Escape entspricht Abbrechen,
   Fokusrückgabe; Toast pausier-/schließbar, Fokus wird nicht verschoben).
8. Statusmeldungen (Warnung, Fehler, Erfolg, ungespeichert) nie nur an der
   Farbe erkennen: Icon und Text bleiben lesbar.

Die Schritte 1–6 entsprechen den automatisierten Smokes
(`LayoutShellSmokeTests`, `ResponsiveShellSmokeTests`); Abweichungen zwischen
manueller Beobachtung und Smokes sind vor dem nächsten Slice zu klären.

## Nicht Teil dieser Checkliste

- Screenreader-Durchläufe und Plattform-Vergrößerungen (eigenes späteres
  Planungsgate).
- Automatisierte WCAG-Scans; der Umfang der automatisierten Nachweise ist im
  jeweiligen Roadmap-Task festgelegt.

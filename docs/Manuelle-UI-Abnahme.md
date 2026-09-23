# Manuelle UI-Abnahme

Diese Checkliste richtet sich an Menschen und ergänzt die automatisierten
Browser-Smokes aus `KnowHowToAI.BrowserTests` um echten Browserzoom und
visuelle Reflow-Prüfung. Der Agent führt sie nicht interaktiv aus.
Tastaturnavigation und Fokuswahrnehmung sind keine Abnahmepunkte.

## Prüfumgebung

- Google Chrome Stable Desktop (installierte aktuelle Version), sonstige
  Browsererweiterungen deaktiviert.
- Fenstergröße etwa 1280 × 720; die Zoomstufen werden über das
  Chrome-Zoommenü eingestellt, nicht über die
  Bildschirmauflösung.

## Zoom- und Reflow-Durchlauf

| Zoomstufe | Wirksame Layoutbreite | Erwartung |
|---|---|---|
| 100 % | 1280 CSS-Pixel | Kopf, Navigation, Arbeitsfläche nebeneinander; kein horizontaler Scrollbalken |
| 200 % | 640 CSS-Pixel | Inhalt reflowt einspaltig; kein horizontaler Scrollbalken bei normalem Content; alle Texte und Aktionen erreichbar |
| 400 % | 320 CSS-Pixel | wie 200 %; fachlich zweidimensionale Flächen nutzen nur in ihrem eigenen Bereich den Bildlauf |

Bei jeder Stufe prüfen:

1. Es erscheint kein ungewollter horizontaler Seiten-Scrollbalken bei
   normalem Inhalt; zweidimensionale Flächen nutzen nur in sich selbst den Bildlauf.
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

Statusmeldungen (Warnung, Fehler, Erfolg, ungespeichert) dürfen nicht nur an
der Farbe erkennbar sein: Icon und Text bleiben lesbar.

## Nicht Teil dieser Checkliste

- Screenreader-Durchläufe und Plattform-Vergrößerungen (eigenes späteres
  Planungsgate).
- Tastatur- und Fokuswahrnehmungsprüfungen sowie automatisierte WCAG-Scans.
  Der Umfang automatisierter Nachweise steht im jeweiligen Roadmap-Task.

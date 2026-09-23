# Manuelle UI-Abnahme

Diese Checkliste richtet sich an Menschen und ergänzt die automatisierten
Browser-Smokes aus `KnowHowToAI.BrowserTests` um die visuelle Prüfung der
Desktopansichten mit Maus. Der Agent führt sie nicht interaktiv aus.

## Prüfumgebung

- Google Chrome Stable Desktop (installierte aktuelle Version), sonstige
  Browsererweiterungen deaktiviert.
- Primär 1280 × 800 CSS-Pixel, sekundär 1024 × 720 CSS-Pixel. Die Fenstergröße
  wird so eingestellt, dass der jeweilige CSS-Viewport erreicht wird.

## Desktop-Durchlauf

| Viewport | Erwartung |
|---|---|
| 1280 × 800 CSS-Pixel | Kopf, Navigation und Arbeitsfläche sind wie vorgesehen angeordnet; kein horizontaler Seiten-Scrollbalken |
| 1024 × 720 CSS-Pixel | Shell-Zustand, Seiteninhalt und Aktionen bleiben sichtbar und bedienbar; kein horizontaler Seiten-Scrollbalken |

Bei beiden Viewports prüfen:

1. Es erscheint kein ungewollter horizontaler Seiten-Scrollbalken bei
   normalem Inhalt; zweidimensionale Flächen nutzen nur in sich selbst den Bildlauf.
2. Alle Texte und Aktionen bleiben sichtbar und bedienbar; nichts wird
   abgeschnitten oder überdeckt.
3. Der Mausablauf bleibt im Wissensbaum sowie in den relevanten Formularen und
   Aktionsbereichen erreichbar.

Der Menübutton bleibt als kompakter quadratischer Drei-Linien-Button oben links
in der App-Leiste sichtbar. Sein zugänglicher Name beziehungsweise Tooltip
lautet zustandsabhängig „Navigation einblenden“ oder „Navigation ausblenden“.
Auf dem Desktop startet die Navigation offen; nach dem Schließen muss derselbe
Button die Navigation wieder öffnen. Bei 1024 CSS-Pixeln ist der dazugehörige
Shell-Zustand ebenfalls zu prüfen.

Statusmeldungen (Warnung, Fehler, Erfolg, ungespeichert) dürfen nicht nur an
der Farbe erkennbar sein: Icon und Text bleiben lesbar.

## Nicht Teil dieser Checkliste

- Screenreader-Durchläufe und Plattform-Vergrößerungen (eigenes späteres
  Planungsgate).
- Tastatur- und Fokusverhalten sowie automatisierte WCAG-Scans.
  Der Umfang weiterer Nachweise steht im jeweiligen Roadmap-Task.

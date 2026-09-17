# Publikation und PDF

Priorität: niedrig.

## Ziel

Die UI exportiert den aktuell ausgewählten Node einschließlich seines gesamten Teilbaums als PDF.

- Auswahl des Root-Nodes exportiert die gesamte Wissenshierarchie.
- Auswahl eines inneren Nodes exportiert nur diesen Node und seine Nachfahren.
- Export verwendet den aktuell gewählten Lesekontext und die angefragte Rolle.
- Rollen-Fallback und Strukturregeln entsprechen dem bestehenden `export_tree`-Verhalten.
- Content wird unverändert exportiert. TODO-Texte sind normaler Inhalt und erscheinen im PDF.

## Genau ein Template

Es gibt zunächst genau ein serverseitiges PDF-Template in einem konfigurierten Ordner, beispielsweise:

```text
templates/pdf/default/
├─ template.html
├─ document.css
├─ logo.svg
└─ fonts/
```

- Keine Profilverwaltung in der UI.
- Keine Auswahl mehrerer Templates oder Kundenprofile.
- Template, CSS, Logo und Fonts werden als Deploymentdateien gepflegt.
- Der Server validiert beim Start, dass die benötigten Dateien und Werkzeuge vorhanden sind.

## Technische Pipeline

```text
ausgewählter Node + Rolle + Read Context
  → export_tree als Markdown
  → Pandoc mit HTML-Template und CSS
  → WeasyPrint als PDF-Engine
  → PDF-Download
```

Pandoc wird mit WeasyPrint als PDF-Engine verwendet, sinngemäß:

```text
pandoc --pdf-engine=weasyprint ...
```

- Der erste PDF-Stand benötigt noch keine verwalteten Content-Bilder.
- Nach dem späteren Asset-Milestone werden Bildreferenzen vor der Konvertierung über die zentrale Asset-Auflösung bereitgestellt.
- Pandoc- und WeasyPrint-Prozesse erhalten Timeout, kontrollierte Arbeitsverzeichnisse und begrenzten Zugriff auf lokale oder externe Ressourcen.
- Fehlerausgabe wird diagnostisch protokolliert, aber nicht ungefiltert an den Browser gegeben.

## UI-Ablauf

1. Benutzer steht auf einem Node und wählt Rolle sowie Lesekontext.
2. Benutzer klickt `PDF-Export`.
3. Server erzeugt das PDF.
4. Browser lädt die Datei als `application/pdf` herunter.

Für den ersten Stand genügt ein Busy-/Progress-Indikator ohne Prozentwert. Ein persistierter Hintergrundjob mit echtem Fortschritt wird erst eingeführt, wenn reale Exportzeiten ihn rechtfertigen.

## Kein initialer Publikationsworkflow

Nicht Bestandteil des ersten PDF-Exports:

- Mehrere Publikationsprofile.
- UI zur Template- oder Profilverwaltung.
- Versionierte Profilkonfiguration.
- Freigabe-, Stale-, Finding- oder TODO-Policies.
- Gespeicherte Exporthistorie.
- Alternative Ausgabeformate wie DOCX.
- Garantie byteidentischer Wiederholung nach einer Templateänderung.

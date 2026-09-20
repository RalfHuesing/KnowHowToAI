# Audience-Hard-Cut

Dieses eigenständige Vorhaben stellt das fachliche Adressatenmodell im gesamten
Repository auf den technischen Begriff `Audience` und in der deutschen
Benutzeroberfläche auf „Zielgruppe“ um. Es ist ein Greenfield-Hard-Cut ohne
Legacy-Aliasse, Doppelverträge oder Kompatibilitätsrouten.

## Verbindlicher Einstieg

- [M1 – Repositoryweiter Audience-Hard-Cut](roadmap/01-repositoryweiter-audience-hard-cut/roadmap.md)
- [Abgeschlossene Planung und Begriffsvertrag](roadmap/01-repositoryweiter-audience-hard-cut/planning.md)
- [Vorbereiteter Abschlussaudit](roadmap/01-repositoryweiter-audience-hard-cut/audit.md)

## Ausführung

- Die Planung ist abgeschlossen; alle Leaf-Tasks sind zur sequenziellen
  Ausführung freigegeben.
- Ein Ausführungsagent bearbeitet genau einen Leaf-Task einschließlich seiner
  Dokumentationsfolgen, Nachweise, Roadmap-Checkbox und seines atomaren Commits.
- Während M1 wird kein anderes Vorhaben implementiert, das dieselben Verträge,
  Pfade oder Planungsartefakte berührt. Insbesondere die noch offenen
  Webfrontend- und UI/UX-Tasks pausieren bis M1.4 abgeschlossen ist.
- Die Umsetzung startet ausschließlich manuell durch den Benutzer. Dieses
  Planungsartefakt startet keinen Implementierungsagenten.

## Abgrenzung

Das Vorhaben ändert ausschließlich Terminologie und die davon abgeleiteten
öffentlichen Namen. Fachliche Semantik, Snapshot- und Transaktionsmodell,
Fallback, Provenienz, Freshness, Berechtigungsmodell und UI-Funktionsumfang
bleiben unverändert.

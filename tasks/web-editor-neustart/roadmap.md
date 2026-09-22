# Roadmap: Web-Fundament und Editor

Verbindliches Ziel: [Konzept](Konzept.md) (`status: ready`). Die Reihenfolge
ist seriell; jeder Leaf ist eine eigene Agent-Session mit eigenem grünen
Abschluss. `docs/` wird nur mit implementiertem Ist-Verhalten geändert.
Eine alte Web-Route darf während des Umbaus vorübergehend bestehen, im
Endzustand nicht. Der Nutzer startet Schritt 3 gesondert.

- [ ] [M1 – Arbeits- und Seitenfundament](roadmap/01-fundament/roadmap.md):
  Planungsregeln konsistent, Entwurfs-Schreibweg und gemeinsame Shell samt
  echten Entwurfsrouten verfügbar; Milestone-Audit bestanden.
- [ ] [M2 – Wissensarbeitsplatz](roadmap/02-wissensarbeitsplatz/roadmap.md):
  großer Baum, Node-Dokument, direkte Edits und Strukturaktionen nutzbar;
  Milestone-Audit bestanden.
- [ ] [M3 – Hard Cut und Gesamtabnahme](roadmap/03-hard-cut/roadmap.md):
  alte Web-Flächen entfernt, Dokumentation aktuell und alle vereinbarten
  Browserabläufe belegt; Milestone-Audit bestanden.

Parent-Checkboxen sind Aggregate; erst nach allen Leaf-Checkboxen und dem
jeweiligen Audit schließen. Keine parallelen Schreib-Slices im gemeinsamen
Worktree. Für jeden Code-Slice gelten die Quality-Gates aus
[Richtlinien](../../.agents/rules/Richtlinien.mdc); der jeweilige Leaf nennt
sein zusätzliches risikogerechtes Testbudget.

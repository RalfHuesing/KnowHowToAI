# M3 – Hard Cut und Gesamtabnahme

## Ziel und Abhängigkeiten

Voraussetzung: M2 abgenommen. Ergebnis: Nur der vereinbarte Web-Umfang
bleibt, während Core, Storage, MCP und ihre Verträge unberührt sind. Die
Ist-Dokumentation beschreibt danach die tatsächlich gebaute Oberfläche.

## Reihenfolge

- [x] [M3.1-T1 – Alte Web-Flächen und Export entfernen](tasks/M3.1-T1.md)
- [ ] [M3.2-T1 – Routeübergreifende Browser-Abnahme und Ist-Doku](tasks/M3.2-T1.md)

## Milestone-Abnahme

- [x] `/` führt nach `/knowledge`; nur `/knowledge[/NodeId]` und
      `/drafts[/TransactionId]` sind produktive Web-Seiten. Kein verstecktes
      Dashboard, keine Suche, Historie, Audience-Verwaltung oder Web-Export.
- [x] `docs/` und Web-Regeln nennen die implementierten Routen und den
      aktuellen Layoutvertrag; MCP-/Core-Tests bleiben grün.
- [ ] Begrenzter M3-Audit gegen Konzept, Regeln, Diff und Nachweise
      dokumentiert; Parent-Checkboxen erst danach schließen.

Audit-Ergebnis: automatisierte Kriterien bestanden; menschliches Chrome-Zoom- und Fokuswahrnehmungs-Gate offen.

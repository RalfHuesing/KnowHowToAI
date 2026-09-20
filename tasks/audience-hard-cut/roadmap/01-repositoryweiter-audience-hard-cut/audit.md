# M1 – Vorbereiteter Abschlussaudit

Status: offen; erst nach M1.1–M1.5 durchführen. Dies ist ein manueller
Milestone-Audit und kein delegierbarer Implementierungs-Leaf-Task.

## Prüfgrenze

Der Audit prüft ausschließlich den repositoryweiten Terminologie-Hard-Cut. Er
eröffnet keine neuen Produktfunktionen, UI-Verbesserungen oder
Architekturrefaktorierungen.

## Prüfliste

- [ ] Alle Leaf- und Parent-Checkboxen in `roadmap.md` sind aufgrund belegter
      Abnahmen geschlossen.
- [ ] Der Begriffsvertrag ist in SQL, Core, Storage, MCP, Web und Tests
      symmetrisch umgesetzt.
- [ ] Es gibt keine Alias-Typen, Compatibility Views, doppelte MCP-Tools,
      JSON-Doppelfelder, Route-Redirects oder Browserzustandsmigrationen.
- [ ] Die direkt korrigierte SQL-Baseline erzeugt nach kontrolliertem Reset ein
      reines Audience-Schema; kein Forward-Migrations- oder Aliasrest existiert.
- [ ] Sichtbare deutsche Texte verwenden „Zielgruppe“; Accessibility-Semantik
      und rollenbasierte Playwright-Locators sind nicht beschädigt.
- [ ] Ist-Dokumentation, Regeln und ausführbare Roadmaps stimmen mit dem
      implementierten Vertrag überein.
- [ ] Jeder Resttreffer der breiten Terminologiesuche ist einzeln als zulässiger
      Standard-, Migrations- oder Entscheidungsnachweis klassifiziert.
- [ ] Abschlussnachweise für Build, Solution-Linter, FastTests und vollständige
      IntegrationTests liegen vor.
- [ ] `git status` enthält keine unbeabsichtigten oder fremden Änderungen.

## Ergebnis

- Entscheidung: <offen / bestanden / eine gezielte Korrekturrunde / neue Planung>
- Geprüfte Commits: <eintragen>
- Befunde: <keine / eintragen>
- Zulässige Resttreffer: <Pfad, Zeile und Kategorie eintragen>

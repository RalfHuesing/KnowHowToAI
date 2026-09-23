# M3-Audit – Hard Cut und Gesamtabnahme

## Ergebnis

Die automatisiert belegbaren M3-Kriterien 1 und 2 sind erfüllt. Es wurden in
diesem begrenzten Audit keine weiteren umsetzungsrelevanten Findings gefunden.
Der Milestone bleibt offen, bis die menschliche Chrome-Zoom- und
Fokuswahrnehmungsprüfung aus `docs/Manuelle-UI-Abnahme.md` dokumentiert ist.
M3.2-T1 und die M3-Audit-/Parent-Checkboxen bleiben deshalb offen.

## Prüfumfang und Nachweise

- Konzept und M3.1/M3.2-Leafs gegen `.agents/rules/Richtlinien.mdc`,
  `.agents/rules/WebUiHtmlCss.mdc`, `.agents/rules/TestRichtlinien.mdc`,
  `.agents/rules/DokuRichtlinien.mdc` und `docs/Manuelle-UI-Abnahme.md`
  geprüft. Der manuelle Teil wird dort ausdrücklich dem Menschen zugewiesen.
- Aktive Razor-Routen in `src/KnowHowToAI.Server/Web/` sind `/`,
  `/knowledge`, `/knowledge/{NodeId:guid}`, `/drafts` und
  `/drafts/{TransactionId:guid}`. `KnowledgeRedirect` implementiert `/`;
  `WebEndpointRegistration` registriert Razor, Assets sowie reservierte
  NotFound-/MethodNotAllowed-Routen, keinen Markdown-Download.
- `tests/KnowHowToAI.IntegrationTests/Server/Hosting/WebHostTests.cs` prüft
  alte Search-/History-/Audience-/Transaction- und Download-URLs mit leeren
  404-Antworten. Die aktive Routensuche findet keine Dashboard- oder sonstigen
  alten UI-Seiten. Der Hard-Cut-Leaf dokumentiert, dass Core-/MCP-Funktionen
  einschließlich Suche, Historie, Zielgruppen und MCP-Markdown-Export bestehen.
- `docs/WebUi.md` dokumentiert die fünf aktiven Routen, Shell-Ownership,
  Abläufe, Exportgrenze und zugehörige Tests. Die Web-UI-Regel verweist auf
  Konzept und manuelle Abnahme und enthält die verbindlichen Layoutnachweise.
- Im M3.1-/M3.2-Abschlussnachweis sind Build, FastTests, Host-/HTTP- und
  Browser-Integration sowie Solution-Linter mit `score=10.0` und null
  Verstößen als grün dokumentiert. M3.1 weist die MCP-Toolregistrierung und
  den MCP-Markdown-Export mit 2/2 Tests nach; M3.2 weist 1.004 FastTests,
  75/75 Host-/HTTP- und 36/36 Browser-Integrationstests aus. Der Diff-Check
  ist ebenfalls als sauber dokumentiert.
- Der aktuelle Arbeitsbaum war vor dem Audit sauber; `git show --format= --check
  HEAD` war ohne Befund. Der Audit ändert ausschließlich Roadmap-Dokumentation.

## Offenes Gate

Die echte Chrome-Desktop-Zoomprüfung bei 200 % und 400 % sowie die menschliche
Wahrnehmungsprüfung der sichtbaren Tastaturfokusringe wurden nicht ausgeführt.
Screenshots, CSS-Pixel-Reflow und automatisierte Fokus-Smokes ersetzen diese
Prüfung laut `docs/Manuelle-UI-Abnahme.md` nicht. Nach dokumentiertem
menschlichem Ergebnis können M3.2-T1, die M3-Audit-Checkbox und die übergeordnete
M3-Checkbox geschlossen werden.

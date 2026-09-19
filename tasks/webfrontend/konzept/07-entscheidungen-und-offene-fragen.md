# Offene Fragen

Diese Datei enthält ausschließlich noch nicht entschiedene Punkte. Nach einer Entscheidung wird der Eintrag entfernt und das Ergebnis im fachlich zuständigen Konzeptdokument sowie bei Bedarf in den Leitentscheidungen des [Konzeptindex](../README.md#gesetzte-leitentscheidungen) festgehalten.

Gesprächsstand, Empfehlungen und Recherche: [Entscheidungsworkshop](../Entscheidungsworkshop.md).

Ein Roadmap-Task darf nicht begonnen werden, solange eine ihm zugeordnete Benutzerentscheidung offen ist. Bei Spike-Entscheidungen ist der angegebene Task selbst für die Entscheidung und ihre Dokumentation zuständig. M3 und spätere Milestones werden erst in ihrem manuellen `Mx.0`-Gate entscheidungs- und ausführungsreif geschärft.

Im aktuellen Planungshorizont M0–M2 ist keine Benutzerentscheidung mehr offen. O-001, O-003, O-002 und O-015 wurden durch M0 geschlossen; ihre Ergebnisse stehen in den fachlich zuständigen Konzepten und als K-021 bis K-025 im [Konzeptindex](../README.md#gesetzte-leitentscheidungen). Der Konsistenz- und Agentenausführbarkeitscheck der verbleibenden M1–M2-Leaf-Tasks wurde am 2026-09-18 nachgeführt.

O-008, O-007, O-025, O-026 und O-027 wurden im M3.0/M4.0-Gate am 2026-09-19 entschieden; ihre Ergebnisse stehen in [Bedienkonzept und UI](02-bedienkonzept-und-ui.md), [Projektstruktur und Codekonventionen](08-projektstruktur-und-codekonventionen.md) sowie im [Konzeptindex](../README.md#gesetzte-leitentscheidungen).

O-010, O-029, O-012, O-017, O-022, O-023, O-024, O-011, O-004 und O-016 werden nicht vorentschieden und werden im `Mx.0`-Gate ihres ersten betroffenen Milestones bearbeitet. O-005 und O-006 bleiben Entscheidungen außerhalb der aktuellen Roadmap.

| ID | Priorität | Klärung | Blockiert | Offene Frage und Startempfehlung |
|---|---|---|---|---|
| O-004 | Niedrig | Spike | M8.1-T1 | Asset-Speicher; immutable/dedupliziert, SQL-Metadaten und Binärspeicheroptionen bewerten |
| O-005 | Mittel | Benutzer, später | außerhalb dieser Roadmap | Presentation Views; kanonischen Baum behalten und Views auf denselben `NodeId`s modellieren |
| O-006 | Niedrig | Benutzer, später | außerhalb dieser Roadmap | Integrierte KI; erst nach stabilen manuellen Workflows konkretisieren |
| O-010 | Mittel | Benutzer | M5.1-T3 | Markdown-Quellmodus im ersten Contenteditor; Empfehlung: aufnehmen, weil Markdown das kanonische Format ist |
| O-011 | Niedrig | Benutzer | M7.1-T1 | PDF-Basislayout: Seitenformat, Deckblatt, Inhaltsverzeichnis, Header/Footer und Logo; Empfehlung: A4, kein Deckblatt, TOC ab zwei Ebenen, Logo im Header, Seitenzahl im Footer |
| O-012 | Hoch | Benutzer | M6.4-T1 | Zieldeployment: Betriebssystem, Prozesshost/Service, Reverse Proxy, Hostname und TLS-Terminierung; Empfehlung: Windows Service hinter IIS oder vorhandenem Unternehmensproxy |
| O-016 | Niedrig | Benutzer | M8.6-T1 | Assetgrenzen: erlaubte Bildtypen, maximale Dateigröße und Pixelzahl; Empfehlung: PNG/JPEG/WebP/SVG, 10 MiB, 40 Megapixel, SVG nur nach sicherer Sanitization |
| O-017 | Hoch | Benutzer | M6.3-T1 bis T2 | Messbare Performanceziele einschließlich Referenzdaten, gleichzeitiger Blazor-Circuits und paralleler MCP-Aufrufe; Empfehlung: 100.000 Nodes, 1.000 direkte Kinder, 20 Circuits, 10 parallele MCP-Aufrufe, P95-Serverantwort unter 500 ms und sichtbare UI-Reaktion unter 1 s im Intranet |
| O-022 | Niedrig | Benutzer in M6.0 | M6.4-T2 | Reine Betriebswahl bei bekannter Zielumgebung: produktive Prozess-/SQL-Identität und gegebenenfalls Secretbehandlung. Bis dahin bleibt die bestehende `DatabaseConnection`-Sektion einschließlich Klartext-SQL-Zugangsdaten zulässig; keine vorsorgliche Dienstkonto-, gMSA- oder Secret-Provider-Infrastruktur. |
| O-023 | Hoch | Benutzer | M6.5-T1 | Recoveryziele und Aufbewahrung für den Wissensbunker; Empfehlung: RPO höchstens 15 Minuten, RTO höchstens 4 Stunden, tägliches Full Backup plus Transaktionslog-Backups, 30 tägliche und 12 monatliche Wiederherstellungspunkte |
| O-024 | Mittel | Benutzer | M6.4-T2 | Betriebsbeobachtung: Logziel/-aufbewahrung, Liveness/Readiness, Alarmierung und verantwortliche Stelle; Empfehlung: strukturierte Rolling Files für 30 Tage, kritische Start-/Betriebsfehler zusätzlich ins Windows Event Log, schmale `/health/live`- und `/health/ready`-Endpunkte, zunächst kein Metriksystem |
| O-029 | Mittel | Benutzer im M5.0-Gate | M5.1-T1 bis T2 | Wie werden Milkdown `@milkdown/crepe` und sein npm-Closure reproduzierbar in lokale, selbst gehostete Browserassets gebaut: Vite, esbuild oder eine andere Toolchain? M0 wählte den Editor, aber ausdrücklich keine Produkt-Buildtoolchain. Das M5.0-Gate muss genau eine Variante, Lockfile, Restore-/Buildbefehl, Outputpfad und CI-/Lizenzintegration festlegen; die aufgelösten Werkzeugversionen stehen im Lockfile. |

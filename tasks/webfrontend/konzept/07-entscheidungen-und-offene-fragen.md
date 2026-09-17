# Offene Fragen

Diese Datei enthält ausschließlich noch nicht entschiedene Punkte. Nach einer Entscheidung wird der Eintrag entfernt und das Ergebnis im fachlich zuständigen Konzeptdokument sowie bei Bedarf in den Leitentscheidungen des [Konzeptindex](../README.md#gesetzte-leitentscheidungen) festgehalten.

Ein Roadmap-Task darf nicht begonnen werden, solange eine ihm zugeordnete Benutzerentscheidung offen ist. Bei Spike-Entscheidungen ist der angegebene Task selbst für die Entscheidung und ihre Dokumentation zuständig.

| ID | Priorität | Klärung | Blockiert | Offene Frage und Startempfehlung |
|---|---|---|---|---|
| O-001 | Hoch | Spike | M0.3-T1 | UI-Komponentenpaket; einheitliches, aktiv gepflegtes Blazor-Paket anhand der dokumentierten Kriterien auswählen |
| O-002 | Hoch | Spike | M0.3-T3 | Rich-Text-Komponente; Markdown-nativ, verlustarmer Roundtrip, deaktivierbare Headings und spätere Upload-Hooks |
| O-003 | Hoch | Spike | M0.3-T2 | Baumkomponente; Lazy Loading, Virtualisierung, Drag-and-drop und Tastaturbedienung |
| O-004 | Niedrig | Spike | M8.1-T1 | Asset-Speicher; immutable/dedupliziert, SQL-Metadaten und Binärspeicheroptionen bewerten |
| O-005 | Mittel | Benutzer, später | außerhalb dieser Roadmap | Presentation Views; kanonischen Baum behalten und Views auf denselben `NodeId`s modellieren |
| O-006 | Niedrig | Benutzer, später | außerhalb dieser Roadmap | Integrierte KI; erst nach stabilen manuellen Workflows konkretisieren |
| O-007 | Hoch | Benutzer | M4.1-T1 | Transaction-`Actor` ohne Auth: freie Eingabe, Serverkonfiguration oder leer; Empfehlung: beim Start frei eingebbar und danach immutable |
| O-008 | Hoch | Benutzer | M3.4-T1 | Initiale Content-Rolle: explizite Pflichtauswahl oder automatisch gemerkte letzte Rolle; Empfehlung: keine stille Rolle, letzte Auswahl nur pro Browsertab merken |
| O-009 | Hoch | Benutzer | M2.1-T2 | Branding: Produktname, Logo, Primärfarbe und gewünschte Standardschrift; Empfehlung ohne Vorgabe: Textlogo „KnowHowTo AI“, neutrales Blau und Systemschrift |
| O-010 | Mittel | Benutzer | M5.1-T3 | Markdown-Quellmodus im ersten Contenteditor; Empfehlung: aufnehmen, weil Markdown das kanonische Format ist |
| O-011 | Niedrig | Benutzer | M7.1-T1 | PDF-Basislayout: Seitenformat, Deckblatt, Inhaltsverzeichnis, Header/Footer und Logo; Empfehlung: A4, kein Deckblatt, TOC ab zwei Ebenen, Logo im Header, Seitenzahl im Footer |
| O-012 | Hoch | Benutzer | M6.4-T1 | Zieldeployment: Betriebssystem, Prozesshost/Service, Reverse Proxy, Hostname und TLS-Terminierung; Empfehlung: Windows Service hinter IIS oder vorhandenem Unternehmensproxy |
| O-013 | Hoch | Benutzer | M0.3-T1 bis T3 | Unterstützte Browser, Versionen und Viewports; Empfehlung: aktuelle und vorherige Hauptversion von Edge/Chrome, volle Bearbeitung ab 1280×720, lesbare reduzierte Ansicht ab 1024 px, keine mobile Optimierung |
| O-014 | Hoch | Benutzer | M2.1-T2 | UI-Sprache und Lokalisierung; Empfehlung: zunächst ausschließlich Deutsch, Texte dennoch zentral und nicht in Fachlogik verteilen |
| O-015 | Mittel | Spike | M0.3-T4 | Testwerkzeuge für Razor-Komponenten und Browser-E2E; Empfehlung: bUnit und Microsoft Playwright nach Kompatibilitätsprüfung |
| O-016 | Niedrig | Benutzer | M8.6-T1 | Assetgrenzen: erlaubte Bildtypen, maximale Dateigröße und Pixelzahl; Empfehlung: PNG/JPEG/WebP/SVG, 10 MiB, 40 Megapixel, SVG nur nach sicherer Sanitization |
| O-017 | Hoch | Benutzer | M6.3-T1 bis T2 | Messbare Performanceziele und Referenzdatenmenge für Baum, Suche und Nodewechsel; Empfehlung: 100.000 Nodes, 1.000 direkte Kinder, P95-Serverantwort unter 500 ms und sichtbare UI-Reaktion unter 1 s im Intranet |
| O-018 | Hoch | Benutzer | M0.3-T1 bis T3 | Dürfen kommerzielle UI-/Tree-/Editor-Komponenten beschafft werden und welches Budget/Lizenzmodell gilt; Empfehlung: vorhandene Firmenlizenzen nutzen, sonst OSS oder kostenfreie kommerzielle Nutzung bevorzugen |
| O-019 | Hoch | Benutzer | M1.3-T3 | Verbindliche MCP-Zielclients und Versionen für die HTTP-Abnahme; Empfehlung: jeden tatsächlich täglich eingesetzten Client mindestens mit Tool Discovery, Read und vollständiger Transaction prüfen |

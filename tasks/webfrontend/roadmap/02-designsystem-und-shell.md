# M2 – Designsystem und Anwendungsshell

[Roadmap-Index](../Roadmap.md)

- [ ] **M2 abschließen**

Abhängigkeit: [M1](01-webhost-und-mcp-http.md)

Verbindliche M0-Basis: keine allgemeine UI-Bibliothek. M2 verwendet natives Blazor, semantisches HTML, eigenes CSS und nur schmale lokale JS-Isolation für Funktionen wie den nativen `dialog`. Es findet keine erneute Komponenten- oder Testwerkzeugsuche statt.

Ziel: Alle Fachfeatures erhalten eine konsistente, moderne und belastbare UI-Grundlage.

Referenzen: [Visueller Stil](../konzept/02-bedienkonzept-und-ui.md#visueller-stil), [Grundlayout](../konzept/02-bedienkonzept-und-ui.md#grundlayout), [Blazor-Betrieb](../konzept/06-betrieb-sicherheit-und-risiken.md#blazor-betrieb)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M2.1 – Komponentenbasis und Theme

- [x] **M2.1 abschließen**

  - [x] **M2.1-T1 – Native UI-Basis implementieren**
    - Voraussetzung: `KnowHowToAI.Web.Tests` verwendet bUnit mit xUnit v3; `KnowHowToAI.BrowserTests` verwendet Microsoft.Playwright .NET und die installierte aktuelle Google-Chrome-Stable-Version ausschließlich headless mit `Channel = "chrome"`.
    - Integration: Blazor-/HTML-Komponenten direkt implementieren; keine allgemeine UI-Paketreferenz, keine Suite-Services und kein leeres Wrapperframework hinzufügen. Lokales CSS und nur tatsächlich benötigte schmale JS-Isolation in `App.razor` einbinden. Keine CDN-, Cloud-, Telemetrie- oder Laufzeit-Downloadabhängigkeit.
    - Dialog: nativen HTML-`dialog` über einen lokalen JS-Isolationsbaustein mit `showModal()`, Fokusfalle, `Escape` und Fokusrückgabe kapseln; der Baustein enthält keine allgemeine Zustandsmaschine.
    - Testprojekt: Ein nur in `KnowHowToAI.Web.Tests` gerendertes Showcase-Fixture belegt beschriftetes Formular samt Validierung, Button, Dialog, kleine Tabelle, Inlinehinweis und Toast; es entsteht keine öffentliche Demo-/Showcase-Route im Produkt.
    - Ressourcen: produktiv nur tatsächlich für M2 benötigte eigene Assets übernehmen; Publishgröße von `wwwroot` und Gesamtpublish gegen M1 protokollieren. `THIRD-PARTY-NOTICES.md` nur ändern, wenn der tatsächliche Restore neue Abhängigkeiten enthält.
    - Nicht enthalten: Knowledge Tree und Rich-Text-Editor.
    - Tests: bUnit-Render-/Interaktionssmoke; Playwright-Dialogfolge `Enter`, `Tab`, `Shift+Tab`, `Escape` einschließlich sichtbarem Fokus und Fokusrückgabe; echter Hoststart und Netzwerkassertion, dass beim Laden keine Drittanbieter-Origin angefordert wird.
    - Abnahme: das Showcase-Fixture rendert und bedient alle genannten Basiskomponenten; der Dialogvertrag ist per Tastatur belegt; die Produktionsshell startet ohne allgemeine Komponentenbibliothek, externe Ressourcen oder Demooberfläche.

  - [x] **M2.1-T2 – Design-Tokens und Business-Theme definieren**
    - Ablage: globale Tokens ausschließlich als CSS Custom Properties in `wwwroot/css/app.css`; komponentenspezifische Werte bleiben in scoped CSS und referenzieren die globalen Tokens. Es gibt kein zweites Bibliotheks-Theme.
    - Startwerte Farben: Primary `#2563EB`, Primary Hover `#1D4ED8`, Primary Active `#1E40AF`, Text `#111827`, Secondary Text `#4B5563`, Page `#F8FAFC`, Surface `#FFFFFF`, Border `#CBD5E1`, Success `#15803D` auf `#F0FDF4`, Warning `#B45309` auf `#FFFBEB`, Danger `#B91C1C` auf `#FEF2F2` und Info/Primary auf `#EFF6FF`. Status besitzt zusätzlich Icon und Text.
    - Startwerte Maße: `4/8/12/16/24/32px`-Abstandsskala, Radien `4/8px`, Surface-Schatten `0 1px 2px rgb(15 23 42 / 0.08)`, Basistext `16px` mit `1.5` Zeilenhöhe und die Systemschriftkette `Segoe UI, Arial, sans-serif`. Fokus ist ein mindestens `3px` starker Ring in Primary mit `2px` Abstand und wird nicht per `outline: none` entfernt.
    - Icons: ausschließlich kleine repo-eigene SVGs. Dekorative Icons sind für Assistenztechnik verborgen; alleinstehende Iconbuttons erhalten einen zugänglichen Namen. Kein eigenes Logo- oder Iconfont-Asset.
    - Branding: `KnowHowToAI` in der Shell als reine Textwortmarke darstellen; kein Dummy-/Bildlogo und kein Favicon-Zwang.
    - Sprache: ausschließlich deutsche UI ohne Lokalisierungsinfrastruktur. Einmalige Texte bleiben featurelokal; nur tatsächlich gemeinsam verwendete Bezeichnungen und Fehlermappings werden zentralisiert.
    - Ausschluss: kein Dummy-/Bildlogo, Webfont, Font-CDN, `.resx`, `IStringLocalizer` oder globale Sammlung aller UI-Texte.
    - Zustände: neutral, aktiv, Erfolg, Warnung, Fehler, deaktiviert und ungespeichert.
    - Tests: feste Tokenwerte per Stylesheet-/Markup-Test, berechnete Kontraste für Text, Status und Fokus sowie Showcase-Screenshot im Light Theme. Kein Dark Theme anlegen.
    - Abnahme: Theme ist zentral definiert; Komponenten enthalten keine verstreuten globalen Designwerte und alle festgelegten Kontrastpaare bestehen.

## M2.2 – Anwendungsshell

- [x] **M2.2 abschließen**

  - [x] **M2.2-T1 – Hauptlayout und Navigation implementieren**
    - Komponenten: `MainLayout`, `PrimaryNavigation`, `BreadcrumbRegion`, `PageActions` und `ContextPanel` unter `Web/Components/Layout`. RenderFragments/Parameter bilden Seiteninhalt, Breadcrumbs, Aktionen und Kontext ab; keine Featurekomponente kennt das CSS-Seitenraster.
    - Sichtbarer M2-Inhalt: Wortmarke und genau der vorhandene Start-Link. Noch nicht implementierte M3+-Routen, leere Menüpunkte und fachliche Beispieldaten werden nicht angezeigt. Leere Breadcrumb-/Aktions-/Kontextbereiche belegen keinen Platz und benötigen keine Dummytexte.
    - Desktoplayout: ab 1280 × 720 stehen Navigation, Arbeitsfläche und optionaler Kontextbereich nebeneinander; Arbeitsfläche darf nicht unter `min-width`-Defaults überlaufen. Bei 1024 × 720 werden Seitenbereiche über klar beschriftete Buttons ein-/ausgeklappt, während alle Funktionen erreichbar bleiben. Unterhalb 1024 besteht nur die Zoom-/Reflow-Anforderung aus O-021, keine Smartphone-Navigation.
    - Semantik und Scrollen: Skip-Link, `header`, `nav`, genau ein `main` und optionales `aside`; aussagekräftige Landmark-Namen. Seite und längere Inhaltsbereiche bleiben per Tastatur scrollbar, ohne verschachtelte Scrollfalle oder fixierte Höhe für normalen Content.
    - Fokus: Öffnen eines Seitenbereichs setzt Fokus auf dessen Überschrift/erstes Bedienelement; Schließen gibt ihn an den Auslöser zurück. Escape schließt nur den zuletzt geöffneten überlagernden Bereich.
    - Nicht enthalten: fachliche Dashboard-, Baum- oder Editorimplementierung.
    - Tests: Layout- und Landmark-Assertions, Tab-/Escape-Sequenz, Fokusübergabe sowie Überlaufsmokes für 1280 × 720 und 1024 × 720 mit langem Testinhalt.
    - Abnahme: spätere Fachseiten können Inhalt, Breadcrumbs, Aktionen und Kontext ohne eigenes Seitenraster einhängen; keine tote Navigation oder Platzhalterfachlichkeit ist sichtbar.

  - [x] **M2.2-T2 – Globale Wissenskontextleiste implementieren**
    - Modell: genau ein immutable `KnowledgeContextViewModel` im Layoutbereich mit Read-Context-Art (`Current`, `Snapshot`, `Transaction`, `Release`), optionaler ID/Bezeichnung, optionaler Rolle und `IsDirty`. Keine Domain-Typen direkt im Markup und kein vorgezogener `WorkspaceState` aus M3.
    - M2-Initialzustand: aus dem tatsächlichen Seitenkontext `Current`, keine ausgewählte Rolle und `IsDirty = false` mappen. Sichtbare Texte: „Current Snapshot“, „Keine Rolle ausgewählt“ und nur bei `IsDirty` „Ungespeicherte Änderungen“. Es werden keine Dummy-IDs oder erfundenen Serverdaten angezeigt.
    - Darstellung: Kontextleiste global nahe der Wortmarke; Werte als Text/Status, noch ohne Selektor, Links oder Mutation. Fehlende Rolle wird neutral und nicht als technischer Fehler dargestellt.
    - Tests: alle vier Read-Context-Arten, Rolle gesetzt/nicht gesetzt und dirty/clean als parameterisierte Komponententests; zugängliche Gruppierung und keine reine Farbcodierung.
    - Abnahme: jede Seite kann denselben expliziten ViewModelvertrag liefern; Arbeitsstand, Rolle und Dirty-State besitzen genau eine globale Darstellung.

## M2.3 – Wiederverwendbare UI-Zustände

- [ ] **M2.3 abschließen**

  - [ ] **M2.3-T1 – Lade-, Leer- und Fehlerzustände bereitstellen**
    - Komponenten: `LoadingState` für Initial Load, `BusyOverlay` für Teilaktualisierung, `EmptyState`, `NotFoundState` und `TechnicalErrorState` unter `Web/Components/Shared`. Sie erhalten nur Anzeigeparameter und optional einen `Retry`-Callback; keine Application-Aufrufe oder globale Zustandsmaschine.
    - Verträge: Initial Load verwendet wahrnehmbaren Status ohne endlose leere Fläche; Busy Overlay verdeckt Inhalt nicht vollständig und sperrt nur die konkret laufende Aktion; Empty und Not Found sind fachlich getrennt; Technical Error zeigt neutralen deutschen Text, optionale Correlation-ID und Retry, aber keine Exception, Pfade, SQL- oder Toolausgabe.
    - Accessibility: `aria-busy` am betroffenen Bereich, einmalige höfliche Statusmeldung, Fehler fokussierbar und mit Seitenüberschrift verknüpft. Animation respektiert `prefers-reduced-motion`.
    - Tests: jeder Zustand, Retry genau einmal, fehlende/gesetzte Correlation-ID, kein sensibles Detail, Busy-Doppelaktion blockiert und zugängliche Namen/Rollen.
    - Abnahme: Zustände sind unabhängig vom Featureinhalt wiederverwendbar; keine Fachseite muss Grunddarstellung oder Retry-Schutz neu erfinden.

  - [ ] **M2.3-T2 – Warnungs-, Bestätigungs- und Änderungszustände bereitstellen**
    - Komponenten: `InlineAlert`, `StatusBanner`, eine einzelne globale `ToastRegion`, `ConfirmationDialog` und `WorkingIndicator` nativ implementieren. Gemeinsame Bausteine entstehen nur für den hier definierten wiederverwendeten Vertrag; keine generische Control-Abstraktionsschicht.
    - Regeln: Inline Alert bleibt beim auslösenden Inhalt; Banner gilt für die Seite; Toast bestätigt nur nichtkritische abgeschlossene Aktionen und ist nie alleinige Fehler-/Warnquelle. Working/dirty wird mit Text plus Icon gezeigt, nicht nur Farbe.
    - Dialog: Titel, kurze Auswirkung, primäre Aktion und „Abbrechen“; destruktive Aktion optisch/semantisch eindeutig, aber keine Texteingabe zur Bestätigung. Beim Öffnen Fokus auf die sichere Aktion „Abbrechen“, Fokusfalle, Escape entspricht Abbrechen, Schließen gibt Fokus zurück. Während eines Requests sind beide Aktionen gegen Doppelaufruf geschützt.
    - Toast: neue Meldungen werden höflich angekündigt; Fokus wird nicht automatisch verschoben; Meldung ist pausier-/schließbar und kritische Information bleibt zusätzlich im Seitenzustand sichtbar.
    - Tests: Tastatur- und Fokusfolge, Escape/Abbrechen/Bestätigen, exakt ein Callback bei Doppelklick/-taste, alle Alertstufen, Toastankündigung und Working-Indikator.
    - Abnahme: Interaktionsmuster sind in Komponenten und `docs/`-Ist-Dokumentation eindeutig beschrieben und ohne Fachfeature testbar.

## M2.4 – Robustheit und Zugänglichkeit

- [ ] **M2.4 abschließen**

  - [ ] **M2.4-T1 – Responsive Mindestdarstellung und Tastaturnavigation absichern**
    - Prüfflächen: Root-Shell, geöffnete/geschlossene Seitenbereiche, Kontextleiste, Lade-/Fehlerzustand, Alert/Toast und Bestätigungsdialog. Keine M3-Fachseiten vorziehen.
    - Automatisierte Matrix in Headless Chrome: 1280 × 720 und 1024 × 720 bei 100 %; zusätzlich äquivalente Layoutbreiten 640 CSS-Pixel für 200-%- und 320 CSS-Pixel für 400-%-Reflow. Die beiden schmalen Prüfungen sind Zoom-/Reflow-Nachweise und keine Smartphonefreigabe; echter Browserzoom bleibt Teil der manuellen Checkliste.
    - Assertions: kein ungewollter horizontaler Seitenoverflow bei normalem Content; alle Texte/Aktionen erreichbar; fachlich zweidimensionale Testfläche scrollt nur im eigenen Bereich; Landmark-/Labelstruktur; logische Tabreihenfolge; Skip-Link; sichtbarer Fokus; Dialogfokus; Status nicht nur per Farbe; feste Tokenkontraste.
    - Tastatursequenz: Start ab Dokumentanfang, Skip-Link nutzen, Hauptnavigation durchlaufen sowie beide Seitenbereiche öffnen und schließen. Dialog- und Toast-Tastaturverträge werden in M2.3 als Komponententests geprüft und mit dem ersten echten Verbraucher im zuständigen späteren Planungsgate in den Browserlauf aufgenommen. Nur beobachtbare Zustände, keine festen Wartezeiten.
    - Manuelle Checkliste: `docs/Manuelle-UI-Abnahme.md` mit Chrome Stable Desktop, 100/200/400 % Browserzoom und denselben kurzen Tastaturschritten anlegen; Ist-Doku-Index verlinken. Die Checkliste richtet sich an Menschen, wird vom Agenten nicht interaktiv ausgeführt und behauptet keine WCAG-Zertifizierung.
    - Tests: Komponentenassertionen und die genannten gezielten Browser-Smokes; kein generischer Scanner oder weitere Accessibility-Abhängigkeit allein für diesen Task.
    - Abnahme: Kernnavigation und Shellzustände sind ohne Maus bedienbar; Fokus bleibt sichtbar, Inhalt bleibt bei Mindestbreite und Desktop-Zoom erreichbar. Der Agent startet keinen interaktiven Browser und behauptet keine formale WCAG-Zertifizierung.

  - [ ] **M2.4-T2 – Reconnect- und Circuit-Verlust-Oberfläche implementieren**
    - Umsetzung: die offizielle .NET-10-Blazor-Reconnect-Oberfläche/-Hooks verwenden und nur deren Markup/Styling kontrolliert anpassen; keine eigene parallele SignalR-Verbindung und keine unbegrenzte Retryschleife.
    - Zustände und Texte: „Verbindung wird wiederhergestellt …“ während Retry, „Verbindung getrennt“ mit „Erneut versuchen“ nach vorläufigem Fehlschlag sowie „Sitzung nicht mehr verfügbar“ mit „Seite neu laden“ bei abgelehntem/abgelaufenem Circuit. Status wird als Text plus Icon angekündigt.
    - Verhalten: Overlay blockiert nur unsichere Interaktion, behält die letzte Ansicht sichtbar und hat deterministischen Fokus. Erfolgreicher Reconnect schließt es ohne fachlichen Erfolgshinweis. Reload warnt nur bei tatsächlich gesetztem `IsDirty`; eine persistierte Transaction wird nicht als ungespeichert bezeichnet.
    - Tests: Komponenten-/Markupzustände; Browserintegration mit kontrolliert unterbrochener Circuit-Verbindung für Reconnecting und erfolgreichen Reconnect; Hostneustart/abgelaufener Circuit für Reload-Pfad. Auf UI-Zustand warten, keine Sleep-Zeiten und kein sichtbarer Browser.
    - Abnahme: Benutzer erkennt Zustand und sichere nächste Aktion; nach erfolgreichem Reconnect funktioniert der lokale M1-Interaktionsnachweis weiter, ohne fachlichen Zustand im Circuit zu erfinden.

  - [ ] **M2.4-T3 – Komponenten- und visuelle Smoke-Testbasis erweitern**
    - Projekt: das seit M1.2 vorhandene `KnowHowToAI.BrowserTests` mit Microsoft.Playwright .NET verwenden. Es referenziert weiterhin kein Produktionsprojekt und behandelt die veröffentlichte Server-EXE als Black Box.
    - Testhost: Server einmal pro Testkollektion mit `dotnet publish`-Artefakt, dynamischem Loopback-Port und Environment-/Kommandozeilen-Overrides starten; Migration für Shell-Smokes deaktivieren. Readiness über beobachtbaren HTTP-Zustand, Logs begrenzen/redigieren, Prozess und Port im `IAsyncLifetime` auch bei Fehlschlag sicher freigeben. Keine zweite Appsettings-Datei.
    - Browser: ausschließlich installiertes Google Chrome Stable (aktuelle installierte Version) über `Channel = "chrome"` und `Headless = true`; fehlendes Chrome ist ein klarer Preflight-Fehler, kein Chromium-Fallback. Kein Edge/Firefox/Safari-Projekt, keine sichtbare Debugkonfiguration als regulärer Testpfad.
    - Smokes: ausschließlich die tatsächlich erreichbare Shell bei 1280 × 720 und die kompakte Shell bei 1024 × 720. Je Smoke zuerst semantische/Verhaltensassertionen, danach ein stabil maskierter Light-Theme-Screenshot; Animationen, Zeitwerte, Correlation-IDs und andere volatile Inhalte maskieren. Es wird keine Test-/Demo-Route in das Produkt eingebaut.
    - Baselines: genau diese zwei Shell-Viewports als versionierte visuelle Baselines. Weitere Zustände erzeugen erst mit einem echten späteren Verbraucher Diagnose-Screenshots oder bewusst beschlossene Baselines. Baselineänderungen benötigen Diff-Prüfung; automatische Aktualisierung im regulären Lauf ist verboten.
    - Skripte: feste lokale/CI-Befehle und Chrome-Preflight aus dem [Strukturkonzept](../konzept/08-projektstruktur-und-codekonventionen.md#feste-testabhängigkeiten-und-befehle) dokumentieren. Vitest ist nicht erforderlich; kein `package.json` allein für BrowserTests anlegen.
    - Nicht enthalten: flächendeckende Pixeltests.
    - Abnahme: Web.Tests und BrowserTests laufen zweimal hintereinander grün, hinterlassen keinen Prozess/Port und spätere Milestones besitzen klar benannte Ablagen für Komponenten- beziehungsweise echte Benutzerablauftests.

## Milestone-Abnahme

- Einheitliche Anwendungsshell ohne fachliche Platzhalterlogik.
- Design, Zustände, Reconnect und grundlegende Zugänglichkeit sind wiederverwendbar abgesichert.
- Alle folgenden Features verwenden dieselbe Komponenten- und Themebasis.

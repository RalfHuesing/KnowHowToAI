# AF1 – BrowserTests-Infrastruktur konsolidieren

[Roadmap-Index](Roadmap.md)

- [ ] **AF1 abschließen**

Abhängigkeit: keine. Referenz-Audit: `temp/webfrontend-audit-M1-M2.md` (Befunde A1, B1, B2; Wegwerf-Ablage, nicht eingecheckt).

Ziel: Die Browser-Testinfrastruktur hält den M2.4-T3-Vertrag „Server einmal pro Testkollektion" ein, Chrome-Start und Interaktivitätsnachweis existieren je genau einmal, und die Testdauern werden nach jedem Lauf automatisch sichtbar ausgewertet.

Verbindliche Basis: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md) (Abschnitte Teststruktur und Feste Testabhängigkeiten), [TestRichtlinien](../../../.agents/rules/TestRichtlinien.mdc).

Gemessener Ausgangszustand (2026-09-19, siehe [Roadmap-Index](Roadmap.md)): 10 `PublishedServerHost.StartAsync()`-Aufrufe in 6 Smoke-Klassen (je Testmethode ein vollständiges Publish und ein Serverstart), Chrome-Launch-Block identisch in 7 Dateien, Interaktivitäts-Klick-Retry-Loop wortgleich in 5 Dateien; voller Browserlauf 50 s Wallclock, alle grün.

## AF1.1 – Host-Lifecycle

- [x] **AF1.1 abschließen**

  - [x] **AF1.1-T1 – Serverhost pro Testkollektion starten**
    - Neue Datei `tests/KnowHowToAI.BrowserTests/TestSupport/SmokeHostFixture.cs`: `public sealed class SmokeHostFixture : IAsyncLifetime`. `InitializeAsync` ruft `PublishedServerHost.StartAsync()` auf und hält das Ergebnis in der Eigenschaft `Host`; `DisposeAsync` gibt den Host frei. Klasse enthält keine Assertions und keine andere Logik. Namespace `KnowHowToAI.BrowserTests.TestSupport`.
    - Neue Datei `tests/KnowHowToAI.BrowserTests/TestSupport/SmokeHostCollection.cs`: `[CollectionDefinition("Smoke-Host")] public sealed class SmokeHostCollection : ICollectionFixture<SmokeHostFixture>;` — nur diese Deklaration, keine Member. Zwei separate Dateien, weil jede `.cs`-Datei genau einen öffentlichen Top-Level-Typ besitzt.
    - Anpassen der fünf Smoke-Klassen `ShellSmokeTests`, `DialogKeyboardSmokeTests`, `LayoutShellSmokeTests`, `ResponsiveShellSmokeTests` und `VisualShellSmokeTests` im Ordner `ReadOnly/`: Attribut `[Collection("Smoke-Host")]`, Konstruktor nimmt einen `SmokeHostFixture`-Parameter und merkt sich `fixture.Host`. Jeder bisherige Aufruf `await PublishedServerHost.StartAsync()` in diesen Klassen wird durch den geteilten Host ersetzt; die Tests nutzen dessen `Address` unverändert weiter.
    - `ReconnectOverlaySmokeTests` bleibt unverändert: Seine Tests töten und starten Hosts absichtlich neu (Hostneustart, abgelaufener Circuit am selben Origin). Diese Ausnahme ist im Klassen-Dokumentationskommentar bereits so beschrieben und wird nicht umgebaut.
    - Bekannte Fallstricke: xUnit führt Testklassen derselben Collection sequenziell aus; das bestehende statische Publish-Gate in `PublishedServerHost` bleibt unverändert und serialisiert die Publishes der Parallel-Klassen weiterhin. `FullyQualifiedName~ShellSmokeTests` matcht als Substring auch `LayoutShellSmokeTests` und `ResponsiveShellSmokeTests` — bei gefilterten Läufen die erwartete Testzahl prüfen.
    - Doku: `docs/Architektur.md` (Abschnitt BrowserTests-Beschreibung) im selben Commit auf „Serverstart einmal pro Testkollektion über eine gemeinsame Kollektions-Fixture; Reconnect-Smokes behalten bewusst eigene Hosts" anpassen.
    - Tests: `dotnet build KnowHowToAI.slnx -v q --nologo`, danach das komplette BrowserTests-Projekt **zweimal hintereinander** grün: `dotnet test tests/KnowHowToAI.BrowserTests/KnowHowToAI.BrowserTests.csproj --nologo`. Kein Testfall wird entfernt, umbenannt oder zusammengelegt.
    - Abnahme: Alle 10 BrowserTests zweimal grün; `grep -rn "PublishedServerHost.StartAsync" tests/KnowHowToAI.BrowserTests/ReadOnly/` liefert Treffer ausschließlich in `ReconnectOverlaySmokeTests.cs` (3 Aufrufe); keine neue Assertion, kein verändertes Testverhalten.

## AF1.2 – Gemeinsame Helfer

- [ ] **AF1.2 abschließen**

  - [ ] **AF1.2-T1 – Chrome-Preflight ins TestSupport-Projekt heben**
    - Neue Datei `tests/KnowHowToAI.TestSupport/ChromeStablePreflight.cs`: `public static class ChromeStablePreflight` mit `public static void EnsureIsInstalled()`. Die Methode übernimmt die Logik aus `PublishedServerHost.EnsureChromeStableIsInstalled` und `PublishedServerHost.ReadChromeVersion` (Uninstall-Eintrag `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome` sowie WOW6432Node-Variante, `DisplayVersion`-Wert; fehlt beides: `InvalidOperationException` mit der bestehenden deutschen Meldung). Nicht-Windows wirft `PlatformNotSupportedException` via `OperatingSystem.IsWindows()`-Prüfung. XML-Doku aus der bestehenden Implementierung übernehmen.
    - `PublishedServerHost` gibt die beiden privaten Methoden auf und ruft stattdessen `ChromeStablePreflight.EnsureIsInstalled()` auf; Verhalten und Fehlermeldungen bleiben identisch.
    - Paket: `tests/KnowHowToAI.TestSupport/KnowHowToAI.TestSupport.csproj` erhält `<PackageReference Include="Microsoft.Win32.Registry" />`. Zentrale Version `<PackageVersion Include="Microsoft.Win32.Registry" Version="5.0.0" />` in `Directory.Packages.props` ergänzen — exakt 5.0.0, weil diese Version bereits transitiv über xunit im Graph aufgelöst ist; keine neuere Version suchen oder wählen (Abhängigkeitsregel des Strukturkonzepts).
    - `THIRD-PARTY-NOTICES.md`: die bestehende Zeile `Microsoft.Win32.Registry 5.0.0` von „transitiv" auf „direkt in KnowHowToAI.TestSupport; zusätzlich transitiv über xunit" anpassen; alle übrigen Spalten der Zeile bleiben unverändert.
    - Tests: Build grün; BrowserTests-Projekt einmal grün; `verify(targetPath, scope: "changes")` grün.
    - Abnahme: `PublishedServerHost` enthält keine Registry-Logik mehr; das Projekt `KnowHowToAI.TestSupport` bleibt xunit-frei (keine Testframework-Referenz).

  - [ ] **AF1.2-T2 – Chrome-Start und Interaktivitätsprobe zentralisieren**
    - Neue Datei `tests/KnowHowToAI.BrowserTests/TestSupport/ChromeBrowser.cs`: `public sealed class ChromeBrowser : IAsyncDisposable`. Statische Factory `LaunchAsync(BrowserTypeLaunchOptions? options = null)` setzt erzwingend `Channel = "chrome"` und `Headless = true` (Aufrufer-Optionen werden danach angewandt und dürfen nur Viewport/ReducedMotion ergänzen), erzeugt `Playwright` und Browser und stellt über `NewPageAsync(BrowserNewPageOptions? options = null)` Seiten bereit. `DisposeAsync` gibt Browser und Playwright frei. Keine Assertions, kein Preflight (der Host übernimmt ihn).
    - Neue Datei `tests/KnowHowToAI.BrowserTests/TestSupport/CircuitProbe.cs`: `public static class CircuitProbe` mit `public static async Task WaitForInteractivityAsync(IPage page)`. Die Methode übernimmt den bestehenden Klick-Retry-Loop aus `ShellSmokeTests` unverändert in Verhalten und Kommentar: Klick auf den Button „Interaktivität prüfen" (Rolle Button, exakter Name), danach `Expect(...).ToHaveTextAsync("Interaktivität ist verfügbar.", Timeout 2000 ms)`, bei `PlaywrightException` erneut klicken, maximal 10 Versuche, danach schlägt der letzte Erwartungswartende fehl. Nur beobachtbare Zustände, keine Sleeps.
    - Umarbeitung aller sechs Smoke-Klassen in `ReadOnly/`: Chrome-Start-Block ersetzt durch `ChromeBrowser.LaunchAsync`, jeder kopierte Retry-Loop ersetzt durch `CircuitProbe.WaitForInteractivityAsync(page)`. Testkörper, Assertions, Viewports, Maskierungen und Baselines bleiben inhaltlich identisch.
    - `ReconnectOverlaySmokeTests` nutzt ebenfalls beide Helfer; seine eigenen `PublishedServerHost.StartAsync`-Aufrufe (AF1.1) bleiben.
    - Duplikat-Hinweis: `LayoutShellSmokeTests.OpenNavigationAsync` (Maus) und `ResponsiveShellSmokeTests.OpenNavigationWithKeyboardAsync` (Tastatur) sind absichtlich verschiedene Eingabemodi und werden **nicht** zusammengelegt.
    - Tests: BrowserTests-Projekt zweimal hintereinander grün; `verify(scope: "changes")` grün.
    - Abnahme: `grep -rn 'Channel = "chrome"' tests/KnowHowToAI.BrowserTests/ReadOnly/` → 0 Treffer; `grep -rn 'attempt < 10' tests/KnowHowToAI.BrowserTests/ReadOnly/` → 0 Treffer (der Loop existiert genau einmal in `CircuitProbe`); alle 10 Tests grün.

## AF1.3 – Testdauer-Sichtbarkeit

- [ ] **AF1.3 abschließen**

  - [ ] **AF1.3-T1 – Testdauer-Auswertung in den Testskripten etablieren**
    - Neue Datei `scripts/test-durations.ps1` (`#requires -Version 7.0`): Parameter `[string[]]$TrxPath` (Pflicht) und `[int]$Top = 15`. Das Skript parst die übergebenen TRX-Dateien (XML-Namespace `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`), ordnet über `UnitTestResult.testId` → `UnitTest/TestMethod.className` zu, summiert Dauern je Klasse (Dauerformat `HH:MM:SS.fffffff`) und druckt: Gesamtsumme, Testanzahl und eine absteigend sortierte Tabelle der `Top`-Klassen mit Summe, Anzahl und längstem Einzeltest. Fehlende Datei: klare deutsche Fehlermeldung, Exit 1. Keine Abhängigkeit außer PowerShell-Bordmitteln.
    - `scripts/test-fast.ps1`: nach jedem erfolgreichen `dotnet test`-Lauf eines Projekts den Aufruf `& (Join-Path $PSScriptRoot 'test-durations.ps1') -TrxPath $trxFilePath` ergänzen (Pfad der gerade geschriebenen TRX). Der Aufruf darf den Exitcode des Skripts nicht beeinflussen (Eigene `try/catch`-Klammer mit Warnungsausgabe).
    - `scripts/test-integration.ps1`: gleiche Ergänzung für beide erzeugten TRX-Dateien `TestResults/IntegrationTests.trx` und `TestResults/IntegrationTests.Browser.trx`.
    - Nicht enthalten: keine Schwellwerte, keine Gates, keine Farbauswertung, keine Änderung an Filtern oder Laufverhalten der Tests.
    - Tests: `pwsh -NoProfile -File scripts/test-fast.ps1` einmal grün mit sichtbarer Dauertabelle; `pwsh -NoProfile -File scripts/test-durations.ps1 -TrxPath TestResults/FastTests.KnowHowToAI.Web.Tests.trx` gegen eine bestehende TRX liefert die Tabelle; Aufruf mit nicht existierendem Pfad beendet sich mit Fehlermeldung und Exit 1.
    - Abnahme: Beide Testskripte laufen unverändert grün und enden mit der Dauertabelle; die Dauertabelle des Fast-Gates zeigt rund 10 Sekunden Gesamttestdauer (Referenz aus dem Roadmap-Index); kein Testverhalten verändert.

## Milestone-Abnahme

- Das BrowserTests-Projekt startet pro Testkollektion genau einen Host und ein Publish; nur die Reconnect-Smokes betreiben bewusst eigene Hosts.
- Chrome-Preflight, Chrome-Start und Interaktivitätsnachweis existieren je genau einmal im Quellbaum.
- Nach jedem Fast- und Integrationstestlauf wird automatisch eine Testdauertabelle ausgegeben.
- Alle Quality-Gates grün (Build, FastTests, BrowserTests zweimal, `verify(scope: "solution")` mit verdict pass).

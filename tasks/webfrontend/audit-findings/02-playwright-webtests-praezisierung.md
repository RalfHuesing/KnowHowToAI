# AF2 – Playwright in Web.Tests präzisieren und absichern

[Roadmap-Index](Roadmap.md)

- [ ] **AF2 abschließen**

Abhängigkeit: [AF1](01-browsertests-infrastruktur.md) — konkret AF1.2-T1 (`ChromeStablePreflight` existiert im TestSupport-Projekt).

Ausgangslage: `KnowHowToAI.Web.Tests` referenziert `Microsoft.Playwright` und startet in `Features/DesignTokens/DesignTokensShowcaseTests.cs` Headless Chrome für berechnete Stile und den Showcase-Screenshot. Das Zielkonzept `tasks/webfrontend/konzept/08-projektstruktur-und-codekonventionen.md` sagt in der Tabelle „Feste Testabhängigkeiten und Befehle" jedoch „nur `KnowHowToAI.BrowserTests`". Der Ist-Zustand war nie ins Zielkonzept zurückgeschrieben worden; außerdem läuft der Chrome-startende Test als `Category=Unit` im FastTest-Gate, ohne dass der dort übliche Chrome-Preflight ausgeführt würde.

Entscheidung (durch diese Roadmap getroffen, keine neue Offenheit): Playwright bleibt in `Web.Tests` **ausschließlich** für TestSupport-Fixture-Renderings ohne Produkt-Route; der Test bleibt `Category=Unit`; der Chrome-Preflight wird vor dem Browserstart aufgerufen, damit ein fehlendes Chrome im Fast-Gate einen klaren, deutschsprachigen Fehler liefert statt einer unklaren Playwright-Ausnahme.

## AF2.1 – Konzept und Preflight

- [ ] **AF2.1 abschließen**

  - [x] **AF2.1-T1 – Zielkonzept-Zeile zu Playwright präzisieren**
    - Bereits am 2026-09-19 durch den Audit-Agenten ausgeführt und committet (`0a8706c`; genau eine Tabellenzeile per Byte-Ersatz geändert). Dieser Punkt wird von ausführenden Agenten nicht erneut bearbeitet.
    - **Dokumentierte Readonly-Ausnahme** (siehe Roadmap-Index): Nur für die hier angegebenen Bytes in `tasks/webfrontend/konzept/08-projektstruktur-und-codekonventionen.md` ist die Konzeptänderung vom Benutzer ausdrücklich freigegeben. Nichts darüber hinaus am Konzept ändern.
    - Einzige Änderung ist die Zeile in der Tabelle „Feste Testabhängigkeiten und Befehle". Alt (wortwörtlich, inklusive Rahmen):

      ```text
      | Browsersteuerung | `Microsoft.Playwright` | nur `KnowHowToAI.BrowserTests`; Version gemäß allgemeiner Abhängigkeitsregel |
      ```

      Neu (wortwörtlich, inklusive Rahmen):

      ```text
      | Browsersteuerung | `Microsoft.Playwright` | `KnowHowToAI.BrowserTests` für alle Browserabläufe gegen den echten Host; zusätzlich `KnowHowToAI.Web.Tests` ausschließlich für TestSupport-Fixture-Renderungen ohne Produkt-Route (berechnete Stile, Screenshot) mit Chrome-Preflight vor dem Start; kein weiteres Projekt; Version gemäß allgemeiner Abhängigkeitsregel |
      ```

    - Danach `git diff` prüfen: es darf genau eine geänderte Zeile in genau dieser Datei stehen. Kein Umbrechen weiterer Zeilen, keine Umformatierung der Tabelle.

  - [ ] **AF2.1-T2 – Chrome-Preflight im DesignTokens-Showcase-Test aufrufen**
    - Voraussetzung: AF1.2-T1 ist abgeschlossen und committed; `KnowHowToAI.TestSupport` enthält `ChromeStablePreflight.EnsureIsInstalled()`.
    - `tests/KnowHowToAI.Web.Tests/Features/DesignTokens/DesignTokensShowcaseTests.cs`: als erste Anweisung des Testkörpers `ChromeStablePreflight.EnsureIsInstalled();` ergänzen (using `KnowHowToAI.TestSupport` ist dort bereits vorhanden oder wird ergänzt). Nichts am Testablauf darüber hinaus ändern.
    - Doku: `docs/Architektur.md` — in dem Absatz, der das DesignTokens-Showcase-Fixture beschreibt, einen Satz ergänzen, dass der Test vor dem Chrome-Start den Chrome-Stable-Preflight aus `KnowHowToAI.TestSupport` ausführt und deshalb ohne installiertes Chrome mit einer klaren deutschen Fehlermeldung fehlschlägt.
    - Tests: `pwsh -NoProfile -File scripts/test-fast.ps1` einmal vollständig grün (Testanzahl unverändert, weiterhin 126 Web-Tests plus Core/Integration); `verify(scope: "changes")` grün.
    - Abnahme: Der Test startet Chrome erst nach bestandenem Preflight; es existiert keine zweite Chrome-Erkennungslogik in `Web.Tests` (grep nach `Registry` in `tests/KnowHowToAI.Web.Tests/` → 0 Treffer).

## Milestone-Abnahme

- Das Zielkonzept beschreibt die tatsächliche Playwright-Nutzung beider Testprojekte korrekt.
- Der Fast-Gate scheitert ohne installiertes Chrome mit der klaren Preflight-Fehlermeldung aus `ChromeStablePreflight`.
- Alle Quality-Gates grün.

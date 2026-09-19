# AF3 – Web.Tests-Ordner an Produktionsgrenzen angleichen

[Roadmap-Index](Roadmap.md)

- [x] **AF3 abschließen**

Abhängigkeit: keine (unabhängig von AF1/AF2/AF4; darf parallel oder danach ausgeführt werden, aber seriell im Worktree wie jeder andere Leaf-Task).

Ausgangslage: Das Strukturkonzept und die Ist-Doku (`docs/Architektur.md`, Abschnitt Testablagen) verlangen `Components/{Layout,Shared}`, `Features/<Feature>`, `State/` und `TestSupport/`. Tatsächlich liegen in `tests/KnowHowToAI.Web.Tests`:

- `Features/Shell/` mit `MainLayoutTests`, `KnowledgeContextBarTests`, `ReconnectModalTests` — geprüfte Komponenten liegen in `Web/Components/Layout`; es gibt kein Produktionsfeature „Shell".
- `Features/Shared/AppStatusTests.cs` — `AppStatus` liegt in `Web/Components/Shared`.
- `Features/DesignTokens/` mit `DesignTokensTests` und `DesignTokensShowcaseTests` — reine Testkonstrukte (Showcase, Token-Datei), kein Produktionsfeature; die zugehörigen Fixtures (`DesignTokensShowcase.razor`, `UiBasisShowcase.razor`) liegen bereits in `TestSupport/`.

Es geht ausschließlich um Ablage und Namespaces — kein Testinhalt, keine Assertion und kein Produktionscode wird geändert.

## AF3.1 – Ordner und Namespaces

- [x] **AF3.1 abschließen**

  - [x] **AF3.1-T1 – Testdateien an die Produktionsstruktur verschieben**
    - Verschiebungen mit `git mv` (erhält die Historie):
      - `tests/KnowHowToAI.Web.Tests/Features/Shell/MainLayoutTests.cs` → `tests/KnowHowToAI.Web.Tests/Components/Layout/MainLayoutTests.cs`
      - `tests/KnowHowToAI.Web.Tests/Features/Shell/KnowledgeContextBarTests.cs` → `tests/KnowHowToAI.Web.Tests/Components/Layout/KnowledgeContextBarTests.cs`
      - `tests/KnowHowToAI.Web.Tests/Features/Shell/ReconnectModalTests.cs` → `tests/KnowHowToAI.Web.Tests/Components/Layout/ReconnectModalTests.cs`
      - `tests/KnowHowToAI.Web.Tests/Features/Shared/AppStatusTests.cs` → `tests/KnowHowToAI.Web.Tests/Components/Shared/AppStatusTests.cs`
      - `tests/KnowHowToAI.Web.Tests/Features/DesignTokens/DesignTokensTests.cs` → `tests/KnowHowToAI.Web.Tests/TestSupport/DesignTokensTests.cs`
      - `tests/KnowHowToAI.Web.Tests/Features/DesignTokens/DesignTokensShowcaseTests.cs` → `tests/KnowHowToAI.Web.Tests/TestSupport/DesignTokensShowcaseTests.cs`
    - Danach die leeren Ordner `Features/Shell`, `Features/Shared` und `Features/DesignTokens` entfernen (nach `git mv` sollten sie leer sein; `Features/` selbst bleibt — `Features/Dashboard` ist korrekt belegt).
    - Namespaces der verschobenen Dateien an den neuen Ordner anpassen (file-scoped, wie bisher): `KnowHowToAI.Web.Tests.Components.Layout`, `KnowHowToAI.Web.Tests.Components.Shared`, `KnowHowToAI.Web.Tests.TestSupport`. Danach repositoryweit nach Referenzen auf die drei alten Namespaces suchen (`rg "Web.Tests.Features.Shell|Web.Tests.Features.Shared|Web.Tests.Features.DesignTokens"`) und alle Fundstellen (usings, xmldoc-Verweise) auf die neuen Namespaces umstellen.
    - Keine weiteren Umbenennungen: Klassen, Testnamen, Traits und Assertions bleiben unverändert.
    - Doku: `docs/Architektur.md`, Absatz „Die Testablagen sind nach Prüfgegenstand benannt" — den Satz zu den Komponententests so anpassen, dass `Components/{Layout,Shared}` (Layout- und Shared-Komponenten), `Features/<Feature>` (echte Feature-Seiten wie Dashboard) und `TestSupport/` (Showcase- und Token-Fixture-Tests: `UiBasisShowcase`, `DesignTokens`) die tatsächliche Ablage beschreiben. Die BrowserTests-Aussagen des Absatzes bleiben unverändert.
    - Tests: `dotnet build KnowHowToAI.slnx -v q --nologo`; danach `pwsh -NoProfile -File scripts/test-fast.ps1` vollständig grün mit unveränderter Testzahl (126 Web-Tests); `verify(scope: "changes")` grün.
    - Abnahme: `ls tests/KnowHowToAI.Web.Tests/Features/` zeigt nur noch `Dashboard`; unter `Components/` existieren `Layout/` und `Shared/` mit den genannten Dateien; `git log --stat -1` zeigt nur Renames und Namespace/using-Änderungen plus die eine Doku-Änderung; alle Gates grün.

## Milestone-Abnahme

- Die Testablage in `Web.Tests` spiegelt die Produktionsgrenzen aus dem Strukturkonzept.
- Die Ist-Doku beschreibt die Testablagen korrekt.
- Alle Quality-Gates grün.

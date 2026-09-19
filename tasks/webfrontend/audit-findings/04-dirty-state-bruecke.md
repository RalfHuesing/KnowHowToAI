# AF4 – Dirty-State-Brücke zwischen Kontextleiste und Reconnect

[Roadmap-Index](Roadmap.md)

- [x] **AF4 abschließen**

Abhängigkeit: keine inhaltliche; AF1.1-T1 sollte abgeschlossen sein, damit der erweiterte Shell-Smoke auf dem Kollektions-Host läuft (kein hartes Kriterium — der Test funktioniert auch mit dem bisherigen pro-Test-Host).

Ausgangslage: `ReconnectModal.razor.js` warnt bei `beforeunload` nur, wenn `window.KnowHowToAI?.isDirty === true`. Repository-weit existiert kein Schreiber dieses Flags; in M2 kann kein Feature dirty sein, deshalb ist das Verhalten heute korrekt. Es existieren aber zwei Repräsentationen desselben fachlichen Zustands (C#-`KnowledgeContextViewModel.IsDirty` im Circuit versus JS-Global im Browser) ohne definierte, getestete Brücke. Das erste M3/M4-Feature mit echtem Dirty-State würde sie ad hoc oder gar nicht errichten.

Entscheidung (durch diese Roadmap getroffen, keine neue Offenheit): **Das Wurzelelement der Kontextleiste trägt den Zustand als Markup-Attribut, und das Reconnect-Modul liest es zum Ereigniszeitpunkt.** Damit entfällt das ungeschriebene `window`-Flag; die eine Wahrheit ist der ViewModel-Vertrag aus M2.2-T2, das Attribut wird bereits beim Prerendering mitgeliefert und funktioniert deshalb auch vor verbundem Circuit. Kein neuer JS-Interop-Baustein, keine Zustandsmaschine.

## AF4.1 – Attribut und Lesung

- [x] **AF4.1 abschließen**

  - [x] **AF4.1-T1 – `data-ktai-dirty`-Attribut etablieren und `beforeunload` darauf umstellen**
    - `src/KnowHowToAI.Server/Web/Components/Layout/KnowledgeContextBar.razor`: am Wurzelelement `<div class="knowledge-context" role="group" aria-label="Wissenskontext">` das Attribut `data-ktai-dirty="@(Context.IsDirty ? "true" : "false")"` ergänzen. Keine weiteren Markup-Änderungen; das bestehende `AppStatus`-Verhalten (Icon plus Text nur bei `IsDirty`) bleibt unverändert.
    - `src/KnowHowToAI.Server/Web/Components/Layout/ReconnectModal.razor.js`: den `beforeunload`-Listener ersetzen. Alt (wortwörtlich):

      ```javascript
      document.addEventListener("beforeunload", (event) => {
          if (window.KnowHowToAI?.isDirty !== true) {
              return;
          }
          event.preventDefault();
          event.returnValue = "";
      });
      ```

      Neu (wortwörtlich):

      ```javascript
      // Reload warnt nur bei tatsächlich ungespeicherten Änderungen: die
      // Kontextleiste trägt den Dirty-Zustand als data-ktai-dirty-Attribut
      // (einzige Wahrheit ist der ViewModel-Vertrag, auch im Prerendering
      // vorhanden); fehlt das Element, gilt die Seite als nicht dirty; eine
      // persistierte Transaction gilt nie als ungespeichert.
      document.addEventListener("beforeunload", (event) => {
          if (document.querySelector("[data-ktai-dirty]")?.dataset.ktaiDirty !== "true") {
              return;
          }
          event.preventDefault();
          event.returnValue = "";
      });
      ```

      Der zugehörige alte Kommentar („Reload warnt nur bei tatsächlich ungespeicherten Änderungen: solange kein Feature den Arbeitsstand als dirty meldet, …") wird durch den neuen Kommentar ersetzt. `window.KnowHowToAI` wird danach im gesamten Repository nicht mehr referenziert.
    - Komponententests `tests/KnowHowToAI.Web.Tests/Components/Layout/KnowledgeContextBarTests.cs` (Ablage nach AF3; andernfalls `Features/Shell/`): die bestehenden parameterisierten Fälle um je eine Attribut-Assertion erweitern — bei `IsDirty = true` steht `data-ktai-dirty` auf `"true"`, bei `IsDirty = false` auf `"false"`. Kein neuer Testfall, keine Änderung bestehender Assertionen.
    - Browser-Smoke: in `tests/KnowHowToAI.BrowserTests/ReadOnly/ShellSmokeTests.cs` nach dem Interaktivitätsnachweis eine Assertion ergänzen, dass die gerenderte Kontextleiste das Attribut trägt: `await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "false");` — die Dashboard-Seite liefert `Current` mit `IsDirty = false`, deshalb ist `"false"` der erwartete echte Wert. Kein Testfall für `IsDirty = true` im Browser: dafür existiert bis M3 kein echter Erzeuger, und die Komponententests decken beide Werte ab (keine künstliche Test-Route, keine Test-Hooks).
    - Doku: `docs/Architektur.md` — die Abschnitte zur Wissenskontextleiste und zur Reconnect-Oberfläche im selben Commit ergänzen: die Kontextleiste spiegelt `IsDirty` als `data-ktai-dirty`-Attribut ihres Wurzelelements; `beforeunload` liest dieses Attribut zum Ereigniszeitpunkt; ein fehlendes Element gilt als nicht dirty; es existiert kein `window`-Flag mehr.
    - Nicht enthalten: `WorkspaceState` aus M3, Rollen-/Transaktionsselektoren, irgendein M3+-Fachfeature, ein Warnungsdialog vor dem Neuladen (der native `beforeunload`-Dialog bleibt das Mittel).
    - Tests: `pwsh -NoProfile -File scripts/test-fast.ps1` vollständig grün (Testzahl steigt genau um die erweiterten Assertionen — keine neuen Testfälle, daher gleiche Fallzahl 126 Web-Tests); BrowserTests-Projekt einmal grün; `verify(scope: "changes")` grün; danach `verify(scope: "solution")` mit verdict pass.
    - Abnahme: `rg "window.KnowHowToAI" -g '!temp'` → 0 Treffer im Repository; `rg "data-ktai-dirty" src tests` zeigt genau KnowledgeContextBar.razor, ReconnectModal.razor.js, KnowledgeContextBarTests und ShellSmokeTests; alle Gates grün.

## Milestone-Abnahme

- Es existiert genau eine Dirty-Wahrheit: der ViewModel-Vertrag der Kontextleiste.
- `beforeunload` verhält sich belegbar: Attribut `"true"` → Warnung; `"false"` oder Element fehlt → keine Warnung.
- Die Brücke funktioniert bereits im Prerendering (Attribut ist serverseitig gerendertes Markup).
- Alle Quality-Gates grün.

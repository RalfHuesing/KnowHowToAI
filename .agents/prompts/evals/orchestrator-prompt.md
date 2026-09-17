# Trigger-Prompt für den Orchestrator (Hermes-Desktop-Session)

**Auslöser:** Ralf schreibt im Chat sinngemäß „ich will `<name>` testen" und verweist auf
diese Datei. `<name>` ist der Eval-Name (kebab-case, Ralf gibt ihn an oder ergibt sich
aus dem Auftrag). Kein anderes Vorgehen — dieses Dokument ist die verbindliche
Anweisung für den gesamten Ablauf.

## Rollen

- **Orchestrator (du):** bereitet den Lauf vor, startet das Skript, analysiert das
  Ergebnis, berichtet. Schreibt ausschließlich in das Eval-Verzeichnis und liest sonst
  nur. Keine Code-Änderungen, keine Commits außer dem Pathspec-Commit der
  Eval-Infrastruktur, wenn Ralf das ausdrücklich beauftragt.
- **Eval-Agent (wird vom Skript gespawnt):** eigener `hermes chat -q`-Prozess, spannt
  den KnowHowToAI-MCP-Server als Kindprozess, führt die Task agentisch aus.
- **Writer (nur auf Ralfs Anweisung):** fixt Code/Descriptions, hat keine MCP-Tools.

## Ablauf

1. **Eval-Verzeichnis anlegen:** `tasks/eval-<name>/` (Wegwerf, wird aus .gitignore
   ausgeschlossen und von Ralf manuell gelöscht). Strikt ein Lauf zur Zeit —
   Scratch-DB-Konkurrenz.
2. **Task-Datei schreiben:** `tasks/eval-<name>/task.md` nach dem MCPMark-Dreieck:
   - **Instruction:** was der Eval-Agent tun soll (aus Ralfs Chat-Auftrag abgeleitet,
     konkret und agentisch formuliert).
   - **Initialzustand:** welcher DB-Zustand erwartet wird (i. d. R. der geseedete
     Stand; Mutations-Erlaubnis hier explizit vermerken oder Read-only deklarieren).
   - **Verifikation:** was programmatisch am Ergebnis prüfbare ist (Read-Tool-Call,
     Fehlercode-Verhalten, Envelope-Form) — bewertet wird das **Ergebnis**, nicht die
     Aufrufsequenz.
   Die Task-Datei vor dem Start Ralf im Chat zeigen (kurz), dann starten — außer der
   Auftrag enthält „ohne Rückfrage" o. ä.
3. **Skript ausführen:**
   `pwsh -NoProfile -File scripts/eval-run.ps1 -Name <name> [-MaxTurns N]`
   im Hintergrund starten (`terminal(background=true, notify=true)`), Ausgabe
   abwarten. Das Skript: killt alte Server-Instanzen, wärmt den Build, macht den
   MCP-Verbindungstest, komponiert den Prompt aus dem Template und startet den
   Eval-Agenten. Produkte: `prompt.md`, `run.log` im Eval-Verzeichnis; das Journal
   `journal.md` schreibt der Eval-Agent selbst.
4. **Auswerten (nach Skriptende):**
   - `journal.md` vollständig lesen, `run.log` querlesen (Abbrüche, Approval-Hänger).
   - `git status` prüfen: muss clean sein (Eval-Verzeichnis ist ignoriert).
   - Findings bewerten: pro Befund Severity (Blocker/Major/Minor/Observation),
     betroffenes Tool, Contract-Bezug. Rohe-Transkript bei Unklarheiten heranziehen.
5. **Bericht an Ralf:** Ergebnis (Resultat der Verifikation), Findings-Tabelle,
   Gesamturteil zur Agenten-Usability. Contract-Befunde (Fehlercode-Gestalt,
   Parameterform, Envelope-Shape) zur Sondierung vorlegen — Server-Code nicht
   eigenmächtig ändern. Offene Punkte für einen Writer-Slice klar benennen.

## Regeln

- Eval-Läufe sind Hinweise, keine Regression-Gates (nichtdeterministisch).
- DB-Mutationen des Eval-Agenten vermerken; Cleanup nur auf Ralfs Anweisung oder
  wenn die Task es selbst vorsieht.
- Nach jedem Lauf: Lessons Learned in den Skill `knowhowtoai-development` bzw. diesen
  Prompt zurückspielen, wenn der Ablauf selbst Stolperfallen zeigte.

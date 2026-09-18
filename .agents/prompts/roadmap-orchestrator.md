# Roadmap-Orchestrator

Wird dieser Prompt zusammen mit einem Roadmap-Pfad übergeben, orchestrierst du
**die gesamte übergebene Roadmap**. Der Roadmap-Pfad ist der Scope. Beispiele,
Bezeichnungen und IDs in diesem Prompt sind nicht als Arbeitsauftrag zu
verstehen. Lies die Roadmap und ermittle alle Meilensteine und offenen
`T`-Unterpunkte selbst; nichts ist fest verdrahtet.

## Ablauf

1. Lies `AGENTS.md`, die Roadmap sowie alle von ihnen verlangten Regeln,
   Ist-Dokumentationen und Konzepte. Prüfe den Git-Status und bestehende
   Roadmap-Checkboxen.
2. Bearbeite offene `T`-Unterpunkte in Roadmap-Reihenfolge und seriell: Pro
   Unterpunkt genau einen schreibenden Implementierungs-Subagenten im
   gemeinsamen Worktree starten.
3. Der Auftrag an den Subagenten beginnt mit
   `Implementiere ausschließlich <T-ID> – <T-Titel>.` Er enthält danach nur
   den für diesen Slice nötigen Kontext: Akzeptanzkriterien, relevante Regeln,
   Ist-Doku, readonly Konzeptgrenzen, Vorbedingungen, vorhandene Änderungen,
   proportionale Prüfungen sowie die verbindliche Vorgabe, bei C#-Aufgaben
   pro-aktiv den `AiNetLinter` MCP-Server zu nutzen.
4. Der Implementierer setzt seinen vollständigen Slice um, pflegt erforderliche
   Ist-Doku, führt sinnvolle und notwendige Prüfungen aus und erstellt einen
   atomaren Commit. Er verändert weder fremde Änderungen noch andere
   Roadmap-Punkte.
5. Prüfe die Übergabe. Ist der vollständige Akzeptanzumfang erfüllt und
   committed, hake den `T`-Punkt ab. Sind alle `T`-Punkte eines
   Teilmeilensteins erledigt, hake dessen Abschluss ab. Committe die
   Roadmap-Pflege atomar. Unvollständige oder blockierte Punkte bleiben offen.
6. Wiederhole ab Schritt 2, bis alle Punkte des aktuellen Meilensteins erledigt
   sind. Danach folgt der Review-Loop.

## Review-Loop

Nach Abschluss jedes Meilensteins startet ein eigener Review-Agent (Auftrag inkl. verbindlicher `AiNetLinter`-Vorgabe).

- Kleine, lokale Befunde behebt er selbst, prüft und committed sie.
- Größere oder strukturelle Befunde ergänzt er zuerst als präzise neue
  `T`-Unterpunkte mit Akzeptanzkriterien in der Roadmap und committed diese.
  Bearbeite diese Punkte anschließend wieder ab Schritt 2 und reviewe erneut.
- Ein Meilenstein ist erst abgeschlossen, wenn der letzte Review keine offenen
  Befunde mehr hat und alle ergänzten Punkte abgehakt sind.

## Harte Grenzen

- Konzepte sind readonly, insbesondere zugehörige `konzept/`-Dateien.
- `docs/` beschreibt nur belegbaren Ist-Zustand; Roadmaps bleiben
  Planungsartefakte, sind aber bei erfüllten Punkten abzuhaken.
- Keine spekulative Architektur, produktiven Test-Hooks oder Testfälle, die nur
  einen künstlichen Nachweis erzwingen. Teste repräsentativ, risikobasiert und
  nur notwendiges geändertes Verhalten bzw. reale Grenzen. Bevorzuge vorhandene
  Tests/Fixtures; keine Varianten-, Framework- oder Test-Loop-Duplikate.
- Wenn ein Nachweis ohne Architekturbruch nicht sinnvoll beobachtbar ist, den
  konkreten Konflikt festhalten und den Punkt offen lassen; keine Fallbacks,
  abgeschwächten Tests oder Scope-Umgehungen.
- Befolge projektspezifische Quality-Gates. Eigene Fehler vor Übergabe beheben;
  externe/vorbestehende Blocker klar abgrenzen.
- **AiNetLinter MCP pro-aktiv nutzen**: Bei C#-Aufgaben setzen Orchestrator wie
  Subagenten (Implementierer und Reviewer) pro-aktiv die semantischen MCP-Tools
  des `AiNetLinter` ein (Erkundung, AST-/Impact-Analyse, `verify`). Der
  Orchestrator gibt diese Pflicht verbindlich an alle Subagenten weiter.
- Jeder Code-, Test-, Doku-, Roadmap- und Review-Slice erhält einen passenden
  atomaren deutschen Conventional Commit. Kein Push oder History-Rewrite ohne
  Nutzerauftrag.

Berichte kurz gestartete und abgeschlossene Punkte, Commits, Checkboxen,
Review-Befunde und echte Blocker. Stoppe erst nach vollständiger Roadmap samt
Review-Loops oder wenn eine Nutzerentscheidung für einen echten Scope- oder
Umgebungskonflikt nötig ist.

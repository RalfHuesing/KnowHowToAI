# 03 – Wissensbasis-Root

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/03_knowledge_root_desktop_1280x800.png` · Route `/knowledge` · Rolle ausgewählt, Root-Zustand vor belastbarer Bestätigung · Desktop 1280×800.

## Neutrale Beobachtung

- Der Screenshot zeigt den Knowledge-Shell-Kontext nach einer Rollenaktion.
- Der Baum-/Root-Bereich ist sichtbar, aber die Aufnahme macht den Übergang von Auswahl zu bestätigtem Root nicht eindeutig.
- Der globale Rollenhinweis bleibt Teil der Shell.
- Die sichtbare Information reicht nicht aus, um eine konkrete Root-Node-Auswahl sicher zu bestätigen.
- Der Dateiname behauptet Root, während der sichtbare Zustand eher „ausgewählt vor Bestätigung“ belegt.
- Ein späterer Agent kann aus dem Bild allein nicht sicher entscheiden, ob der Datenkontext bereits aktiv ist.

## Probleme und Schwere

- **P1 – Auditsemantik:** Name und tatsächlicher sichtbarer Zustand können auseinanderfallen.
- **P1 – Kontext:** Beziehung zwischen Rolle, Root und Inhalt wird nicht eindeutig erklärt.
- **P2 – Affordance:** Die nächste bestätigende Aktion bzw. ihr Ergebnis ist nicht klar erkennbar.
- **P2 – Vertrauen:** Ein automatischer Vergleich könnte fälschlich einen bestätigten Root als belegt melden.

## Gelungene Aspekte

- Die Rollenaktion und der Knowledge-Bereich sind reproduzierbar erreichbar.
- Die Shell behält Navigation und Kontext bei.
- Der Zustand eignet sich als Ausgangspunkt für eine präzisere web-first Assertion.

## Folgerungen ohne Featureausweitung

- Capture erst nach sichtbarer Bestätigung des bestehenden Root-Zustands schreiben.
- Dateiname, Manifest und Befund an den tatsächlich gerenderten Zustand koppeln.
- Die vorhandene Root-/Rollenbedeutung nicht durch eine neue Produktbotschaft ersetzen.
- Bei Nichterreichbarkeit die Audit-Lücke explizit ausweisen.

## Abhängigkeiten

M1.2-T1, Visual-Shell-Seed, Rollen-/Knowledge-State und bestehende Bestätigungsnavigation.

## Audit-Lücken

**Ja.** Der Lauf belegt nur „ausgewählt vor Bestätigung“, nicht sicher den bestätigten Root-Zustand.

## M1.2-Nachweis

Der stabilisierte UiAudit-Lauf `temp/ui-audit/2026-09-20_19-48-27/03_knowledge_root_desktop_1280x800.png` wartet nach der Rollenaktion auf das geschlossene Auswahlfenster, die URL mit `roleId=Default`, den gerenderten Wissensbaum und den sichtbaren Root-Titel `Browser-Testwissen`. Der Capture zeigt damit den bestätigten Root-Zustand; die ursprüngliche Audit-Lücke ist für den Runner geschlossen.

## M1.3-Bestätigung

Der grüne manuelle Re-Audit-Lauf `temp/ui-audit/2026-09-20_20-05-26/03_knowledge_root_desktop_1280x800.png` bestätigt den geschlossenen Auswahlpfad und den gerenderten Root erneut. Die Capture-Lücke ist abgeschlossen; daraus wird keine neue Produktänderung abgeleitet.

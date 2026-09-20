# 19 – Rollen Working

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/19_roles_working_desktop_1280x800.png` · Route `/roles` · Working Transaction mit Anlegen/Umbenennen/Löschen · Desktop 1280×800.

## Neutrale Beobachtung

- Die Seite zeigt oben ein Formular „Rolle anlegen“ mit Name, optionaler Beschreibung und primärer Anlageaktion.
- Darunter stehen zwei bestehende Rollenkarten.
- Jede Karte zeigt Namen, RoleId und Beschreibung.
- Umbenennen und Löschen stehen rechts in jeder Karte.
- Working-Kontext ist vor allem durch Formular und Schreibaktionen ableitbar, nicht durch einen kurzen Statussatz.
- Der globale Header zeigt „Keine Rolle ausgewählt“, obwohl Rollen bearbeitet werden.
- Die untere Rollenkarte reicht nahe an den unteren Viewportrand.

## Probleme und Schwere

- **P1 – Kontext:** Nicht unmittelbar sichtbar, wann Änderungen wirksam werden und welchem Transaction-Lifecycle sie folgen.
- **P1 – Technik:** RoleId verdrängt Zweck und Beschreibung als Zielgruppeninformation.
- **P2 – Aktion:** Löschen besitzt eine ähnliche visuelle Präsenz wie die nicht-destruktive Umbenennung.
- **P2 – Globaler Zustand:** Seiten- und Shellkontext wirken nicht vollständig abgestimmt.
- **P2 – Above the fold:** Weitere Zustände/Aktionen können unterhalb des ersten Bereichs liegen.

## Gelungene Aspekte

- Das Anlegen-Formular ist klar vom Rollenbestand getrennt.
- Die primäre Anlageaktion ist sichtbar und verständlich benannt.
- Bestehende Rollen, Beschreibungen und Schreibaktionen sind direkt auffindbar.
- Die Seite vermittelt mehr Handlungsfähigkeit als der Read-only-Zustand.

## Folgerungen ohne Featureausweitung

- Working-/Commit-Kontext mit bestehender Information sichtbar rahmen.
- RoleId progressiv offenlegen und Name/Beschreibung voranstellen.
- Destruktive Aktion sekundär und klar getrennt von Umbenennen führen.
- Keine neue Rollenverwaltung und keine neue Persistenzaktion ergänzen.

## Abhängigkeiten

Rollen-, Working- und Commit-Verträge, globaler Kontext, Keyboard-/Dialogpfad; M1.3-Re-Audit.

## Audit-Lücken

Keine.

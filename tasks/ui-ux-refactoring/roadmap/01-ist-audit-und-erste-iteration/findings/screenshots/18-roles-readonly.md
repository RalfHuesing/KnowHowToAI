# 18 – Rollen Read-only

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/18_roles_readonly_desktop_1280x800.png` · Route `/roles` · Read-only-Rollenliste · Desktop 1280×800.

## Neutrale Beobachtung

- Die Seite trägt den Titel „Wissensrollen“ und erklärt Zielgruppenrollen.
- Ein sichtbarer Hinweis erklärt, dass Rollen nur in einer offenen Working Transaction geändert werden können.
- Zwei Rollen-Karten zeigen Namen, RoleId und Beschreibung.
- Es gibt im Read-only-Zustand keine Umbenennen-/Löschen-Aktionen.
- Der globale Header zeigt gleichzeitig „Keine Rolle ausgewählt“.
- Viel freie Fläche bleibt unterhalb der Rollenliste.
- RoleId ist als regulärer sichtbarer Kartentext platziert, nicht als Experteninformation.

## Probleme und Schwere

- **P1 – Technik:** RoleId ist für Support/Consultants zu prominent gegenüber Name und Zweck.
- **P1 – Kontext:** „Keine Rolle ausgewählt“ im globalen Header kann der verständlichen Read-only-Erklärung widersprechen.
- **P2 – Dichte:** Große Leerfläche lässt die Seite unfertig bzw. aufgabenarm wirken.
- **P2 – Aufgabe:** Sortierung oder nächste Rollenaktion ist nicht sichtbar, obwohl die Liste der zentrale Inhalt ist.

## Gelungene Aspekte

- Read-only wird explizit und fachlich korrekt erklärt.
- Die Unterscheidung zu Authentifizierungs-/ACL-Rollen wird verständlich gemacht.
- Keine irreführenden deaktivierten Schreibaktionen werden angezeigt.
- Rollenname und Beschreibung sind gut lesbar.

## Folgerungen ohne Featureausweitung

- Rollenname und Zweck vor RoleId führen; ID progressiv in einem Expertenbereich anzeigen.
- Globalen Kontext und Seitenkontext sprachlich konsistent ordnen.
- Die vorhandene Liste kompakter und inhaltlich führender gestalten.
- Keine Authentifizierungs- oder ACL-Bedeutung ergänzen.

## Abhängigkeiten

Rollenvertrag, Read-only-/Working-Kontext, globale Rollenauswahl und M1.3-Re-Audit.

## Audit-Lücken

Keine.

# 18 – Zielgruppen Read-only

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/18_audiences_readonly_desktop_1280x800.png` · Route `/audiences` · Read-only-Zielgruppenliste · Desktop 1280×800.

## Neutrale Beobachtung

- Die Seite trägt den Titel „Wissenszielgruppen“ und erklärt Zielgruppen.
- Ein sichtbarer Hinweis erklärt, dass Zielgruppen nur in einer offenen Working Transaction geändert werden können.
- Zwei Zielgruppen-Karten zeigen Namen, AudienceId und Beschreibung.
- Es gibt im Read-only-Zustand keine Umbenennen-/Löschen-Aktionen.
- Der globale Header zeigt gleichzeitig „Keine Zielgruppe ausgewählt“.
- Viel freie Fläche bleibt unterhalb der Zielgruppenliste.
- AudienceId ist als regulärer sichtbarer Kartentext platziert, nicht als Experteninformation.

## Probleme und Schwere

- **P1 – Technik:** AudienceId ist für Support/Consultants zu prominent gegenüber Name und Zweck.
- **P1 – Kontext:** „Keine Zielgruppe ausgewählt“ im globalen Header kann der verständlichen Read-only-Erklärung widersprechen.
- **P2 – Dichte:** Große Leerfläche lässt die Seite unfertig bzw. aufgabenarm wirken.
- **P2 – Aufgabe:** Sortierung oder nächste Zielgruppenaktion ist nicht sichtbar, obwohl die Liste der zentrale Inhalt ist.

## Gelungene Aspekte

- Read-only wird explizit und fachlich korrekt erklärt.
- Die Unterscheidung zu Authentifizierungs-/ACL-Zielgruppen wird verständlich gemacht.
- Keine irreführenden deaktivierten Schreibaktionen werden angezeigt.
- Zielgruppenname und Beschreibung sind gut lesbar.

## Folgerungen ohne Featureausweitung

- Zielgruppenname und Zweck vor AudienceId führen; ID progressiv in einem Expertenbereich anzeigen.
- Globalen Kontext und Seitenkontext sprachlich konsistent ordnen.
- Die vorhandene Liste kompakter und inhaltlich führender gestalten.
- Keine Authentifizierungs- oder ACL-Bedeutung ergänzen.

## Abhängigkeiten

Zielgruppenvertrag, Read-only-/Working-Kontext, globale Zielgruppenauswahl und M1.3-Re-Audit.

## Audit-Lücken

Keine.

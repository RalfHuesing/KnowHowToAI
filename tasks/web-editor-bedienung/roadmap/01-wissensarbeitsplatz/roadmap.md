# M1 – Wissensarbeitsplatz für Desktop und Maus

## Ziel und Grenze

Node-Kennungen verschwinden aus nutzerlesbarem Text, der Baum wird nur mit der
Maus verschoben und behält geöffnete Zweige während der laufenden Ansicht.
Vier Reiter trennen Lesen, Titel/Beschreibung, Inhaltseditor und technische
Angaben. Core, Storage, MCP und Routenformat bleiben unverändert. Maßgeblich
ist das [Konzept](../../Konzept.md).

## Nicht-Ziele

Besondere Tastaturbedienung für Baum, Verschieben oder Reiter sowie ein
Tastatur-/Fokus-Abnahmenachweis gehören nicht zu diesem Milestone. Frühere
Tests oder Ist-Beschreibungen sind kein Auftrag, diese Bedienung erneut
einzubauen. Native Texteingabe und Browserfunktionen werden nicht blockiert.

## Reihenfolge

- [ ] [M1.1-T1 – Sichtbare Node-Kennungen und Herkunft bereinigen](tasks/M1.1-T1.md)
- [ ] [M1.2-T1 – Baum auf Maus-Drag-and-drop ausrichten](tasks/M1.2-T1.md)
- [ ] [M1.3-T1 – Aufklappzustand bei Cache und Moves erhalten](tasks/M1.3-T1.md)
- [ ] [M1.4-T1 – Vier Node-Reiter und getrennte Speicherung](tasks/M1.4-T1.md)
- [ ] [M1.5-T1 – Routeübergreifende Browser-Abnahme und Ist-Doku](tasks/M1.5-T1.md)

Jeder Leaf schließt eigene Tests und zwingende Dokumentationsfolgen vor seinem
Commit. Kein Leaf setzt fremde Checkboxen vorzeitig auf erledigt.

## Milestone-Abnahme

- [ ] Auf `/knowledge` und `/knowledge/{NodeId}` sind keine Node-GUIDs als
      nutzerlesbarer Text sichtbar; die URL und interne IDs funktionieren.
- [ ] Maus-Drop vor/nach/unter einschließlich Ablehnung und Recovery ist
      belegt. Die sichtbaren Move-Buttons und die spezielle
      Baum-Tastatursteuerung sind entfernt. Geöffnete Zweige bleiben während
      der Ansicht offen, auch nach Move/Cache-Verdrängung; Reload und
      Zielgruppenwechsel starten frisch. Höchstens zehn Elternseiten sind
      geladen.
- [ ] Die vier Reiter funktionieren für normalen, Fallback-, fehlenden und
      Derived Content. Titel/Beschreibung und Inhalt speichern getrennt;
      Dirty-Eingaben überleben Reiterwechsel. Ein `h1`, Seiten-Innenkanten,
      Reflow und erreichbare Mausaktionen sind nachgewiesen.
- [ ] `docs/WebUi.md`, betroffene Web-UI-Regeln und Tests beschreiben den
      implementierten Zustand; Build, vollständige FastTests, relevante
      Browser-/Host-Integration und Solution-Linter-Gate sind grün.
- [ ] Menschliche Chrome-Stable-Sichtprüfung bei echtem 200/400-%-Zoom nach
      dem Zoom-/Reflow-Teil von `docs/Manuelle-UI-Abnahme.md` für die
      geänderten Mausabläufe dokumentiert. Dies ist ein manuelles Gate ohne
      `-T` und kein Agentenauftrag; Baum-/Reiter-Tastaturbedienung gehört
      nicht zu dieser Abnahme.
- [ ] Begrenzter Audit gegen Konzept, Diff, Regeln und Nachweise dokumentiert;
      Parent erst nach dem menschlichen Gate schließen.

Audit-Ergebnis: offen.

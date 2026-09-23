# M1-Audit – Wissensarbeitsplatz für Desktop und Maus

## Ergebnis

Bestanden für den automatisiert belegten Milestone-Umfang; keine Findings.
Die vier automatisiert belegbaren Milestone-Abnahmepunkte sind in der
[Milestone-Roadmap](roadmap.md) abgehakt. Die echte Chrome-Stable-Prüfung bei
200/400 % bleibt ein menschliches Gate; der Projekt-Parent bleibt bis dahin
offen.

## Geprüfter Umfang

- Abgleich des [Konzepts](../../Konzept.md), aller fünf Leaf-Dateien und ihrer
  Abschlussnachweise mit dem Milestone-Ziel und den Invarianten.
- Review des Diffs und der Commits `44888f2..fae76ae` mit Fokus auf
  Node-Kennungen/Herkunft, Baum-Drag-and-drop und Expansion-Cache,
  Node-Reiter/Dirty-Zustand sowie Browser- und Dokumentationsnachweise.
- Abgleich mit `docs/WebUi.md`, `docs/Manuelle-UI-Abnahme.md`,
  `docs/Invarianten.md`, `.agents/rules/Richtlinien.mdc`,
  `.agents/rules/TestRichtlinien.mdc`,
  `.agents/rules/WebUiHtmlCss.mdc` und
  `.agents/rules/DokuRichtlinien.mdc`.

## Befund und Nachweise

- **Node-Identität:** In der gerenderten Knotendarstellung stehen Titel und
  Kontext; IDs verbleiben in Route und internen Attributen. Herkunftstitel
  werden im selben `ReadContext` geladen; bei fehlender Quelle oder Fehler
  erscheint „Quellknoten nicht verfügbar“. Die Leaf-Nachweise führen
  Komponenten-/Mapping- und Browser-Smokes für sichtbaren Text, Direktaufruf
  und Reload auf.
- **Baum und Cache:** Die Move-Schaltflächen und eigens implementierte
  Treeview-Tastatursteuerung sind entfernt. Der Pointer-Ablauf behält
  Drop-Indikator, Validierung und serverbestätigte Mutation; die Browser-Smokes
  belegen Before/Parent/After, ungültige und abgelehnte Drops sowie Recovery.
  Die geladene Elternseitenzahl bleibt auf zehn begrenzt; Eviction leert
  Seitendaten, aber nicht die flüchtige Aufklappabsicht. Der Nachweis umfasst
  Cache-Druck, gezieltes Nachladen, Move/Recovery und frischen Zustand nach
  Reload.
- **Node-Arbeitsfläche:** Die vier nativen Ansichts-Schaltflächen behalten
  getrennte Metadaten- und Content-Speicheraktionen. Bearbeitungskomponenten
  bleiben beim Ansichtswechsel montiert; Fallback/None starten eine leere
  eigene Fassung und Derived-Content bleibt schreibgeschützt. Die Nachweise
  belegen Dirty-Eingaben, Reiterwechsel, Fallback/None/Derived und genau ein
  `h1`.
- **Qualität und Dokumentation:** Die Leaf-Abschlussnachweise nennen
  erfolgreiche Builds, vollständige FastTests, passende Browser-Smokes bzw.
  Integration und Solution-Linter `pass`/`10.0`/`0` Verstöße. M1.5 belegt
  38/38 Browser-Integrationstests, 41 gemeinsam geprüfte Screenshots für
  betroffene Zustände und CSS-Breiten, Build, FastTests und Linter. Die
  geänderte Ist-Doku und Web-UI-Regel stimmen mit dem geprüften Code überein.
  `git diff --check 44888f2..HEAD` ist sauber.

Im geprüften Soll-Scope wurden keine Sicherheits-, Datenintegritäts- oder
Regressionsfehler gefunden. Build, Tests und Linter wurden für dieses Audit
nicht erneut ausgeführt; die referenzierten Leaf-Nachweise sind die Grundlage.

## Offenes menschliches Gate

Die manuelle Sichtprüfung mit echtem Chrome-Zoom bei 200 und 400 % ist nicht
durch CSS-Breiten, automatisierte Assertions oder Screenshots ersetzt. Sie
bleibt gemäß `docs/Manuelle-UI-Abnahme.md` offen. Danach kann der Projekt-Parent
in `tasks/web-editor-bedienung/roadmap.md` geschlossen werden.

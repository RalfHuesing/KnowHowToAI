# Planungs- und Ausführungspattern für Roadmaps

Dieses Dokument beschreibt ein wiederverwendbares, pragmatisches Pattern für
agentisch bearbeitete Vorhaben. Es ist ein Planungsstandard, kein Skill und
keine Beschreibung des implementierten Ist-Zustands. Fachliche Anforderungen
und Projektregeln bleiben in den jeweils zuständigen Dokumenten des Projekts.

## Zweck und Grundidee

Ein Modell mit höherer Reasoning-Kapazität zerlegt ein Vorhaben in kleine,
ausführbare Arbeitspakete und hält die Entscheidungen schriftlich fest. Ein
günstigeres Ausführungsmodell arbeitet danach einen einzelnen Leaf-Task anhand
der verbindlichen Abnahmepunkte ab. Ein begrenzter Audit prüft am Ende das
Ergebnis. Die Dokumentation soll Entscheidungen und Vergessensschutz liefern,
nicht jede Implementierungszeile vorwegnehmen.

## Hierarchie

```text
Projekt
└─ Roadmap-Index
   └─ Milestone-Verzeichnis Mx
      ├─ roadmap.md                  # Milestone-Übersicht und aggregierter Status
      ├─ planning.md                 # optionales manuelles Planungsartefakt
      ├─ audit.md                    # optionales manuelles Audit-Artefakt
      └─ tasks/
         └─ Mx.y-Tz.md               # genau ein ausführbarer Leaf-Task
```

- Ein **Milestone** ist ein fachlich zusammenhängendes Ergebnis mit Ziel,
  Abhängigkeiten und eigener Abnahme.
- Ein **Arbeitspaket** (`Mx.y`) bündelt logisch zusammengehörige Schritte. Es
  wird in der Milestone-`roadmap.md` beschrieben und ist kein automatisch
  ausführbarer Auftrag.
- Ein **Leaf-Task** (`Mx.y-Tz`) ist die kleinste ausführbare Einheit. Er muss
  so geschnitten sein, dass ein Ausführungsmodell keine Produkt- oder
  Architekturentscheidung mehr erfinden muss.
- Ein Planungs-/Konzept-Gate ist bei Bedarf ein manuelles Artefakt wie
  `planning.md`; ein Milestone-Audit kann in `audit.md` festgehalten werden.
  Beide erhalten ausdrücklich keine `-T`-ID. Sie schließen Entscheidungen
  beziehungsweise die Abnahme des Milestones und sind keine
  Implementierungs-Leaf-Tasks.

## Single Source of Truth

`roadmap.md` ist die Quelle für Milestone-Ziel, Reihenfolge, Abhängigkeiten,
die Liste der Leaf-Tasks und den aggregierten Erledigt-/Freigabestatus. Die
jeweilige Leaf-Datei ist die einzige verbindliche Detailquelle für ihren Task.
Ihre Checkliste dokumentiert den Detailfortschritt und die Nachweise, ist aber
keine zweite Quelle für den übergeordneten Erledigtstatus. Keine
Detailbeschreibung wird in mehreren Dateien kopiert; andere Dokumente
verlinken stattdessen darauf.

Die Projekt-Roadmap bzw. der Index verlinkt jedes Milestone-Verzeichnis. Eine
Detaildatei darf den Ist-Stand, Regeln oder Konzepte referenzieren, ersetzt sie
aber nicht. Implementierter Ist-Zustand bleibt in der dafür vorgesehenen
Dokumentation des Projekts.

## Struktur und Benennung

```text
tasks/<projekt>/roadmap/
└─ <laufende-nr>-<milestone-kurzname>/
   ├─ roadmap.md
   ├─ planning.md                  # optional, manuelles Planungs-Gate
   ├─ audit.md                     # optional, manueller Milestone-Audit
   └─ tasks/
      ├─ Mx.1-T1.md
      └─ Mx.1-T2.md
```

Milestone-Verzeichnisse verwenden eine stabile laufende Nummer, Kleinbuchstaben
und Bindestriche. Leaf-Dateien verwenden ausschließlich ihre stabile ID
(`Mx.y-Tz`) als Dateinamen; dadurch bleiben Links auch bei einer geänderten
Bezeichnung unverändert. Ein Task wird geteilt, wenn er mehrere
unabhängige Entscheidungen, mehrere schwer abgrenzbare Ergebnisse oder einen
zu großen Kontext erfordert.

## Verbindlich und empfohlen

In einer Detaildatei sind Aussagen als **Verbindlich**, **Empfohlen** oder
**Neu zu bewerten** zu kennzeichnen:

- **Verbindlich:** Zielverhalten, Invarianten, Scope, Nicht-Ziele,
  Abnahmekriterien und notwendige Nachweise.
- **Empfohlen:** vorgeschlagene Reihenfolge, Dateiaufteilung und technische
  Umsetzung, sofern der geprüfte Ist-Stand nicht widerspricht.
- **Neu zu bewerten:** konkrete Details, die erst während der Arbeit anhand
  des aktuellen Codes oder einer begründeten neuen Erkenntnis entschieden
  werden dürfen.

So bleibt der Plan deterministisch, ohne veraltete Detailvorgaben künstlich
gegen den Ist-Stand durchzusetzen.

## Obligatorische Checklisten

Jeder zur Ausführung freigegebene, neu geplante oder neu geschnittene
Leaf-Task enthält eine vollständige `- [ ]`-Checkliste. Checklisten sind ein
absichtlicher Gedächtnisschutz für schwächere Ausführungsmodelle: Jeder Punkt
muss sichtbar abgehakt oder mit einer begründeten Abweichung dokumentiert
werden. Ein Punkt beschreibt ein prüfbares Ergebnis, nicht bloß eine
Tippbewegung.

Die Checkliste deckt mindestens Vorbereitung, Umsetzung, Abnahme und
Abschlussnachweis ab. Parent-Checkboxen in `roadmap.md` sind nur Aggregate;
eine übergeordnete Checkbox wird erst geschlossen, wenn alle direkten Kinder
und deren Abnahme erfüllt sind.

Bei einer reinen Strukturmigration dürfen historische Leaf-Dateien zunächst
ihre bisherige Einzel-Checkbox und ihre Nachweise im Altformat behalten. Diese
Dateien sind noch nicht zur Delegation freigegeben: Beim nächsten Planungs-
oder Ausführungsgate werden sie in das vollständige Schema überführt. Die
historische Einzel-Checkbox ist bis dahin nur Migrationsbestand und keine
eigene Statusquelle; maßgeblich bleibt ausschließlich die Checkbox in
`roadmap.md`.

## Inhalt eines freigegebenen Leaf-Tasks

Jede zur Ausführung freigegebene oder neu geplante Detaildatei enthält
mindestens:

1. **Intention:** Warum ist der Task nötig und welches Ergebnis entsteht?
2. **Voraussetzungen:** Abhängige Tasks, Entscheidungen und Pflichtlektüre.
3. **Scope:** Was wird geändert?
4. **Nicht-Ziele:** Was bleibt ausdrücklich unangetastet?
5. **Verträge und Invarianten:** Was darf sich nicht ändern bzw. muss gelten?
6. **Umsetzungsvorschlag:** bevorzugter Weg, klar als empfohlen markiert.
7. **Akzeptanzkriterien:** beobachtbare, überprüfbare Ergebnisse.
8. **Testbudget:** erforderliche Testebenen und ausdrücklich nicht nötige
   Wiederholungen oder theoretische Testvarianten.
9. **Abschlussnachweis:** geänderte Dateien, relevante Befehle/Ergebnisse,
   aktualisierte Roadmap-Checkbox und Commit.

## Rollen und Ablauf

- **Planer:** untersucht Ist-Stand und Ziele, schneidet Milestones und
  Leaf-Tasks, dokumentiert Entscheidungen und Checklisten.
- **Ausführer:** bearbeitet genau einen freigegebenen Leaf-Task, bleibt im
  Scope, führt das festgelegte Testbudget aus und hinterlässt Nachweise.
- **Auditor:** prüft den abgeschlossenen Milestone gegen seine Kriterien,
  Invarianten und belegten Nachweise.
- **Menschliche Entscheidung:** löst Produktentscheidungen, offene Fragen
  und echte Scope-Konflikte.

Die Standardreihenfolge ist: planen und freigeben, Leaf-Tasks in dokumentierter
Reihenfolge ausführen, Milestone begrenzt auditieren. Parallelität ist nur
sicher, wenn Schreibbereiche, Verträge und Abhängigkeiten disjunkt sind; bei
gemeinsamen Dateien, Datenmodellen oder aufeinander aufbauenden Änderungen
wird sequenziell gearbeitet.

## Stop, Eskalation und Auditgrenzen

Der Ausführer stoppt statt zu raten, wenn eine offene Entscheidung, fehlende
Voraussetzung, widersprüchliche Regel oder ein unerwartet größerer Scope den
Task blockiert. Die Blockade, die betroffene Checkbox und die benötigte
Entscheidung werden dokumentiert und eskaliert.

Der Audit prüft nur den geplanten Milestone: Abnahmekriterien, Invarianten,
Sicherheit, Datenintegrität, Build/Test-Nachweise soweit im Testbudget
vorgesehen und erkennbare Regressionen. Er darf keine neuen Produkt-
anforderungen eröffnen. Verbesserungen ohne konkreten Verstoß kommen in einen
separaten Backlog. Pro Milestone ist höchstens eine gezielte Korrekturrunde
vorgesehen; danach entscheidet der Mensch über Abnahme oder neue Planung.

## Status und Commitgrenzen

Der aggregierte Status wird an einer Stelle gepflegt: in der Milestone-
`roadmap.md` über die verlinkten Leaf-Checkboxen und Milestone-Kriterien. Die
Leaf-Datei führt daneben ihre Detail-Checkliste und Nachweise; sie wiederholt
nicht den übergeordneten Erledigtstatus als eigene Wahrheit. Ein Task ist erst
erledigt, wenn Umsetzung, Nachweise und Dokumentationsfolgen abgeschlossen
sind und die zugehörige Checkbox in `roadmap.md` geschlossen wurde. Ein
Milestone ist erst abgeschlossen, wenn seine Kinder erledigt und der Audit
dokumentiert ist.

Jeder abgeschlossene Leaf-Task erhält einen kleinen, atomaren Commit mit nur
seinem Scope und den zwingenden Dokumentationsfolgen. Parent- und Milestone-
Checkboxen werden im passenden Commit aktualisiert. Nicht beauftragte
Aufräumarbeiten, spekulative Tests und Nachbarfeatures bleiben separate Tasks.

## Vorlage: Milestone-`roadmap.md`

```markdown
# Mx – <Milestone-Name>

## Ziel

<Ein überprüfbares Milestone-Ergebnis.>

## Abhängigkeiten und Freigabe

- Voraussetzungen: <...>
- Freigabe: <z. B. `planning.md` abgeschlossen / keine>

## Arbeitspakete und Leaf-Tasks

### Mx.1 – <Arbeitspaket>

- [ ] [Mx.1-T1 – <Name>](tasks/Mx.1-T1.md)
- [ ] [Mx.1-T2 – <Name>](tasks/Mx.1-T2.md)

## Milestone-Abnahme

- [ ] <Abnahmekriterium 1>
- [ ] <Abnahmekriterium 2>

## Audit

- [ ] Begrenzter Audit gegen Ziel, Kriterien, Invarianten und Nachweise
- Ergebnis: <offen / bestanden / Korrekturrunde / zurück in Planung>
```

## Vorlage: Leaf-Taskdatei

```markdown
# Mx.y-Tz – <Task-Name>

## Intention

<Warum und erwartetes Ergebnis.>

## Voraussetzungen und Referenzen

- <Abhängigkeiten, Entscheidungen, Pflichtlektüre>

## Scope

- <Verbindliche Änderung 1>

## Nicht-Ziele

- <Bewusst nicht enthalten>

## Verträge und Invarianten

- **Verbindlich:** <...>
- **Empfohlen:** <...>

## Umsetzungsvorschlag

<Kurzer, anpassbarer Lösungsweg.>

## Akzeptanz und Testbudget

- [ ] <Beobachtbares Akzeptanzkriterium>
- [ ] <Erforderlicher Test/Nachweis>
- Nicht erforderlich: <bewusst ausgeschlossene Testebenen>

## Checkliste

- [ ] Voraussetzungen und aktuellen Ist-Stand geprüft
- [ ] Scope umgesetzt, Nicht-Ziele eingehalten
- [ ] Akzeptanzkriterien und Testbudget erfüllt
- [ ] Dokumentation aktualisiert und die zugehörige Checkbox in `roadmap.md`
      erst nach erfüllter Abnahme geschlossen
- [ ] Abschlussnachweis und atomarer Commit erstellt

## Abschlussnachweis

- Geänderte Dateien: <...>
- Nachweise: <Befehle/Ergebnisse oder begründete Entfalle>
- Abweichungen/Blockaden: <keine / ...>
```

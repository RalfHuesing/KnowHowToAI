# Rollen und Content

## Rollenmodell

Rollen sind vollständig frei definierbar (z. B. `Default`, `Developer`,
`Consultant`, `EndUser`, `Administrator`, `Support`, `AI`). Eine feste,
in der Geschäftslogik kodierte Rollenstruktur gibt es nicht. Das Seed-Skript legt
die initiale Rolle `Default` samt Resolution Order an; danach werden Rollen und
ihre Resolution Orders wie jeder andere versionierte Wissenszustand innerhalb
einer Transaction über die Service-/MCP-Grenzen gepflegt (`create_role`,
`update_role`, `delete_role`, `set_role_resolution`). Eine
Administrationsoberfläche ist kein Bestandteil von V1.

## Atomarer Schreibschutz gegen stale Writes

Alle Rollen- und Content-Mutationen benötigen eine offene `TransactionId` und
akzeptieren den zuvor gelesenen `expectedChangeVersion`-Stand. Die Prüfung
erfolgt unter derselben kurzen Working-Snapshot-Sperre wie die fachliche
Mutation. Bei einer Abweichung wird der stabile Fehler `ChangeVersionConflict`
mit `expectedChangeVersion` und `actualChangeVersion` geliefert; Working
Snapshot und ChangeVersion bleiben unverändert. Erfolgreiche Mutationen liefern
die neue ChangeVersion bis zu MCP und Web zurück.

## Wissensrolle ist keine Berechtigungsrolle

Eine Rolle beschreibt: *Für welche Zielgruppe ist dieser Inhalt geschrieben?*
Sie beschreibt **nicht**: *Wer darf diesen Inhalt lesen oder ändern?* Authentifizierung,
Autorisierung und ACLs sind ein separates, späteres Thema. Diese Trennung bleibt
erhalten.

## Rollenabhängiger Content

Ein Node kann für jede Rolle eigenen Content besitzen:

```text
Node: Auftragserfassung
Default:     allgemeine fachliche Beschreibung
Consultant:  Prozess- und Konfigurationswissen
Developer:   technische Implementierungsdetails
EndUser:     Bedienungsanleitung
```

Es ist ausdrücklich erlaubt, dass für eine Rolle kein eigener Content existiert.
Fehlender expliziter Content ist kein automatisch Qualitätsfehler; er kann
bedeuten, dass Fallback genügt, der Node für die Rolle irrelevant ist oder die
Dokumentation noch nicht erstellt wurde. Die MCP-Antwort macht transparent, ob
`Explicit`, `Fallback` oder `None` verwendet wird.

## Role Resolution Orders

Für jede angefragte Rolle existiert eine frei konfigurierbare, geordnete
Kandidatenliste:

```text
RequestedRole = Developer
Resolution order:
1. Developer
2. Consultant
3. Default
```

Es handelt sich bewusst nicht um objektorientierte Vererbung. Die Reihenfolge ist
explizit gespeichert, deterministisch und wird **nicht rekursiv** aufgelöst;
zyklische Fallback-Ketten können dadurch nicht entstehen. Regeln:

- die angefragte Rolle steht normalerweise an erster Position,
- eine Rolle kommt innerhalb einer Order nicht mehrfach vor,
- es kann vorkommen, dass für keinen Kandidaten Content existiert.

`set_role_resolution` ersetzt die Order der angefragten Rolle vollständig.

## Transparenz der Auflösung

```text
get_node(nodeId, role = Developer)
```

liefert beispielsweise:

```text
requestedRole = Developer
resolvedRole = Consultant
fallbackUsed = true
```

Die tatsächlich verwendete Rolle wird nie implizit verborgen. Rollen werden in
allen contentbezogenen MCP-Aufrufen explizit als `roleId` übergeben; es gibt keinen
unsichtbaren globalen Rollenstatus pro Session.

## Keine unnötigen Duplikate

Rollen-Fallback verhindert, dass identischer Content mehrfach gespeichert wird.
Ist derselbe Text für alle Zielgruppen geeignet, genügt der Default-Content;
es wird keine Kopie pro Rolle erzeugt. Eigener Rollen-Content wird nur gespeichert,
wenn die Darstellung tatsächlich abweicht.

## Fallback löst keinen inhaltlichen Drift

Fallback beantwortet nur: *Welchen Content verwenden, wenn kein eigener
vorhanden ist?* Er beantwortet nicht: *Ist ein vorhandener Rollen-Content noch
fachlich aktuell?* Besitzt eine Rolle eigenen (möglicherweise veralteten) Content,
greift kein Fallback. Dafür existiert der unten beschriebene
Provenienz-Mechanismus.

## Content-Revisions

Jeder explizite Rollen-Content besitzt eine logische `ContentRevisionId`
(GUID). Wird ein Snapshot kopiert, bleibt die Revision identisch, solange sich der
Inhalt nicht ändert; bei einer Inhaltsänderung entsteht eine neue Revision. Damit
ist feststellbar, ob sich eine fachliche Quelle geändert hat – unabhängig davon,
wie viele Snapshots inzwischen entstanden sind.

## Content-Abhängigkeiten und Provenienz

Expliziter Rollen-Content kann aus anderen Wissensinhalten abgeleitet sein:

```text
Target: NodeId + RoleId
Source:  NodeId + RoleId + ContentRevisionId
```

Eine Abhängigkeit kann auch auf einen anderen Node zeigen; das System ist nicht
auf Rollen desselben Nodes beschränkt.

## Independent und Derived

Expliziter Rollen-Content hat einen von zwei Modi:

- **Independent**: der Inhalt ist für diese Rolle eigenständig maßgeblich.
- **Derived**: der Inhalt wurde aus anderen Wissensinhalten abgeleitet; die
  verwendeten Source-Revisions werden strukturiert gespeichert.

Ändert sich eine Source-Revision, wird der abgeleitete Inhalt stale. Ist eine
Source selbst `Derived` und stale, ist der davon abhängige Content transitiv
stale. Dependency-Zyklen (direkt oder indirekt) sind eine harte Invariante und
werden abgelehnt.

## Stale-Erkennung

```text
EndUser-Content basiert auf Developer Revision 17.
Developer wird auf Revision 18 geändert.
=> EndUser ist Stale.
```

Eine bestehende Provenienz bleibt erhalten, auch wenn ihre Source später
tombstoned oder in einem historischen Zustand fehlt: der Derived-Content wird
`Stale`, der Snapshot bleibt valide. Ausschließlich beim Anlegen oder Ändern einer
Dependency muss die Source als aktiver expliziter Content existieren. So werden
weder neue ungültige Abhängigkeiten akzeptiert noch die Nachvollziehbarkeit
entstandener Ableitungen verloren. Der abgeleitete Text wird nicht automatisch
verändert; das System signalisiert nur, dass der Content auf einem älteren
Wissensstand basiert.

## Availability und Freshness

Zwei getrennte Dimensionen:

- **Availability**: `Explicit`, `Fallback`, `None`
- **Freshness**: für expliziten abgeleiteten Content `Current` oder `Stale`;
  für unabhängigen Content entfällt die Abhängigkeitsprüfung

```text
RequestedRole: EndUser    →  Availability: Explicit,  Freshness: Stale
RequestedRole: Developer  →  Availability: Fallback, ResolvedRole: Consultant, Freshness: Current
```

Beispielhafte Angaben einer Antwort sind also kombinierbar und unabhängig
voneinander aussagekräftig.

## Keine automatische Synchronisation

Das System versucht nicht, bei jeder Änderung alle Rollen sofort zu synchronisieren.
Automatische Neugenerierung bei jeder Iteration würde unnötige LLM-Aufrufe,
Rauschen und schlechte Nachvollziehbarkeit erzeugen. Drift wird sichtbar gemacht
und später bewusst bearbeitet: ein Synchronisations-Agent erhält die stale
Inhalte, aktuellen Quellen und fehlenden Inhalte und aktualisiert die abgeleitete
Dokumentation in einer eigenen Transaction mit neuen Source-Revisions
([Intention](Intention.md)). Ein normaler Commit oder Release darf stale Derived
Content enthalten; harte Release-Policies existieren in V1 bewusst nicht
([Entscheidungen](Entscheidungen.md)).

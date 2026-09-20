# Zielgruppen und Content

## Zielgruppenmodell

Zielgruppen sind vollständig frei definierbar (z. B. `Default`, `Developer`,
`Consultant`, `EndUser`, `Administrator`, `Support`, `AI`). Eine feste,
in der Geschäftslogik kodierte Rollenstruktur gibt es nicht. Das Seed-Skript legt
die initiale Zielgruppe `Default` samt Resolution Order an; danach werden Zielgruppen und
ihre Resolution Orders wie jeder andere versionierte Wissenszustand innerhalb
einer Transaction über die Service-/MCP-Grenzen gepflegt (`create_role`,
`update_role`, `delete_role`, `set_role_resolution`). Die Weboberfläche stellt
die Rollenpflege unter `/roles` bereit; Resolution Orders bleiben dort bis zur
separaten Umsetzung read-only. Die Seite lädt Zielgruppen für den über Query
gewählten Current-, Snapshot-, Release- oder Working-Kontext. Nur eine offene
Working Transaction erlaubt Erstellen, Umbenennen und Löschen; historische und
committed Kontexte zeigen dieselben Zielgruppen schreibgeschützt.

## Atomarer Schreibschutz gegen stale Writes

Alle Zielgruppen- und Content-Mutationen benötigen eine offene `TransactionId` und
ein verpflichtendes `expectedChangeVersion`-Feld mit dem zuvor gelesenen Stand.
Fehlt der Versionsstand, wird der Aufruf bereits am jeweiligen Vertrag abgelehnt. Die Prüfung
erfolgt unter derselben kurzen Working-Snapshot-Sperre wie die fachliche
Mutation. Bei einer Abweichung wird der stabile Fehler `ChangeVersionConflict`
mit `expectedChangeVersion` und `actualChangeVersion` geliefert; Working
Snapshot und ChangeVersion bleiben unverändert. Erfolgreiche Mutationen liefern
die neue ChangeVersion bis zu MCP und Web zurück.

## Wissenszielgruppe ist keine Berechtigungsrolle

Eine Zielgruppe beschreibt: *Für welche Zielgruppe ist dieser Inhalt geschrieben?*
Sie beschreibt **nicht**: *Wer darf diesen Inhalt lesen oder ändern?* Authentifizierung,
Autorisierung und ACLs sind ein separates, späteres Thema. Diese Trennung bleibt
erhalten.

## Zielgruppenabhängiger Content

Ein Node kann für jede Zielgruppe eigenen Content besitzen:

```text
Node: Auftragserfassung
Default:     allgemeine fachliche Beschreibung
Consultant:  Prozess- und Konfigurationswissen
Developer:   technische Implementierungsdetails
EndUser:     Bedienungsanleitung
```

Es ist ausdrücklich erlaubt, dass für eine Zielgruppe kein eigener Content existiert.
Fehlender expliziter Content ist kein automatisch Qualitätsfehler; er kann
bedeuten, dass Fallback genügt, der Node für die Zielgruppe irrelevant ist oder die
Dokumentation noch nicht erstellt wurde. Die MCP-Antwort macht transparent, ob
`Explicit`, `Fallback` oder `None` verwendet wird.

## Audience Resolution Orders

Für jede angefragte Zielgruppe existiert eine frei konfigurierbare, geordnete
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

- die angefragte Zielgruppe steht normalerweise an erster Position,
- eine Zielgruppe kommt innerhalb einer Order nicht mehrfach vor,
- es kann vorkommen, dass für keinen Kandidaten Content existiert.

`set_role_resolution` ersetzt die Order der angefragten Zielgruppe vollständig.

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

Die tatsächlich verwendete Zielgruppe wird nie implizit verborgen. Zielgruppen werden in
allen contentbezogenen MCP-Aufrufen explizit als `roleId` übergeben; es gibt keinen
unsichtbaren globalen Rollenstatus pro Session.

## Keine unnötigen Duplikate

Zielgruppen-Fallback verhindert, dass identischer Content mehrfach gespeichert wird.
Ist derselbe Text für alle Zielgruppen geeignet, genügt der Default-Content;
es wird keine Kopie pro Zielgruppe erzeugt. Eigener Zielgruppen-Content wird nur gespeichert,
wenn die Darstellung tatsächlich abweicht.

## Fallback löst keinen inhaltlichen Drift

Fallback beantwortet nur: *Welchen Content verwenden, wenn kein eigener
vorhanden ist?* Er beantwortet nicht: *Ist ein vorhandener Zielgruppen-Content noch
fachlich aktuell?* Besitzt eine Zielgruppe eigenen (möglicherweise veralteten) Content,
greift kein Fallback. Dafür existiert der unten beschriebene
Provenienz-Mechanismus.

## Content-Revisions

Jeder explizite Zielgruppen-Content besitzt eine logische `ContentRevisionId`
(GUID). Wird ein Snapshot kopiert, bleibt die Revision identisch, solange sich der
Inhalt nicht ändert; bei einer Inhaltsänderung entsteht eine neue Revision. Damit
ist feststellbar, ob sich eine fachliche Quelle geändert hat – unabhängig davon,
wie viele Snapshots inzwischen entstanden sind.

## Content-Abhängigkeiten und Provenienz

Expliziter Zielgruppen-Content kann aus anderen Wissensinhalten abgeleitet sein:

```text
Target: NodeId + RoleId
Source:  NodeId + RoleId + ContentRevisionId
```

Eine Abhängigkeit kann auch auf einen anderen Node zeigen; das System ist nicht
auf Zielgruppen desselben Nodes beschränkt.

## Independent und Derived

Expliziter Zielgruppen-Content hat einen von zwei Modi:

- **Independent**: der Inhalt ist für diese Zielgruppe eigenständig maßgeblich.
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

Das System versucht nicht, bei jeder Änderung alle Zielgruppen sofort zu synchronisieren.
Automatische Neugenerierung bei jeder Iteration würde unnötige LLM-Aufrufe,
Rauschen und schlechte Nachvollziehbarkeit erzeugen. Drift wird sichtbar gemacht
und später bewusst bearbeitet: ein Synchronisations-Agent erhält die stale
Inhalte, aktuellen Quellen und fehlenden Inhalte und aktualisiert die abgeleitete
Dokumentation in einer eigenen Transaction mit neuen Source-Revisions
([Intention](Intention.md)). Ein normaler Commit oder Release darf stale Derived
Content enthalten; harte Release-Policies existieren in V1 bewusst nicht
([Entscheidungen](Entscheidungen.md)).

# 17. Rollenmodell

Rollen sind vollständig frei definierbar.

Beispiele:

```text
Default
Developer
Consultant
EndUser
Administrator
Support
AI
```

V1 kann initial eine Rolle `Default` anlegen.

Es darf jedoch keine fest in der Geschäftslogik kodierte Rollenstruktur geben.

Neue Rollen können grundsätzlich hinzugefügt werden.

Eine Administrationsoberfläche für Rollen ist in V1 nicht Bestandteil des Projekts.

Die initiale Rolle `Default` kann über Seed-Daten angelegt werden. Danach werden
Rollen und ihre Resolution Orders wie anderer versionierter Wissenszustand innerhalb
einer KnowHowTo-AI-Transaction über Application Services und MCP-Funktionen gepflegt.

Auch bei der Rollenpflege dürfen bereits committed Snapshots nicht nachträglich verändert werden.

---

# 18. Wissensrolle ist keine Berechtigungsrolle

Eine KnowHowTo-AI-Rolle beschreibt:

```text
Für welche Zielgruppe ist dieser Inhalt geschrieben?
```

Sie beschreibt **nicht**:

```text
Wer darf diesen Inhalt lesen oder ändern?
```

`Developer`, `Consultant` oder `EndUser` sind deshalb keine Security-Rollen.

Authentifizierung, Autorisierung und ACLs sind ein separates zukünftiges Thema.

Diese Trennung muss erhalten bleiben.

---

# 19. Rollenabhängiger Content

Ein Node kann für jede Rolle eigenen Content besitzen.

Beispiel:

```text
Node: Auftragserfassung

Default:
  allgemeine fachliche Beschreibung

Consultant:
  Prozess- und Konfigurationswissen

Developer:
  technische Implementierungsdetails

EndUser:
  Bedienungsanleitung
```

Es ist ausdrücklich erlaubt, dass für eine Rolle kein eigener Content existiert.

Beispiel:

```text
Developer: vorhanden
Consultant: vorhanden
EndUser: nicht vorhanden
```

---

# 20. Rollenauflösung

Für jede angefragte Rolle existiert eine frei konfigurierbare geordnete Liste von Kandidaten.

Beispiel:

```text
RequestedRole = Developer

Resolution order:
1. Developer
2. Consultant
3. Default
```

Für Consultant könnte unabhängig davon gelten:

```text
RequestedRole = Consultant

Resolution order:
1. Consultant
2. Developer
3. Default
```

Es handelt sich bewusst nicht um klassische objektorientierte Vererbung.

Die Reihenfolge ist eine **Role Resolution Order**.

Sie wird nicht rekursiv aufgelöst.

Dadurch entstehen keine komplizierten zyklischen Fallback-Ketten.

Für jede angefragte Rolle ist die vollständige Kandidatenreihenfolge explizit gespeichert.

Regeln:

- die angefragte Rolle steht normalerweise an erster Position,
- eine Rolle darf innerhalb einer Resolution Order nicht mehrfach vorkommen,
- die Reihenfolge ist deterministisch,
- es kann vorkommen, dass für keine Rolle Content existiert.

---

# 21. Ergebnis einer Rollenauflösung

Bei einer Anfrage:

```text
get_node(
    nodeId,
    role = Developer
)
```

kann beispielsweise zurückkommen:

```text
requestedRole = Developer
resolvedRole = Consultant
fallbackUsed = true
```

Damit weiß der Agent eindeutig, dass er gerade Consultant-Wissen erhalten hat.

Die tatsächlich verwendete Rolle darf niemals implizit verborgen werden.

---

# 22. Vermeidung unnötiger Duplikate

Rollen-Fallback dient unter anderem dazu, identischen Content nicht mehrfach zu speichern.

Wenn derselbe Text für alle Zielgruppen geeignet ist:

```text
Default:
"Die Anwendung wird über Menü X gestartet."

Developer:
kein eigener Content

Consultant:
kein eigener Content

EndUser:
kein eigener Content
```

Dann können alle Rollen denselben Default-Content verwenden.

Es soll nicht automatisch für jede Rolle eine Kopie desselben Textes erzeugt werden.

Eigener Rollen-Content wird nur gespeichert, wenn die Darstellung für diese Rolle tatsächlich abweicht.

---

# 23. Rollen-Fallback löst keinen inhaltlichen Drift

Fallback beantwortet nur:

```text
Welchen Content soll ich verwenden, wenn für diese Rolle kein eigener vorhanden ist?
```

Fallback beantwortet nicht:

```text
Ist ein bereits vorhandener Rollen-Content noch fachlich aktuell?
```

Beispiel:

```text
Developer:
Version 18 beschreibt A, B und C.

EndUser:
älterer eigener Text beschreibt nur A und B.
```

Da EndUser eigenen Content besitzt, wird kein Fallback verwendet.

Trotzdem ist die EndUser-Dokumentation veraltet.

Dafür wird ein separater Mechanismus benötigt.

---

# 24. Content-Revisions

Jeder explizite Rollen-Content besitzt zusätzlich eine logische `ContentRevisionId`.

Beispiel:

```text
NodeId = ABC
RoleId = Developer
ContentRevisionId = REV-17
```

Wird ein Snapshot vollständig kopiert, bleibt die `ContentRevisionId` identisch, solange sich der Inhalt nicht ändert.

Wird der Inhalt verändert, entsteht eine neue `ContentRevisionId`.

Beispiel:

```text
Snapshot 100:
Developer ContentRevision = 17

Snapshot 101:
unverändert
Developer ContentRevision = 17

Snapshot 102:
Content geändert
Developer ContentRevision = 18
```

Dadurch lässt sich feststellen, ob sich eine fachliche Quelle wirklich geändert hat, unabhängig davon, wie viele Snapshots inzwischen erstellt wurden.

---

# 25. Content-Abhängigkeiten und Provenienz

Expliziter Rollen-Content kann von anderen Wissensinhalten abgeleitet sein.

Beispiel:

```text
EndUser / Auftragserfassung

based on:

Developer / Auftragserfassung / Revision 18
Consultant / Auftragserfassung / Revision 9
```

Diese Abhängigkeiten werden strukturiert gespeichert.

Konzeptionell:

```text
Target:
  NodeId
  RoleId

Source:
  NodeId
  RoleId
  ContentRevisionId
```

Eine Abhängigkeit kann auch auf einen anderen Node zeigen.

Damit ist das System nicht darauf beschränkt, nur unterschiedliche Rollen desselben Nodes miteinander zu vergleichen.

---

# 26. Independent und Derived Content

Expliziter Rollen-Content kann konzeptionell zwei Bedeutungen haben.

## Independent

Der Inhalt ist für diese Rolle eigenständig maßgeblich und soll nicht automatisch von anderen Rollen als abgeleitet betrachtet werden.

```text
ContentMode = Independent
```

## Derived

Der Inhalt wurde aus anderen Wissensinhalten abgeleitet.

```text
ContentMode = Derived
```

Dann werden die verwendeten Source-Revisions gespeichert.

Beispiel:

```text
EndUser Content
Mode = Derived

Sources:
- Consultant Revision 12
- Developer Revision 31
```

Wenn sich später eine Source-Revision ändert, wird der abgeleitete Inhalt stale.

Ist eine Source selbst `Derived` und stale, ist auch der davon abhängige Content
transitiv stale. Abhängigkeiten dürfen deshalb weder direkte noch indirekte Zyklen
bilden. Ein Dependency-Zyklus ist eine harte Invariante und wird abgelehnt.

---

# 27. Stale-Erkennung

Beispiel:

EndUser wurde erstellt auf Basis von:

```text
Developer Revision 17
```

Später wird Developer geändert:

```text
Developer Revision 18
```

Dann gilt:

```text
EndUser dependency:
expected Developer Revision 17

current Developer Revision:
18

=> EndUser = Stale
```

Der EndUser-Text wird **nicht automatisch verändert**.

Das System signalisiert lediglich:

```text
Dieser Content basiert auf einem älteren Wissensstand.
```

---

# 28. Content-Zustände

Es müssen mindestens zwei Dimensionen unterschieden werden.

## Availability

```text
Explicit
Fallback
None
```

## Freshness

Für expliziten abgeleiteten Content beispielsweise:

```text
Current
Stale
```

Für unabhängigen Content kann die Abhängigkeitsprüfung entfallen.

Damit ist beispielsweise folgende Aussage möglich:

```text
RequestedRole: EndUser
Availability: Explicit
Freshness: Stale
```

oder:

```text
RequestedRole: Developer
Availability: Fallback
ResolvedRole: Consultant
Freshness: Current
```

Fallback und Freshness sind getrennte Konzepte.

---

# 29. Drift wird nicht permanent automatisch repariert

KnowHowTo AI versucht ausdrücklich nicht, bei jeder Änderung alle Rollen sofort zu synchronisieren.

Das wäre für reale Entwicklungsabläufe ungeeignet.

Beispiel:

```text
Implementierung 1
→ EndUser-Doku regenerieren

Implementierung verworfen

Implementierung 2
→ EndUser-Doku erneut regenerieren

erneute Änderung

Implementierung 3
→ EndUser-Doku wieder regenerieren
```

Das erzeugt:

- unnötige LLM-Aufrufe,
- unnötige Änderungen,
- Rauschen,
- schlechte Nachvollziehbarkeit.

Stattdessen wird Drift sichtbar gemacht und später bewusst bearbeitet.

---

# 30. Typischer Arbeitsflow

## Phase 1: Consultant

```text
Consultant
→ fachliches Konzept erstellen
→ Wissen aktualisieren
→ Transaction committen
```

## Phase 2: Entwicklung

```text
Developer
→ Consultant-Wissen lesen
→ implementieren
→ Developer-Wissen ergänzen
→ technische Sackgassen erkennen
→ Inhalte mehrfach verändern
→ mehrere Transactions / Snapshots möglich
```

Währenddessen werden eventuell abhängige Rollen-Inhalte stale.

Sie werden jedoch nicht automatisch geändert.

## Phase 3: Dokumentationssynchronisation

Später explizit:

```text
"Aktualisiere die Endanwender-Dokumentation."
```

Ein Agent erhält dann beispielsweise:

- stale EndUser-Inhalte,
- neue relevante Nodes,
- fehlende Inhalte,
- aktuelle Developer-Inhalte,
- aktuelle Consultant-Inhalte.

Der Agent erzeugt eine neue eigene Transaction und aktualisiert gezielt die EndUser-Dokumentation.

Danach werden neue Dependencies mit den aktuellen Source-Revisions gespeichert.

---

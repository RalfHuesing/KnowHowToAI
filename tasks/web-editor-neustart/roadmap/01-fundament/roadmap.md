# M1 – Arbeits- und Seitenfundament

## Ziel und Abhängigkeiten

Startpunkt: [ready-Konzept](../../Konzept.md). Ergebnis: Die Planungsregeln
stehen nicht im Widerspruch zum neuen Task; alle Web-Schreibaktionen können
einen eindeutigen Entwurf verwenden; `/drafts` und `/drafts/{TransactionId}`
sind benutzbar; die gemeinsame Shell trägt Wissen und Entwürfe. Alte Web-Seiten
dürfen bis M3 noch technisch erreichbar sein, sind aber keine Zielnavigation.

## Reihenfolge

- [x] [M1.1-T1 – Regeln und Doku-Grenze](tasks/M1.1-T1.md)
- [x] [M1.2-T1 – Entwurfs- und URL-Arbeitszustand](tasks/M1.2-T1.md)
- [x] [M1.3-T1 – Entwurfsübersicht und Abschluss](tasks/M1.3-T1.md)
- [ ] [M1.4-T1 – Gemeinsame Shell und Navigation](tasks/M1.4-T1.md)

## Milestone-Abnahme

- [ ] Ein Direktaufruf der beiden Entwurfsrouten zeigt einen eindeutig
      ausgewählten Entwurf und erlaubt nur dessen Commit oder Discard.
- [ ] Wissen und Entwürfe besitzen dieselben Innenkanten und genau ein `main`;
      die globale Navigation ist kompakt und enthält keine technischen
      Kontextselektoren als Bedingung für einen Edit.
- [ ] Begrenzter M1-Audit gegen Konzept, Regeln, Diff und Nachweise
      dokumentiert; keine neue Produktentscheidung im Audit.

Audit-Ergebnis: offen.

# Invarianten

Die folgenden Regeln sind verbindlich. Sie sind nicht abschaltbar und nicht
Konfigurationsgegenstand (Trennung: [Konfiguration und
Betrieb](Konfiguration-und-Betrieb.md)). Fehler- und Warncodes, mit denen
Verletzungen gemeldet werden, stehen im [Katalog der MCP-API](McpApi.md).

1. Jeder Write am versionierten Wissenszustand benötigt eine `TransactionId`; die
   Registrierung eines Releases ist reine Metadatenverwaltung und die einzige
   Ausnahme.
2. Eine KnowHowTo-AI-Transaction ist keine langfristig offene SQL-Transaction; sie
   besteht aus kurzen, atomaren SQL-Operationen.
3. Beim Öffnen einer Transaction wird der vollständige aktuelle Wissensstand
   kopiert (Working Snapshot).
4. Committed Snapshots sind unveränderlich.
5. Historische Daten werden nicht physisch gelöscht.
6. Jeder Node besitzt eine stabile logische `NodeId`, die über Snapshots und
   Änderungen hinweg identisch bleibt.
7. Pro Snapshot existiert höchstens ein aktiver persistierter Root-Node; der
   initiale leere Snapshot besitzt keinen.
8. Die Hierarchie ist global für alle Zielgruppen.
9. Node-Titel werden nicht als Überschrift oder alleinstehender Ersatztitel in
   `ContentMd` gespeichert; normale Erwähnungen im Fließtext sind erlaubt.
10. Persistierter Content darf keine Markdown- oder HTML-Überschriften enthalten.
11. Überschriften werden ausschließlich aus der Node-Hierarchie erzeugt
    (Export, Darstellung).
12. Heading-Validierung erfolgt mit einem echten Markdown-Parser, nie per
    String-Matching auf `#`.
13. Zielgruppen sind frei definierbar und werden nach dem initialen Seed transaktional
    über die Service-/MCP-Grenzen gepflegt; direkte SQL-Änderungen an committed
    Snapshots sind unzulässig.
14. Audience Resolution Orders sind frei konfigurierbar, deterministisch und nicht
    rekursiv; eine Zielgruppe kommt innerhalb einer Order nicht mehrfach vor.
15. Fehlender Zielgruppen-Content darf per Fallback aufgelöst werden.
16. `requestedAudience` und `resolvedAudience` werden immer transparent zurückgegeben.
17. Identischer Content wird nicht unnötig pro Zielgruppe dupliziert.
18. Eigener Zielgruppen-Content ist `Independent` oder `Derived`.
19. Derived Content speichert explizite Source-Revisions.
20. Änderungen an Source-Revisions oder stale Derived Sources markieren abhängigen
    Content transitiv als stale.
21. Content Dependencies dürfen weder direkte noch indirekte Zyklen bilden.
22. Stale Content wird nicht automatisch verändert oder überschrieben.
23. Dokumentationssynchronisation ist ein eigener, bewusster Workflow.
24. Nodes sollen klein gehalten werden; die Standardwarnschwelle beträgt 4 KiB
    normalisierten UTF-8-Contents und ist konfigurierbar.
25. Zu große Nodes erzeugen eine Qualitätswarnung statt automatischen
    Wissensverlust; Speichern und Commit bleiben erlaubt.
26. Refactoring von Wissen erfolgt bewusst und transaktional mit den normalen
    Mutations-Primitiven.
27. Content-Reads sind metadata-first und token-effizient.
28. Reads laufen gegen den Current Snapshot, einen historischen Snapshot oder die
    offene Working Transaction; `transactionId` und `snapshotId` zugleich sind
    unzulässig.
29. Konkurrierende Commits dürfen keine Änderungen überschreiben
    (`SnapshotConflict`).
30. Es gibt kein automatisches Merge oder Rebase.
31. Node-Löschung (global, alle Zielgruppen) und Zielgruppen-Content-Löschung (nur der
    explizite Content einer Zielgruppe) sind unterschiedliche Operationen.
32. Streamable HTTP unter `/mcp` ist der einzige MCP-Transport; die
    Geschäftslogik ist nicht an einen Transport gebunden.

Zusätzlich verbindliche Konfigurations- und Migrationsregeln:

- Die Datenbank enthält ausschließlich Wissens-, Versions-, Transaktions- und
  Release-Daten. Betriebs- und Quality-Policies sind Anwendungskonfiguration und
  werden nicht in fachlichen Tabellen gespeichert.
- Angewendete Migrationsskripte sind unveränderlich; Änderungen erfolgen
  ausschließlich durch neue, vorwärts gerichtete Migrationen mit neuer
  Versionsnummer.
- Der SQL-Test-Harness erzeugt oder entfernt keine Datenbanken; die Testdatenbank
  wird manuell bereitgestellt.

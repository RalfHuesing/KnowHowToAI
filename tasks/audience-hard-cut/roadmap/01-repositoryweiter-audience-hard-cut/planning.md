# M1.0 – Planungs- und Begriffsvertrag

Stand: 2026-09-21

Status: abgeschlossen; M1.1–M1.5 sind zur strikt sequenziellen Ausführung
freigegeben. Die Umsetzung wird vom Benutzer manuell gestartet.

## Ausgangslage

Das vorhandene Fachkonzept beschreibt die Zielgruppe eines Inhalts, verwendet
technisch jedoch den Begriff `Role` und in der deutschen Oberfläche „Rolle“.
Das kollidiert sprachlich mit späterer Authentifizierung und Autorisierung,
obwohl die bestehende Wissenszielgruppe ausdrücklich keine Berechtigungsrolle
ist.

Eine Bestandsaufnahme am 2026-09-21 ergab für die einfache, bewusst breite
Suche `rg -il "role"` 400 Dateien: 193 unter `src/`, 136 unter `tests/`, 57
unter `tasks/`, neun unter `docs/`, vier unter `sql-scripts/` und eine unter
`.agents/`. Darin sind neben der Fachterminologie auch standardisierte
Accessibility-Begriffe enthalten. Betroffen sind Domain und Application,
SQL-Schema und Repositories, MCP-Verträge, Webzustand und UI, alle Testebenen,
Ist-Dokumentation, Regeln sowie aktive und historische Planungsartefakte.

## Geschlossene Produktentscheidung

Option A ist verbindlich:

- Technischer Standardbegriff in Code, SQL, MCP, URL, Persistenzschlüsseln,
  Tests und technischer Dokumentation ist `Audience`.
- Die sichtbare deutsche Benutzeroberfläche verwendet ausschließlich
  „Zielgruppe“ beziehungsweise „Zielgruppen“.
- Das Modell bleibt eine inhaltliche Adressatenschaft. Authentifizierung,
  Autorisierung, RBAC und ACL bleiben außerhalb dieses Vorhabens.
- Es gibt keinen Parallelbetrieb alter und neuer Namen, keine Alias-Typen,
  Weiterleitungsrouten, doppelten JSON-Felder, alternativen MCP-Tools,
  Compatibility Views oder Fallback-Leser für alte Browserzustände.

## Verbindlicher Begriffsvertrag

| Bisherige Verantwortung | Zielname |
|---|---|
| fachliche ID und Entität | `AudienceId`, `Audience` |
| Auflösungsreihenfolge | `AudienceResolution`, `AudienceResolutionOrder` |
| angefragte/aufgelöste Zielgruppe | `RequestedAudienceId`, `ResolvedAudienceId` beziehungsweise `requestedAudience`, `resolvedAudience` |
| Kandidat einer Auflösung | `CandidateAudienceId` beziehungsweise `candidateAudienceId` |
| Content- und Dependency-Bezug | `AudienceId`, `TargetAudienceId`, `SourceAudienceId` |
| Domain-/Application-Namespace | `Domain.Audiences`, `Application.Mutations.Audiences` |
| Persistence-Ports | `IAudienceRepository`, `IAudienceMutationRepository` |
| SQL-Tabellen | `KnowHowToAI_Audience`, `KnowHowToAI_AudienceResolution` |
| SQL-Spalten, Constraints und Indizes | durchgängig `Audience` statt des bisherigen Fachpräfixes |
| MCP-Read | `list_audiences` |
| MCP-Writes | `create_audience`, `update_audience`, `delete_audience`, `set_audience_resolution` |
| MCP-/JSON-Felder | `audienceId`, `requestedAudienceId`, `resolvedAudienceId`, `candidateAudienceIds` und `sourceAudienceId` |
| stabile Fehlercodes | `AudienceNotFound`, `AudienceInUse`, `AudienceNameRequired`, `AudienceIdRequired`, `InvalidAudienceResolution`, `AudienceResolutionNotConfigured`, `RequestedAudienceNotFound`, `RequestedAudienceDeleted`, `CandidateAudienceNotFound`, `CandidateAudienceDeleted`, `DuplicateCandidateAudience` |
| Webroute | `/audiences` |
| rekonstruierbarer Queryparameter | `audienceId` |
| Browserzustand | `knowhowtoai.lastAudienceId` |
| Webfeature und Komponenten | `Features/Audiences`, `AudiencesPage`, `AudienceEditor`, `AudienceMapper` und entsprechend fachlich benannte ViewModels |
| sichtbare deutsche Texte | „Zielgruppe“, „Zielgruppen“, „Zielgruppen-Auflösung“ |

Weitere zusammengesetzte Symbole werden mechanisch nach derselben Regel
gebildet. `Default` bleibt die initiale fachliche ID und der initiale Name; das
Vorhaben lokalisiert keine gespeicherten Nutzdaten.

## Öffentliche Hard-Cut-Verträge

- Alte MCP-Toolnamen und alte JSON-Feldnamen werden im selben MCP-Slice entfernt.
  Ein Aufruf mit dem alten Vertrag ist danach unbekannt beziehungsweise
  schematisch ungültig; es gibt keinen speziellen Übersetzungsfehler.
- `/audiences` und `audienceId` ersetzen Route und Queryvertrag im selben
  Web-Slice. Alte URLs werden nicht weitergeleitet.
- Der alte `localStorage`-Schlüssel wird weder gelesen noch migriert. Nach dem
  Deployment wählt der Benutzer die Zielgruppe einmal erneut aus.
- Alte Paging-Cursor sind flüchtige Clientartefakte und werden nicht übersetzt.
  Ein nicht mehr zum neuen Vertrag passender Cursor wird über den bestehenden
  stabilen Cursorfehler abgelehnt.
- Fehler- und Warnmeldungen verwenden in technischen Details `Audience` und in
  sichtbarem deutschen Text „Zielgruppe“. Fachliche Fehlersemantik und
  HTTP-/MCP-Status bleiben unverändert.

## SQL- und Datenbankentscheidung

Es gibt noch keinen produktiven oder anderweitig zu erhaltenden Datenbankstand.
Der Benutzer hat ausdrücklich entschieden, dass alle vorhandenen
KnowHowToAI-Entwicklungs- und Testdatenbanken einschließlich ihrer Tabellen und
Daten vollständig zurückgesetzt werden dürfen. Deshalb wird die
Greenfield-Baseline direkt korrigiert:

- `0002_create_roles.sql` wird nach `0002_create_audiences.sql` umbenannt und
  definiert ausschließlich Audience-Tabellen, -Spalten, -Constraints und
  -Indizes.
- `0003_create_nodes_and_content.sql` verwendet ausschließlich Audience-Spalten
  und -Fremdschlüssel.
- `0004_seed_initial_state.sql` erzeugt die initiale Audience `Default` und ihre
  Resolution Order.
- Es entsteht kein `0005`, keine Datenmigration und keine Kompatibilitätsbrücke.

Der zuständige Umsetzungsslice ermittelt vor jeder destruktiven Aktion die drei
konfigurierten Zielverbindungen `DatabaseConnection`,
`BrowserTestDatabaseConnection` und `BrowserVisualTestDatabaseConnection`,
protokolliert ausschließlich Server und Datenbankname ohne Credentials und
verifiziert, dass jede Zieldatenbank eine ausdrücklich konfigurierte
KnowHowToAI-Entwicklungs-/Testdatenbank ist. Danach entfernt er nur Objekte mit
dem Präfix `KnowHowToAI_` in diesen exakt aufgelösten Datenbanken, baut das Schema
über den normalen Migration Runner neu auf und prüft den Seed. Die Datenbanken
selbst werden nicht angelegt oder gelöscht. Unklare, nicht erreichbare oder
abweichend benannte Ziele führen zum Stopp statt zu einer geratenen Löschung.
Mehrfach auf dieselbe Server-/Datenbankkombination zeigende Sektionen werden vor
dem Reset dedupliziert; SQL-Server-Systemdatenbanken sind immer ausgeschlossen.

## Bedeutung des Terminologie-Gates

Das ursprüngliche wörtliche Nulltrefferziel wird fachlich präzisiert. Ein
ungefiltertes `rg -i "role"` kann und darf nicht null liefern, weil
barrierefreies HTML und Playwright standardisierte WAI-ARIA-Begriffe wie
`role="status"`, `GetByRole` und `AriaRole` benötigen. Das
Entscheidungsprotokoll muss den ersetzten Begriff ebenfalls nachvollziehbar
festhalten.

Für den Abschluss gilt deshalb:

- In laufendem Produktcode, öffentlichen Verträgen, fachlichen Testdaten,
  sichtbaren Texten, aktueller Ist-Dokumentation und ausführbaren fremden
  Roadmaps existiert keine fachliche Altterminologie mehr.
- Erlaubt bleiben ausschließlich standardisierte Accessibility-Attribute und
  -APIs sowie dieses abgeschlossene Entscheidungs- und Roadmap-Artefakt.
- Jeder verbleibende Treffer der breiten Suchen `rg -in "role"` und
  `rg -in "rolle"` wird im Abschlussnachweis einzeln einer dieser Kategorien
  zugeordnet. Eine generische Pfad- oder Dateiausnahme ist unzulässig.
- Insbesondere sind Kommentare, Testnamen, Test-IDs, CSS-Klassen, URLs,
  Namespaces, Dateinamen, Verzeichnisnamen, DTOs, JSON-Felder, Fehlerschlüssel
  und Benutzertexte keine zulässigen Ausnahmen.

## Slice- und Commitstrategie

Die Reihenfolge ist verbindlich. Jeder Slice bleibt buildbar und aktualisiert
die von ihm tatsächlich geänderten Ist-Verträge im selben Commit. Vorübergehend
noch nicht bearbeitete Adapter dürfen ihre alten öffentlichen Namen behalten,
erhalten aber keine Alias-Schicht zum neuen Kern. Compilerbedingte Anpassungen
außerhalb des benannten Hauptbereichs sind nur direkte Aufruferkorrekturen;
deren öffentliche Umbenennung bleibt im zuständigen Folgeslice.

1. Core/Application und danach SQL/Persistence.
2. MCP-Grenze.
3. Webgrenze, danach reale Browserabläufe.
4. Regeln und sämtliche übrigen Roadmap-/Konzeptartefakte.
5. Repositoryweiter Restabgleich und vollständige Qualitätsgates.

Es arbeitet genau ein schreibender Agent. Builds, Linter und Tests laufen
seriell. Andere Webfrontend- und UI/UX-Implementierungen pausieren, bis die
Planungsreferenzen in M1.4 auf die Zielterminologie umgestellt sind.

## Nicht-Ziele

- keine Änderung von Fallback, Availability, Freshness, Provenienz oder
  Resolution-Semantik;
- keine Änderung gespeicherter Audience-IDs oder fachlicher Namen wie
  `Default`, `Developer`, `Consultant` und `EndUser`;
- keine Benutzer-, Authentifizierungs-, Autorisierungs-, RBAC- oder ACL-Funktion;
- keine REST-API, neue Route neben `/audiences` oder neue MCP-Funktion;
- keine Datenbereinigung, kein Neu-Seed und kein Verlust historischer Snapshots;
- keine allgemeine UI/UX-Überarbeitung und keine sachfremde Refaktorierung.

## Freigabekriterien

- [x] Option A und alle öffentlichen Zielnamen sind festgelegt.
- [x] Hard-Cut-, Greenfield-SQL-, Datenbankreset-, Browserzustands- und
      Cursorverhalten sind entschieden.
- [x] Das Nulltrefferziel ist ohne Accessibilitybruch präzisiert.
- [x] Leaf-Tasks sind sequenziell, vollständig und ohne offene Produkt- oder
      Architekturentscheidung geschnitten.
- [x] `docs/` bleibt in diesem Planungsschritt unverändert und beschreibt weiter
      ausschließlich den Ist-Zustand.

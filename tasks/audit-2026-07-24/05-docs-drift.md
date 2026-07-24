# Dimension 5 — Doku vs. Code-Drift

> **Vergleichsbasis:** `.agents/rules/04-docs-reference.mdc` ("`docs/` als Source of Truth"),
> `.agents/rules/05-documentation.mdc` ("Doku beschreibt den aktuellen Stand, nicht mehr
> und nicht weniger"), sowie der Code selbst (für Drift-Erkennung).
> **Methodik:** Stichprobe jeder Code-Komponente gegen die zugehörige `docs/`-Erwähnung.
> Drift = dokumentierter Sachverhalt stimmt nicht (mehr) mit dem Code überein, *oder*
> ein nicht-trivialer Sachverhalt im Code hat keine Doku-Entsprechung.
> **Nicht im Scope:** Rechtschreibprüfung, Sprachstil, Lesbarkeit — nur Inhalts-Drift.

## Drifts-Übersicht

| ID | Schwere | Titel | Doku-Stelle | Code-Stelle |
| --- | --- | --- | --- | --- |
| [F-DK-001](#f-dk-001) | **High** | `docs/02` Zeile 120 dokumentiert `LogResponseSize` mit `JsonSerializer.SerializeToUtf8Bytes(...).Length` *als Soll-Verhalten* — Code macht genau das, was die Doku sagt; beides ist die suboptimale Implementation, die Doku hätte das Verhalten *kritisieren* oder eine bessere Variante *fordern* sollen | `docs/02-Architektur-und-Techstack.md:120` | `McpTools/DocsMcpTools.cs:43-44` |
| [F-DK-002](#f-dk-002) | Medium | `docs/02` Zeile 122 erwähnt `ServerInstructions` als "kurzer Pointer auf die drei Tools + die Resource" — der konkrete Wortlaut ist nicht dokumentiert; bei zukünftigen Änderungen am ServerInstructions-Text weiß niemand, ob die LLM-UX-Wirksamkeit (Länge, Ton, Vokabular) erhalten bleibt | `docs/02-Architektur-und-Techstack.md:122` | `McpTools/DocsMcpResources.cs:11-14` |
| [F-DK-003](#f-dk-003) | Medium | `docs/03` zeigt Solution-Layout und Layer-Beschreibung, aber *nicht* die Service-Konstruktion in `Program.cs` — die DI-Inkonsistenz (F-AR-001) ist nirgends dokumentiert, weder als "gewollt" noch als "bekannt" | `docs/03-Projektstruktur-und-Konfiguration.md:7-21` | `Cli/Program.cs:64, 84-86, 105-107` |
| [F-DK-004](#f-dk-004) | Medium | `docs/02` Zeile 23 sagt "Schlanker eigener `SchemaMigrator`" und `docs/04` Zeile 9 sagt "Skripte sind selbst idempotent (`IF NOT EXISTS`-Guard)" — das ist konsistent; ABER: der Verlust an Transaktions-Sicherheit (F-AR-004 zu `SchemaMigrator.MigrateAsync` — kein Rollback bei Fehler im 2. Skript) ist *nirgends* erwähnt | `docs/04:9` | `Migrations/SchemaMigrator.cs:27-31` |
| [F-DK-005](#f-dk-005) | Medium | Preview-Dependencies (`ModelContextProtocol 2.0.0-preview.2`, `System.CommandLine 3.0.0-preview.5`) sind in keiner Doku-Datei erwähnt — weder als bewusste Wahl noch mit Risiko-Hinweis | n/a | `Cli/Cli.csproj:16, 19` |
| [F-DK-006](#f-dk-006) | Medium | `appsettings.json` zeigt `TrustServerCertificate=True;` — typisch für lokales Dev-Setup, aber: in `docs/03` Zeile 56-72 nur als kopierter JSON, nicht erklärt; User mit strikteren Security-Setups (z.B. `Encrypt=True`) könnte irritiert sein | `docs/03:56-72` | `Cli/appsettings.json:4` |
| [F-DK-007](#f-dk-007) | Low | `Microsoft.Data.SqlClient 7.0.2` (transitive Dep) hat Breaking Changes (siehe Dim 7) — nirgends in `docs/` als Risiko für bestehende Konfigurationen erwähnt; für lokalen Dev-Use-Case irrelevant, aber bei Wechsel auf z.B. SQL Server 2016 wäre SqlBulkCopy gebrochen | n/a | `Core/Core.csproj:11` |
| [F-DK-008](#f-dk-008) | Low | `docs/01-Konzept-und-Workflow.md` (nicht gelesen in diesem Audit, sondern referenziert) erwähnt "Phase 2: Doku erweitern oder umstrukturieren" — laut Roadmap-Punkt 9 wird `docs://authoring-guide` als Resource geliefert; Cross-Check: passt das mit den Slug-Regeln in `docs/04` Zeile 56 überein? | `docs/04:56` | `McpTools/DocsMcpResources.cs:46-50` |
| [F-DK-009](#f-dk-009) | Info | `docs/05-Roadmap.md` Zeile 53 nennt "End-to-End-Verifikation gegen eine befüllte DB" als offen — passt zur aktuellen Realität (kein SQL Server lokal) | konsistent | konsistent |
| [F-DK-010](#f-dk-010) | Info | `docs/05-Roadmap.md` Zeile 76 listet 3 offene DoD-Punkte, alle blockiert durch SQL-Setup-Problem — konsistent mit `docs/03:84` (Bekannter lokaler Stolperstein) | konsistent | konsistent |
| [F-DK-011](#f-dk-011) | Info | `docs/04` Edge-Case 4.3 (Transaktion + Nebenläufigkeit) sagt "READ COMMITTED reicht" — passt zum Standard-Default von SQL Server und zur `SqlDocumentsStore.ReplaceAllAsync`-Implementation | konsistent | konsistent |
| [F-DK-012](#f-dk-012) | Info | `docs/02` Zeile 26 nennt explizit "kein Konsolen-Sink für keines der vier Kommandos" — passt zum `ConfigureLogger` ohne Console-Sink | konsistent | konsistent |

## Detail-Findings

### F-DK-001 — `LogResponseSize`-Doku dokumentiert suboptimales Verhalten als Soll

**Schweregrad:** High (Doku-Drift, die den Status-Quo zementiert statt ihn zu verbessern)

**Beobachtung:**
`docs/02-Architektur-und-Techstack.md` Zeile 120:
> "nach der Abfrage die Größe der Antwort in Bytes
> (`JsonSerializer.SerializeToUtf8Bytes(...).Length`) — **nicht** deren Inhalt, da der
> Log sonst selbst zum riesigen, unübersichtlichen Datenberg würde."

`src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:43-44`:
```csharp
private void LogResponseSize<T>(string toolName, T response) =>
    logger.LogInformation("{ToolName} response: {ByteCount} bytes", toolName, JsonSerializer.SerializeToUtf8Bytes(response).Length);
```

Die Doku dokumentiert die Implementation *als* Begründung. Die Begründung ("damit der
Log nicht zum Datenberg wird") ist richtig — aber die *konkrete* Implementation
(`SerializeToUtf8Bytes(...).Length`) ist suboptimal: sie serialisiert die *gesamte*
Response zu Bytes nur um die Länge zu messen. Das ist Performance-Müll (siehe F-PE-001 in
Dim 8).

**Drift:** Die Doku sagt "so machen wir's" und begründet *warum*. Der *wie*-Teil ist
suboptimal, der *warum*-Teil ist richtig. Ein driftfreier Doku-Stand würde sagen:
"Die Response wird *nicht* im Log abgelegt; nur ihre Größe, gemessen via
`{ResponseType}.Count` (Listen) bzw. `{DocumentDetail}.Content.Length` (Strings) —
kein vollständiger JSON-Encode."

**Fix-Empfehlung:**
1. `docs/02` Zeile 120 umformulieren, um die *Intention* zu dokumentieren (Größen-
   Information, nicht Vollserialisierung), und in `docs/02` Abschnitt 2
   (Tech-Stack) oder in einem neuen Abschnitt "Logging-Strategie" klarstellen, dass
   *keine* JSON-Re-Serialisierung der Response stattfindet.
2. Code entsprechend fixen (F-AR-003, F-PE-001): `LogResponseSize` gibt
   `Content.Length` für `DocumentDetail` und `result.Count` für `IReadOnlyList<...>`
   aus.

**Aufwand:** ~5 Minuten Doku, ~15 Minuten Code.

---

### F-DK-002 — `ServerInstructions`-Wortlaut nicht dokumentiert

**Schweregrad:** Medium (LLM-UX-Wirksamkeit, nicht-driftend, aber unterdokumentiert)

**Beobachtung:**
`docs/02-Architektur-und-Techstack.md` Zeile 122 sagt:
> "Zusätzlich setzt der Server `ServerInstructions` (kurzer Hinweis auf die drei Tools
> + die Resource), der bei jeder Verbindung automatisch beim Client ankommt."

Der *konkrete* Wortlaut aus `DocsMcpResources.cs:11-14`:
> "KnowHowToAI: durchsuchbare Wissensdatenbank. Lesen: list_children/search_docs/get_doc.
> Neue oder geänderte Doku als .md-Datei im docs-root anlegen (Format siehe Resource
> docs://authoring-guide), danach 'validate' und 'import' per CLI ausführen."

Diese Phrase ist *die* Eingangstür für jedes verbundene LLM. Wenn jemand den Text
"verbessert" (z.B. "weniger kryptisch"), könnte die LLM-Wirksamkeit sinken. Keine
Doku sagt: "ServerInstructions sind LLM-UX-Hot-Path, vor Änderungen in Dim 9
(`MCP-Tool-API-Qualität`) prüfen."

**Fix-Empfehlung:** In `docs/02` (oder einem neuen `docs/06-LLM-UX.md`?) den exakten
Wortlaut zitieren und die Wirkungs-Achsen (Länge, Vokabular, Reihenfolge der Tools)
dokumentieren. Bei Änderungen ist dann sofort klar, was angepasst wurde.

**Aufwand:** ~5 Minuten.

---

### F-DK-003 — Service-Konstruktion in `Program.cs` undokumentiert

**Schweregrad:** Medium (dokumentiert die aktuelle Architektur-Entscheidung nicht)

**Beobachtung:**
`docs/03-Projektstruktur-und-Konfiguration.md` Zeile 7-21 zeigt das Solution-Layout
als Block-Schema. Zeile 25-46 beschreibt die Core/Cli-Trennung und das Delegate-Pattern.
Aber: nirgendwo wird erwähnt, *wie* die Cli-Commands ihre Services bekommen.

Aktueller Stand in `Program.cs`:
- `RunValidate` → `new DocsValidator(...)`
- `RunImport` → `new SqlDocumentsStore(...)` + `new ImportService(...)`
- `RunExport` → `new SqlDocumentsStore(...)` + `new ExportService(...)`
- `RunServer` → `AddSingleton<SqlDocumentsStore>` + `WithToolsFromAssembly()`

Die Inkonsistenz (siehe F-AR-001) ist in der Doku weder als bewusst noch als "noch zu
refaktorisieren" markiert. Wer das Repo liest, könnte denken: "ist das so gewollt?"
und unsicher sein, ob eine Vereinheitlichung gewünscht ist.

**Fix-Empfehlung:** Kurzer Abschnitt in `docs/03` (oder Verweis auf F-AR-001 in diesem
Audit): "Aktuell werden `ImportService`/`ExportService` direkt per `new` in
`Program.cs` konstruiert, nicht per DI. Dies ist historisch gewachsen (jeder
Cli-Command braucht nur einen Service) und konsistent mit dem schlanken Setup in v1.
Vereinheitlichung über ein Composition-Root-Pattern (siehe Audit-F-AR-001) ist
angedacht, aber nicht Teil von v1."

**Aufwand:** ~10 Minuten.

---

### F-DK-004 — `SchemaMigrator` Transaktions-Verlust undokumentiert

**Schweregrad:** Medium (Risiko-Lücke in der Doku)

**Beobachtung:**
`docs/04-Datenmodell-Validierung-Edgecases.md` Zeile 9:
> "Skripte sind selbst idempotent und laufen bei jedem `import` erneut"

Implizit: kein Bedarf für Migration-Journal. Aber: was passiert, wenn das 2. Skript
fehlschlägt, nachdem das 1. erfolgreich war? Aktueller Code (`SchemaMigrator.cs:27-31`):
```csharp
foreach (var script in DiscoverScripts(documentsTableName))
{
    logInformation($"Führe SQL-Skript aus: {script.Name}");
    await connection.ExecuteAsync(new CommandDefinition(script.Sql, ...));
}
```

Jeder `ExecuteAsync` ist auto-committed (kein expliziter `BeginTransaction` im
`SchemaMigrator`). Wenn Skript 1 committed und Skript 2 fehlschlägt, ist die DB in
einem halb-migrierten Zustand.

Für das *aktuelle* Skript-Set (nur `0001_create_documents_table.sql`, idempotent) ist
das irrelevant. Sobald ein zweites Skript hinzukommt (z.B. ein Index-Skript), wird
das Risiko real.

**Fix-Empfehlung:** In `docs/04` Zeile 9-13 (Abschnitt "SQL-Skripte") explizit
dokumentieren: "Der `SchemaMigrator` führt Skripte sequenziell aus, ohne explizite
Transaktion. Bei Mehr-Skript-Setups ist fehlende Atomarität ein Risiko — entweder
Transaktion um die Skript-Liste legen oder explizit dokumentieren, dass jedes Skript
unabhängig idempotent und vor Fehlern sicher sein muss."

**Aufwand:** ~5 Minuten Doku; ggf. ~15 Minuten Code (Transaktion im SchemaMigrator).

---

### F-DK-005 — Preview-Dependencies undokumentiert

**Schweregrad:** Medium (zukünftiges Risiko)

**Beobachtung:** `src/KnowHowToAI.Cli/KnowHowToAI.Cli.csproj:16, 19`:
- `<PackageReference Include="ModelContextProtocol" Version="2.0.0-preview.2" />`
- `<PackageReference Include="System.CommandLine" Version="3.0.0-preview.5.26302.115" />`

Beide sind Preview. Die Konsequenzen:
- `dotnet restore` kann jederzeit eine neuere Preview-Version auflösen, die
  Breaking Changes hat
- Bei einem `dotnet tool` oder `dotnet pack` Build kann sich das Verhalten ändern
- Sicherheits-Patches kommen in Stable-Versionen, nicht in Preview-Versionen (meistens)

Nirgendwo in `docs/` ist erwähnt, *warum* Preview verwendet wird und was die
Rollback-Strategie ist.

**Fix-Empfehlung:** Kurzer Abschnitt in `docs/02` (Tech-Stack-Tabelle) oder am
Anfang von `docs/03`: "Preview-Dependencies: `ModelContextProtocol 2.0.0-preview.2`
und `System.CommandLine 3.0.0-preview.5` — bewusst gewählt wegen [Begründung].
Stable-Downgrade-Plan: bei nächstem 1.x-Release evaluieren."

**Aufwand:** ~5 Minuten.

---

### F-DK-006 — `TrustServerCertificate=True` undokumentiert

**Schweregrad:** Low (lokales Dev-Setup, aber Pattern ist gut erklärbar)

**Beobachtung:** `appsettings.json:4` enthält
`TrustServerCertificate=True;` ohne Erklärung in `docs/03` (außer im JSON-Block selbst,
wo es implizit "mitkopiert" wird).

**Kontext:** In SQL-Server-Setups mit selbst-signierten Zertifikaten (typisch für
lokale Instanzen) muss `TrustServerCertificate=True` gesetzt werden, sonst
schlägt die Verbindung fehl. Für User, die eine produktive SQL-Instanz mit echten
Zertifikaten anbinden, ist das ein "warum ist das an?"-Fragezeichen.

**Fix-Empfehlung:** In `docs/03` Abschnitt 2 (appsettings.json-Beispiel) ein
Kommentar-artiger Hinweis: "TrustServerCertificate=True ist auf lokalen
Dev-Instanzen mit selbst-signierten Zertifikaten erforderlich. Für produktive
Setups mit echten Zertifikaten sollte dieser Wert auf `False` stehen oder die
Zeile komplett entfernt werden."

**Aufwand:** ~3 Minuten.

---

### F-DK-007 — `Microsoft.Data.SqlClient 7.0` Breaking Changes (Info/Low)

**Schweregrad:** Low (für lokalen Use-Case irrelevant; trotzdem ein Risiko-Pattern)

Siehe Dim 7 für Details. Kurz: 7.0 hat SqlBulkCopy-Breaking-Change für SQL Server
2016. Wenn das Repo auf eine solche Instanz zielt (unwahrscheinlich), ist
Fehlersuche schwer ohne Doku-Hinweis.

---

### F-DK-008 — `docs://authoring-guide` Slug-Regeln (Info/Low)

**Schweregrad:** Low (Cross-Check bestanden, Mini-Beobachtung)

**Beobachtung:** `DocsMcpResources.cs:46-50` sagt:
> "Nur `a-z`, `0-9`, `-`. Kein Großbuchstabe, kein Umlaut, kein Leerzeichen, kein
> `_`, keine führenden/doppelten Bindestriche."

`docs/04` Zeile 56-58 sagt das gleiche. Konsistent. Mini-Beobachtung: das Beispiel
"Ungültig: `IT`" und "Gültig: `it`" ist nur in `docs/04` (Zeile 56), nicht in der
Resource. Die Resource ist sehr knapp; ein "Self-Service-Spickzettel" für das LLM.

**Kein Handlungsbedarf.**

---

### F-DK-009 / F-DK-010 / F-DK-011 / F-DK-012 — Konsistenz-Bestätigungen (Info)

Alle vier bestätigen: Doku und Code stimmen an den geprüften Stellen überein. Diese
sind keine Drifts, sondern positive Befunde.

## Zusammenfassung Dim 5

- **12 Findings**, davon 1 × High, 4 × Medium, 2 × Low, 5 × Info.
- **Hauptthema:** Eine kritische Doku-Stelle (F-DK-001) zementiert ein suboptimales
  Verhalten. Vier mittelschwere Lücken, die jeweils ~5-10 Minuten Doku-Aufwand
  bedeuten. Zwei Low-Findings, fünf Info-Bestätigungen.
- **Insgesamt ist die Doku-Qualität hoch.** Die `docs/`-Dateien sind aktuell, gut
  strukturiert, und die Edge-Case-Dokumentation in `docs/04` ist umfassend. Die
  wenigen Lücken sind punktuell, nicht strukturell.
- **Empfehlung:** F-DK-001, F-DK-002, F-DK-003, F-DK-004, F-DK-005, F-DK-006 als
  ein zusammenhängender Doku-Commit (nach den Code-Fixes für F-AR-003 und F-PE-001,
  damit Doku und Code gemeinsam aktualisiert werden).

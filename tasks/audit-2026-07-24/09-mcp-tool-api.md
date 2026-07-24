# Dimension 9 — MCP-Tool-API-Qualität

> **Vergleichsbasis:** MCP-Spezifikation (Tool-Discovery-Schema, Tool-Description-Konventionen),
> LLM-UX-Best-Practices (klare Args-Beschreibung, Beispiel-Outputs, Fehler-Semantik),
> `.agents/rules/05-documentation.mdc` (kurz und mit Mehrwert).
> **Methodik:** Statische Analyse der Tool-Definitionen, der zurückgegebenen Typen, der
> `ServerInstructions` und der `authoring-guide`-Resource. Bewertung aus LLM-Konsumenten-
> Perspektive.
> **Nicht im Scope:** Tatsächliches LLM-Ausprobieren (kein LLM im Audit-Setup), JSON-Schema-
> Generierungs-Validität (das macht das MCP-SDK).

## Tool-Inventar

| Tool | Args | Return-Type | Description-Quelle |
| --- | --- | --- | --- |
| `list_children` | `parentSlug: string?` | `IReadOnlyList<DocumentSummary>` | `DocsMcpTools.cs:16` |
| `search_docs` | `query: string` | `IReadOnlyList<DocumentSummary>` | `DocsMcpTools.cs:25` |
| `get_doc` | `slug: string` | `DocumentDetail?` | `DocsMcpTools.cs:34` |
| `docs://authoring-guide` (Resource) | n/a | `text/markdown` | `DocsMcpResources.cs:16-17` |

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-MC-001](#f-mc-001) | **High** | Tool-Description-Qualität: `list_children`/`search_docs`/`get_doc` beschreiben das *Was*, aber nicht das *Wann-nicht* (Edge-Cases) oder die Fehler-Semantik — LLM trifft falsche Entscheidungen | `McpTools/DocsMcpTools.cs:16, 25, 34` |
| [F-MC-002](#f-mc-002) | Medium | Keine Tool-Beispiele in den Descriptions — LLM-UX-Best-Practice ist, ein konkretes Beispiel-Argument + Beispiel-Output in der Description zu zeigen | `McpTools/DocsMcpTools.cs:16, 25, 34` |
| [F-MC-003](#f-mc-003) | Medium | `ServerInstructions` ist `static const string`, nicht zentral dokumentiert — bei Änderungen fehlt die LLM-UX-Wirksamkeits-Validierung | `McpTools/DocsMcpResources.cs:11-14` |
| [F-MC-004](#f-mc-004) | Medium | `get_doc` mit `DocumentDetail?` (nullable) ist semantisch korrekt, aber: der `null`-Fall ist in der Description nicht erwähnt — LLM weiß nicht, ob es "nicht gefunden" als Fehler oder als leere Antwort interpretieren soll | `McpTools/DocsMcpTools.cs:34` |
| [F-MC-005](#f-mc-005) | Medium | `DocsMcpResources.authoring-guide` erwähnt keine Längen-Empfehlung für Einzeldokumente (`MaxContentLengthWarning` ist in `appsettings.json` und Validator, aber im LLM-Guide nicht erwähnt) | `McpTools/DocsMcpResources.cs:20-57` |
| [F-MC-006](#f-mc-006) | Low | Tool-Namen-Konvention (snake_case) wird in keiner Doku-Datei als Konvention festgehalten — wenn ein viertes Tool hinzukommt, gibt es keine Anleitung | `McpTools/DocsMcpTools.cs:16, 25, 34` |
| [F-MC-007](#f-mc-007) | Low | `CancellationToken` wird vom SDK durchgereicht, aber: das LLM hat keine Cancellation-Controls — bei langlaufenden Queries (F-PE-005 LIKE-Index-Scan) kann das LLM nicht abbrechen | `McpTools/DocsMcpTools.cs:17, 26, 35` |
| [F-MC-008](#f-mc-008) | Info | `DocumentSummary` und `DocumentDetail` als Return-Typen sind saubere DTOs — SDK generiert daraus das JSON-Schema | `Documents/DocumentSummary.cs`, `Documents/DocumentDetail.cs` |
| [F-MC-009](#f-mc-009) | Info | `ServerInstructions` setzt den MCP-Standard-Konventionen entsprechend (kurz, Tool-Liste, Workflow-Hint) | `McpTools/DocsMcpResources.cs:11-14` |
| [F-MC-010](#f-mc-010) | Info | `authoring-guide` deckt den Cold-Start-Fall (leeres docs-root) ab — wichtig für LLM-Onboarding | `McpTools/DocsMcpResources.cs:20-57` |

## Detail-Findings

### F-MC-001 — Tool-Description-Qualität (Edge-Cases & Fehler-Semantik fehlen)

**Schweregrad:** High (LLM-UX-Kernproblem)

**Beobachtung:**

**`list_children` (Zeile 16):**
```
"Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug leer ist)."
```

Was fehlt:
- Was passiert, wenn der `parentSlug` nicht existiert? Aktuell: leere Liste. LLM erwartet
  vielleicht einen Fehler.
- Was ist die Reihenfolge der Treffer? Aktuell: unspezifiziert (siehe F-PE-003). LLM
  sollte das wissen, um ggf. selbst zu sortieren.
- Wann ist `parentSlug` "leer"? `null`, `""`, beides? Aktuell: nur `null` matcht Root.
  LLM schickt vielleicht `""` und ist verwirrt.
- Gibt es eine maximale Anzahl? Aktuell: keine Cap. Bei einem breiten `parentSlug`
  könnten 1000 Items zurückkommen.

**`search_docs` (Zeile 25):**
```
"Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff."
```

Was fehlt:
- Welche Such-Semantik? `LIKE '%query%'` (Substring, case-insensitive auf Windows-Collation,
  case-sensitive auf Linux-Collation — siehe F-SE-004). LLM schickt `query = "Personal"`
  und erwartet case-sensitive — könnte auf Linux-DB scheitern.
- Ranking? Nein, alphabetisch sortiert (siehe `docs/04:48`). LLM weiß nicht, dass die
  *ersten* Treffer nicht die relevantesten sind.
- Max-Treffer-Anzahl? Aktuell: keine Cap (siehe F-PE-002). Bei breitem Query: Token-
  Budget-Sprengung.
- Special Characters? `%` und `_` werden in LIKE-Pattern als Wildcards interpretiert
  (siehe F-SE-001). LLM schickt `query = "50%"` und bekommt Treffer, die "50" + beliebiges
  Zeichen enthalten.

**`get_doc` (Zeile 34):**
```
"Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs."
```

Was fehlt:
- Was, wenn Slug nicht existiert? Aktuell: `null`. LLM muss das erkennen.
- Wie groß kann der Inhalt sein? `NVARCHAR(MAX)`, also mehrere MB. LLM hat Token-
  Budget.
- Enthält der Inhalt YAML-Front-Matter? Nein (das ist in `title`/`tags`/`synonyms`
  aufgeteilt). LLM erwartet vielleicht die ganze Datei.

**Detail-Datei:** [`_findings/F-MC-001-tool-description-quality.md`](_findings/F-MC-001-tool-description-quality.md)

**Fix-Empfehlung (Beispiel `list_children`):**
```csharp
[Description("""
    Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug
    weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

    Edge Cases:
    - parentSlug = null oder weggelassen: listet Root-Dokumente
    - parentSlug = "" (leerer String): wirft ArgumentException, nicht das gleiche wie null
    - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
    - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server akzeptiert,
      liefert leere Liste

    Beispiel:
    - list_children(parentSlug=null) → DocumentSummary[] der Root-Dokumente
    - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"

    Es gibt keine Cap; bei sehr breiten Verzeichnissen ggf. >100 Treffer.
    """)]
```

Plus für `search_docs` und `get_doc` analog.

**Aufwand:** ~30 Minuten für alle drei + Doku-Update in `docs/02` Abschnitt 4.D.

---

### F-MC-002 — Keine Tool-Beispiele in den Descriptions

**Schweregrad:** Medium (LLM-UX)

**Beobachtung:** Die aktuellen Descriptions sind *Definitionen*, nicht *Anleitungen*.
LLMs verarbeiten Beispiele oft besser als abstrakte Beschreibungen.

**Beispiel-Pattern (für LLM-UX):**
```json
// list_children(parentSlug="it")
[
  {"slug": "it/netzwerk", "title": "Netzwerk"},
  {"slug": "it/security", "title": "Security"}
]
```

Wenn das in der Description steht, hat das LLM ein konkretes Format-Verständnis und
muss nicht raten.

**Fix-Empfehlung:** Pro Tool ein konkretes JSON-Beispiel in der Description. Die
Beispiele sollten *valide* sein (syntaktisch und semantisch), sonst verwirrt das LLM
mehr als es hilft.

**Aufwand:** ~20 Minuten.

---

### F-MC-003 — `ServerInstructions` zentral undokumentiert

**Schweregrad:** Medium (Änderungs-Risiko)

**Beobachtung:** Siehe F-DK-002 in Dim 5. Hier nochmal aus API-Perspektive:

`ServerInstructions` ist die *Eingangstür* für jedes verbundene LLM. Bei Änderungen
(z.B. neuer Workflow-Schritt, geänderte Tool-Liste) gibt es keinen Test, keinen
Review-Prozess, keine Dokumentation, die die LLM-UX-Wirksamkeit prüft.

**Fix-Empfehlung:**
1. In `docs/02` (oder einer neuen `docs/06-LLM-UX.md`): den konkreten Wortlaut
   dokumentieren + Wirkungs-Achsen (Länge, Vokabular, Tool-Reihenfolge) festhalten.
2. Optional: Ein Smoke-Test, der `ServerInstructions` in einen LLM-Prompt einfügt
   und prüft, ob das LLM die Tools korrekt benennt (zu aufwendig für Unit-Test,
   aber als Doku-Anforderung sinnvoll).

**Aufwand:** ~5 Minuten (Doku).

---

### F-MC-004 — `null`-Semantik für `get_doc` undokumentiert

**Schweregrad:** Medium (Edge-Case, LLM-UX)

**Beobachtung:**
`GetDocAsync` returnt `DocumentDetail?` (nullable). Die Description sagt das nicht
explizit. LLMs neigen dazu, `null` als Fehler zu interpretieren und in eine
Fehlerbehandlungs-Schleife zu gehen.

**Beispiel-Schleife (häufig in LLM-Logs beobachtet):**
```
LLM: get_doc(slug="foo")
Tool: null
LLM: "foo existiert nicht. Ich versuche foo-bar."
LLM: get_doc(slug="foo-bar")
Tool: null
LLM: "..."
```

Mit klarer Description würde das LLM frühzeitig aufhören:
```csharp
[Description("""
    Lädt Titel und Inhalt eines einzelnen Dokuments.

    Returnt null, wenn der Slug nicht existiert (kein Fehler, einfach nicht da).
    Das LLM soll dann einen anderen Slug probieren oder die Anfrage abbrechen.
    """)]
```

**Fix:** In die Description explizit "Returnt null bei unbekanntem Slug" einbauen.

**Aufwand:** ~5 Minuten.

---

### F-MC-005 — `authoring-guide` Length-Warning fehlt

**Schweregrad:** Medium (LLM-UX)

**Beobachtung:** Der `authoring-guide` (Resource) lehrt LLMs, neue Doku zu schreiben.
Aber: er erwähnt nicht, dass zu lange Doku problematisch ist (`MaxContentLengthWarning`
in appsettings.json + Validator). Ein LLM, das einen 50-KB-Block als ein Doc schreibt,
bekommt beim nächsten `validate` eine Warning, aber das LLM weiß nicht warum.

**Fix-Empfehlung:** Im `authoring-guide` einen kurzen Hinweis:
> "Einzeldokumente sollten idealerweise unter 8.000 Zeichen bleiben (Schwelle
> konfigurierbar in appsettings.json). Längere Inhalte sind möglich, aber das LLM
> bekommt sie in `get_doc` als *ganzen* Content — Token-Budget!"

**Aufwand:** ~5 Minuten.

---

### F-MC-006 — Tool-Naming-Konvention undokumentiert

**Schweregrad:** Low (Prozess-Frage)

**Beobachtung:** Die drei Tools heißen `list_children`, `search_docs`, `get_doc`
— snake_case, alle mit Verb im Imperativ. Das ist MCP-Standard, aber nirgends im
Repo dokumentiert.

**Fix-Empfehlung:** Kurzer Absatz in `docs/02`:
> "MCP-Tool-Namen: snake_case, Verb im Imperativ. Beispiele: `list_children`,
> `search_docs`, `get_doc`. Beim Hinzufügen eines neuen Tools: Name mit Doku
> abgleichen."

**Aufwand:** ~2 Minuten.

---

### F-MC-007 — Cancellation für LLM nicht exposed

**Schweregrad:** Low (MCP-Standard-Limit, nicht behebbar)

**Beobachtung:** `CancellationToken` wird in alle Tool-Methoden durchgereicht, aber
LLM-Clients haben per MCP-Spec keine Möglichkeit, ein laufendes Tool abzubrechen
(außer durch Prozess-Kill). Bei einer 5-Sekunden-`search_docs`-Query auf einer
großen Tabelle (F-PE-005) muss das LLM warten.

**Fix-Empfehlung:** Akzeptiert. Ist ein MCP-Spec-Design, nicht behebbar in diesem
Projekt. Stattdessen: mit F-PE-005/F-PE-002 die Query-Performance verbessern, damit
Cancellation kein Thema ist.

**Aufwand:** 0 (akzeptiert).

---

### F-MC-008 / F-MC-009 / F-MC-010 — Info-Positive-Befunde

Die Typen `DocumentSummary` und `DocumentDetail` sind saubere DTOs (Records mit
klaren Properties), `ServerInstructions` ist idiom MCP, `authoring-guide` deckt
den Cold-Start-Fall ab. Alles positive Bestätigungen, kein Handlungsbedarf.

---

## Tool-API-Coherence-Matrix

| Aspekt | list_children | search_docs | get_doc |
| --- | --- | --- | --- |
| Args-Description | knapp | knapp | knapp |
| Edge-Cases dokumentiert | ❌ | ❌ | ❌ |
| Fehler-Semantik dokumentiert | ❌ | ❌ | ❌ |
| Beispiel-Output | ❌ | ❌ | ❌ |
| Beispiel-Args | ❌ | ❌ | ❌ |
| Sortierung dokumentiert | ❌ | ❌ (implizit) | n/a |
| Cap dokumentiert | ❌ | ❌ | n/a |
| `null`-Return dokumentiert | n/a | n/a | ❌ |
| Performance-Erwartung | ❌ | ❌ | ❌ |
| Token-Budget dokumentiert | ❌ | ❌ | ❌ |

8 von 30 Zellen sind "n/a", 22 sind "❌".

## Zusammenfassung Dim 9

- **10 Findings**, davon 1 × High, 4 × Medium, 2 × Low, 3 × Info.
- **Hauptthema:** Die Tool-Descriptions sind *technisch* korrekt (das MCP-SDK akzeptiert
  sie so), aber *funktional* unzureichend für LLM-Konsumenten. Ein LLM kann mit den
  aktuellen Descriptions die Tools *aufrufen*, aber nicht *optimal aufrufen*.
- **Quick Win:** F-MC-001 (Description-Ausbau) ist die wichtigste einzelne Verbesserung.
  Mit 30 Minuten Aufwand werden die drei Tools deutlich LLM-tauglicher.
- **Mittel- bis langfristig:** F-MC-002 (Beispiele), F-MC-003 (`ServerInstructions`
  dokumentieren), F-MC-004 (`null`-Semantik) sind die nächsten Schritte.
- **Insgesamt:** Das API-Design ist sauber (klare Typen, korrekte Nullable-Annotationen,
  idiomatische Tool-Namen). Was fehlt, ist die Doku-Schicht, die LLMs brauchen, um
  die Tools *gut* zu benutzen.

# Mini-Audit: `demo-docs/`

> **Scope:** Nur die drei Dateien unter `demo-docs/`. Nicht durch den vollen Code-Quality/
> Security/Architecture-Filter gejagt (siehe README, "Working-Tree-Entscheidung"). Stattdessen
> drei Aspekte: (1) Front-Matter-Korrektheit, (2) Slug-Konformität, (3) Feature-Coverage
> für `DocsValidator`/`FrontMatterParser`/`ImportService`.
> **Bewertungs-Basis:** `docs/04-Datenmodell-Validierung-Edgecases.md` + `docs/02` Architektur.

## Inventar

| Datei | Slug | Title | Tags | Synonyms | Content-Länge |
| --- | --- | --- | --- | --- | --- |
| `demo-docs/it.md` | `it` | "IT" | — | — | ~50 Zeichen |
| `demo-docs/it/netzwerk.md` | `it/netzwerk` | "Netzwerk" | `[netzwerk]` | — | ~60 Zeichen |
| `demo-docs/it/netzwerk/routing.md` | `it/netzwerk/routing` | "Routing-Tabelle Core-Switch" | `[netzwerk, switch, cisco]` | `[routing, gateway, statische-route]` | ~250 Zeichen |

## Befunde

### ✅ Korrektheit

| Aspekt | Status | Notiz |
| --- | --- | --- |
| Front Matter mit `---` geöffnet + geschlossen | ✅ alle 3 | konsistent |
| `title` als Pflichtfeld | ✅ alle 3 | vorhanden |
| `tags` als optionale Liste | ✅ 1 von 3 (`netzwerk.md`) | andere ohne Tags — gewollt |
| `synonyms` als optionale Liste | ✅ 1 von 3 (`routing.md`) | andere ohne Synonyms — gewollt |
| Slug-Konformität (Regex `^[a-z0-9]+(-[a-z0-9]+)*$` pro Segment) | ✅ alle 3 | lowercase, Bindestriche korrekt |
| Hierarchie-Vollständigkeit (Orphan-Check) | ✅ alle 3 | `it` → `it/netzwerk` → `it/netzwerk/routing` lückenlos |
| `it.md` + `it/`-Ordner parallel | ✅ | gemäß Edge-Case 4.1 erlaubt |
| Deutsche Umlaute in Title | ✅ | "Routing-Tabelle" — bewusst erlaubt in Title |
| Deutsche Umlaute in Slug | ✅ nicht vorhanden | würde Fehler werfen, aber keiner da |
| Datei-/Pfad-Referenzen in Content (`[text](file://...)`, `[text](x.md)`) | ✅ nicht vorhanden | würde Validator-Fehler werfen, keiner da |
| Content-Länge vs. `MaxContentLengthWarning=8000` | ✅ alle 3 weit unter Schwelle | keine Warnings |

**Konsequenz:** `validate` läuft fehlerfrei über `demo-docs/`. `import` würde alle 3
Dokumente korrekt in die DB schreiben.

### ⚠️ Feature-Coverage-Lücken

Die demo-docs decken nur einen Bruchteil der Edge Cases ab, die der Validator
kennen muss. Für ein *Beispiel-Set* ist das OK. Für ein *umfassendes
Test-Fixture* (das parallel zu den Unit-Tests läuft) fehlt:

| Fehlender Test-Fall | Würde prüfen | Datei:Ort |
| --- | --- | --- |
| Doc mit Umlauten im Title (z.B. `ä`, `ö`, `ü`) | `FrontMatterParser` UTF-8-Encoding | nicht in demo-docs |
| Doc mit Markdown-Image `![alt](file:///...)` | `DocsValidator.ValidateContentLinks` (Bild-Syntax) | nicht in demo-docs |
| Doc mit HTTP-Link `[text](https://...)` | Negative-Path von `ValidateContentLinks` (sollte kein Fehler) | nicht in demo-docs |
| Doc mit internem Slug-Link `[text](it/netzwerk)` | Negative-Path (Slug-Form, kein `.md`) | nicht in demo-docs |
| Doc mit Datei-Link `[text](erfassung.md)` | Positive-Path (sollte Validator-Fehler werfen) | nicht in demo-docs |
| Doc das die Längen-Schwelle (8000) überschreitet | `ValidateContentLength` Warning-Pfad | nicht in demo-docs |
| Doc mit `synonyms: []` (explizit leere Liste) | `FrontMatterParser`-Robustheit | nicht in demo-docs |
| Doc mit `title: ""` (leerer String) | Sollte Validator-Fehler werfen (`IsNullOrWhiteSpace`) | nicht in demo-docs |
| Doc mit `title: "   "` (nur Whitespace) | Sollte Validator-Fehler werfen | nicht in demo-docs (F-TS-006) |
| Doc mit fehlendem `---`-Schluss | Front-Matter-Parser-Error-Pfad | nicht in demo-docs |
| Doc mit ungültigem YAML | `YamlException`-Pfad in `FrontMatterParser` | nicht in demo-docs |
| Hierarchie mit Lücke (z.B. nur `it/netzwerk/routing.md` ohne `it.md`) | Orphan-Check-Pfad | nicht in demo-docs |

**Die ersten 6 sind echte Coverage-Lücken.** Die anderen 6 sind in
`tests/KnowHowToAI.Core.Tests/FrontMatterParserTests.cs` und
`tests/KnowHowToAI.Core.Tests/DocsValidatorTests.cs` bereits als Unit-Tests
abgedeckt — die demo-docs müssen sie also nicht duplizieren.

### 🟡 LLM-UX-Hinweis

`it/netzwerk.md` hat Title `"Netzwerk"` (sehr generisch). Für ein LLM, das die
Bibliothek via `search_docs(query="Netzwerk")` durchsucht, ist das hilfreich —
aber wenn der Title spezifischer wäre (z.B. `"Netzwerk-Infrastruktur"`), wäre
die LLM-UX präziser. Aktuell: das ist eine Doku-Design-Entscheidung, kein
technischer Fehler.

## Empfehlung

Wenn das Repo als Template für andere Wissensbibliotheken genutzt wird, sollte
ein `demo-docs/_examples/` Ordner mit zusätzlichen Edge-Case-Beispielen
angelegt werden, der mit dem `validate`-Befehl getestet werden kann. Aktuell
sind die Unit-Tests (`DocsValidatorTests`, `FrontMatterParserTests`) der
primäre Coverage-Mechanismus — die demo-docs sind nur Smoke-Fixture.

**Aufwand für vollständigen `demo-docs/_examples/`-Ordner:** ~30 Min.

## Mini-Audit-Scorecard

| Aspekt | Bewertung |
| --- | --- |
| Korrektheit (gegen `docs/04`) | ✅ einwandfrei |
| Konsistenz der drei Dateien untereinander | ✅ konsistent |
| Slug-Hierarchie | ✅ vollständig |
| Front-Matter-Korrektheit | ✅ in allen 3 |
| Coverage von `DocsValidator`-Edge-Cases | ⚠️ 6 fehlende Cases |
| Coverage von `FrontMatterParser`-Edge-Cases | ⚠️ 6 fehlende Cases, aber per Unit-Tests abgedeckt |
| LLM-UX (Title-Spezifität) | 🟡 generisch, aber OK |
| Funktioniert mit `validate` ohne Anpassung | ✅ ja |
| Funktioniert mit `import` ohne Anpassung | ✅ ja |
| Funktioniert mit `export` und Round-Trip | ✅ wahrscheinlich ja (nicht getestet im Audit) |

**Gesamt-Bewertung:** Die demo-docs sind *technisch korrekt* und *ausreichend als
Smoke-Fixture*. Sie sind *nicht* ausreichend als *umfassendes*
Edge-Case-Test-Set — dafür sind die Unit-Tests zuständig, was die richtige
Architektur-Entscheidung ist (Unit-Tests > Datei-basierte Tests).

## Kein Handlungsbedarf

Es gibt *keine* technischen Findings in den demo-docs. Alle Dateien sind
valide, die Hierarchie ist konsistent, die `validate`-Pipeline nimmt sie
ohne Fehler an. Die Lücken in der Edge-Case-Coverage sind *nice-to-have*,
nicht *must-fix*.

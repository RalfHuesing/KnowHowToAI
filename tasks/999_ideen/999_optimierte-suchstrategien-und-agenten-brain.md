# Idee: Optimierte Suchstrategien und Agenten-Brain (Metawissen-Navigation)

**Status:** Idee / Brainstorming, kein Entschluss. Unvoreingenommene Sammlung von Ansätzen zur token-effizienten, präzisen Wissensfindung in tiefen Hierarchien.

---

## 1. Ausgangslage & Problemstellung

In der KnowHowToAI-Plattform liegt Wissen tief hierarchisch und versioniert in einer Datenbank (Knoten, Pfade, Rollenkontexte). 

Wenn ein Agent einen Nutzer-Prompt erhält, steht er vor folgendem Problem:
* **Top-Down-Traversal ist eine Token- und Latenzfalle:** Hangelt sich der Agent Ebene für Ebene durch den Baum (`list_children` → Auswerten → `list_children` → Auswerten → `get_node`), entsteht bei breiten Verzweigungen (Branching Factor) ein exponentieller Overhead. Der Agent benötigt viele Round-Trips, verbraucht tausende Tokens und biegt leicht in falsche Zweige ab.
* **Flache Dumps sind unbrauchbar:** Ein Roh-Dump aller Knoten sprengt das Kontextfenster und vernichtet den hierarchischen Kontext.
* **Ziel:** Der Agent muss das vom Nutzer gewünschte Wissen finden – **token-effizient, deterministisch und inhaltlich präzise**.

---

## 2. Lösungsansätze im Überblick

### Ansatz 1: Invertierter Wort-Index & Synonym-Katalog

* **Konzept:** Ein klassischer Volltext-/Wort-Index (`Suchbegriff | Synonyme → Knoten-IDs / Fundstellen`). Der Agent sucht nach Begriffen und erhält konkrete Trefferpunkte.
* **Determinismus:** Sehr hoch (rein relationale SQL-Logik / SQL Server Full-Text Search).
* **Herausforderung (Wer pflegt die Synonyme?):**
  * *Manuell durch Menschen:* Scheitert in der Praxis fast immer an mangelnder Pflege und Veraltung.
  * *Automatisch beim Speichern (Worker):* Ein Hintergrundprozess generiert bei Neuanlage/Änderung eines Knotens via LLM automatisch 5–10 Schlagworte, Synonyme und typische Fragestellungen und legt sie in Index-Tabellen ab.
* **Vorteile:**
  * Extrem schnell (Millisekunden-Bereich in SQL).
  * 100 % deterministisch; findet exakte Treffer, IDs, Artikelnummern, Paragraphen.
  * Nahezu null Token-Kosten bei der reinen Abfrage.
* **Nachteile:**
  * Vokabular-Mismatch: Findet Umschreibungen oder Alltagssprache nicht, wenn das passende Synonym im Katalog fehlt.

---

### Ansatz 2: Contextual Breadcrumb-Index (Bottom-Up statt Top-Down)

* **Konzept:** Knoten werden nicht isoliert indiziert, sondern fest mit ihrem Pfad verknüpft. Der Index-Eintrag (`SearchContext`) lautet beispielsweise:
  `[IT > Sicherheit > Authentifizierung > Passwörter] Komplexitätsregeln, Ablaufzeit, Sonderzeichen`
* **Ergebnis-Präsentation (Hit-Cards):**
  Der Agent erhält keinen Roh-Dump, sondern eine kompakte Liste von Treffer-Karten (z. B. Top 5):
  ```yaml
  Treffer (Top 2 von 12):
  - id: "482"
    path: "IT / Sicherheit / Passwörter"
    title: "Passwort-Richtlinie 2026"
    snippet: "...Passwörter müssen mindestens 16 Zeichen lang sein..."
    score: 0.95
  - id: "105"
    path: "HR / Onboarding / Checklisten"
    title: "Erstzugangsdaten für Mitarbeiter"
    snippet: "...Initialpasswort muss am ersten Tag geändert werden..."
    score: 0.72
  ```
* **Vorteile:**
  * Der Agent sieht mit ~100–150 Tokens sofort den gesamten semantischen Kontext und Pfad.
  * Ermöglicht gezieltes Anspringen (`get_node(482)`) in Runde 2 statt schrittweiser Wanderung durch Ebenen.
* **Nachteile:**
  * Pfad-Ketten müssen bei strukturellen Verschiebungen im Baum im Index mitaktualisiert werden.

---

### Ansatz 3: Semantische Vektorsuche & Hybrid Search

* **Konzept:** Kombination aus Volltextsuche (FTS / BM25) und Dense Vector Embeddings (z. B. über HNSW / Vector-Index). 
* **Lösung des Synonym-Problems:**
  * Vektoren eliminieren die Notwendigkeit manueller Synonym-Listen vollständig. Umschreibungen („Wie mache ich blau?“ findet „Arbeitsunfähigkeit & Krankmeldung“) werden semantisch aufgelöst.
  * Lexikalischer Teil fängt exakte Fachbegriffe, Methodennamen oder Fehlercodes ab.
  * Ein Ranking-Algorithmus (z. B. Reciprocal Rank Fusion / RRF) führt beide Suchen zusammen.
* **Vorteile:**
  * Höchste Treffergenauigkeit bei natürlicher Sprache.
  * Keine manuelle Katalogpflege erforderlich.
* **Nachteile:**
  * Höhere Infrastruktur-Komplexität (Embedding-Pipeline, Vektorspeicher/Dimensionen).
  * Embeddings kosten bei der Ingestion einmalig Rechenleistung/Geld.

---

### Ansatz 4: Facettierte Filterung & Negativ-Filter (Ausschluss-Kriterien)

* **Konzept:** Die Such-Schnittstelle bietet explizite Filter für Metadaten und Pfade:
  * `include_paths`: z. B. `["/IT/*"]`
  * `exclude_paths`: z. B. `["/Archiv/*", "/Recht/*"]`
  * `role`: Rollenkontext (bereits Kern von KnowHowToAI)
* **Vorteile:**
  * Rein deterministisch und extrem schnell via SQL (`WHERE Path NOT LIKE '/Archiv/%'`).
  * Erkennt der Agent in Runde 1, dass Treffer aus `/Archiv` oder `/Vorlagen` stören, kann er diese gezielt ausblenden.
* **Nachteile:**
  * Setzt voraus, dass der Agent versteht, wie die Domäne strukturiert ist, um sinnvolle Filter zu setzen.

---

### Ansatz 5: Das „Agenten-Brain“ (Episodic Memory / Route Caching / Memex Trails)

* **Konzept:** Der Agent baut sich selbst ein meta-analytisches Gedächtnis auf: Er merkt sich erfolgreiche Pfade und bekannte Fallstricke für Anfragetypen:
  * *„User wollte X → Falle war A und B → Guter Zielknoten war NodeId 742“*.
  * Das Wissen selbst wird nicht modifiziert, sondern der Agent pflegt ein Navigations- und Erfahrungs-Netz.
* **Architektur-Details:**
  1. **Stabile Identitäten statt Pfade:** Das Brain merkt sich keine Pfad-Strings (`/A/B/C`), sondern persistente `NodeId`s (GUIDs). Wird der Knoten verschoben, bleibt der direkte Sprung intakt.
  2. **Self-Healing bei Strukturänderungen:**
     * Ruft der Agent einen gelernten Zielknoten auf und erhält `404 / Node Deleted` oder der Inhalt weicht semantisch drastisch ab:
     * Der Eintrag im Brain wird sofort invalidiert/gelöscht, der Agent fällt auf die reguläre Suche zurück und lernt den neuen Zielknoten.
  3. **Phasen-Evolution (0 % bis 80 %):**
     * *Phase 1 (0–50 % Datenbestand, hohe Reorganisation):* Bäume ändern sich ständig. Das Brain speichert primär **heuristische Suchtipps** (z. B. „Suche nach Begriff Y, meide Kategorie Z“) statt harter Verknüpfungen.
     * *Phase 2 (80 % Plateau, Stagnation):* Die Struktur beruhigt sich. Es entstehen feste **„Autobahnen“ (Shortcuts)**. Standardfragen werden ohne Baumsuche mit 1 Tool-Call direkt bedient.
* **Vorteile:**
  * Lernendes System: Wird mit jeder Nutzung schneller, präziser und token-günstiger.
  * Negative Heuristiken („Vermeide X“) sparen massiv Fehlversuche ein.
* **Nachteile:**
  * Cache-Invalidierungslogik erforderlich (Vermeidung von veraltetem Wissen).
  * Zusätzlicher Speicherbereich für das Meta-Wissen des Agenten.

---

### Ansatz 6: Query Expansion (Agenten-gestützt)

* **Konzept:** Bevor der Agent eine Suche absetzt, expandiert er den Nutzerbegriff eigenständig um Synonyme und verwandte Terme (z. B. *„Umzug“* → `Umzug OR Wohnungswechsel OR Sonderurlaub OR Umzugsbeihilfe`).
* **Vorteile:**
  * Das Sprachverständnis des Modells wird direkt genutzt; keine Wörterbücher in der DB nötig.
  * Funktioniert nahtlos mit normaler SQL-Volltextsuche.
* **Nachteile:**
  * Verbraucht etwas Agenten-Denkzeit (Tokens) vor dem ersten Tool-Call.

---

## 3. Gegenüberstellung: Determinismus/Technik vs. Token-Kosten

| Ansatz | Technischer / Programmatischer Aufwand | Laufende Token-Kosten (Agent) | Robustheit bei Strukturänderungen |
| :--- | :--- | :--- | :--- |
| **1. Invertierter Wort-Index** | Gering bis mittel (SQL FTS) | Nahezu 0 | Hoch |
| **2. Breadcrumb-Hit-Cards** | Gering (String-Konkatenation/Index) | Sehr gering (~100–150 Tokens/Suche) | Mittel (Index muss Pfade nachführen) |
| **3. Hybrid Search (Vektoren)** | Mittel bis hoch (Embeddings, Vektor-DB) | Sehr gering (Top-Treffer) | Hoch |
| **4. Negativ-/Pfad-Filter** | Gering (reine SQL-Prädikate) | Nahezu 0 (Teil des Tool-Calls) | Hoch |
| **5. Agenten-Brain (Route Cache)**| Mittel (Meta-Tabelle + Self-Healing) | Minimal im Best-Case (1 Call Direkt-Hit) | Sehr hoch (bei Verwendung von NodeIds + Self-Healing) |
| **6. Query Expansion** | Nahezu 0 (reine Prompt-Anweisung) | Gering bis mittel (LLM-Generierung) | Sehr hoch |

---

## 4. Synthese: Mehrstufiges Kombinations-Szenario (Multi-Tier Retrieval)

In der Praxis dürfte eine Kombination mehrerer Ansätze den optimalen Kompromiss aus Entwicklungsaufwand, Token-Kosten und Ausfallsicherheit bieten:

```text
[User Prompt]
      │
      ▼
┌─────────────────────────────────────────────────────────────┐
│ Tier 1: Agent-Brain / Shortcut-Lookup                       │
│ - Hat der Agent diesen Intent schon einmal gelöst?          │
│ - Liefert direkt NodeId(s) oder bekannte Negativ-Filter     │
└──────────────────────────────┬──────────────────────────────┘
                               │
               ┌───────────────┴───────────────┐
           Treffer                         Kein Treffer /
         (Direct Hit)                   Invalidiert (Self-Healing)
               │                               │
               │                               ▼
               │              ┌─────────────────────────────────────────────────┐
               │              │ Tier 2: Deterministic / Hybrid Search           │
               │              │ - FTS + Vektoren (oder Query Expansion)         │
               │              │ - Facetten-Filter (Rolle, Exclude-Paths)        │
               │              │ - Ausgabe: Max. 5 kompakte "Hit-Cards"          │
               │              └────────────────────────┬────────────────────────┘
               │                                       │
               │                               Top-Kandidaten
               ▼                                       ▼
┌─────────────────────────────────────────────────────────────┐
│ Tier 3: Gezielter Content-Abruf                             │
│ - Agent ruft get_node(targetId) auf                         │
│ - Bei erfolgreicher Antwort: Brain-Update (neuer Trail)     │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Offene Fragen & Diskussionspunkte

1. **Vektor-Unterstützung in der DB:**
   Soll Vektorsuche nativ in SQL Server abgebildet werden (z. B. neuere Vector-Funktionen oder externe Hilfsstrukturen), oder genügt anfangs eine rein deterministische FTS-Lösung mit Breadcrumbs?
2. **Brain-Persistenz & Scope:**
   Ist das „Agenten-Brain“ global für alle Agenten/Nutzer verfügbar, oder rollen- bzw. mandantenabhängig (um Datenschutz und Rollentrennung zu wahren)?
3. **Lebenszyklus von Trails:**
   Welche Kriterien führen zum Verfall (Decay) von gelernten Pfaden, wenn Knoten zwar noch existieren, sich ihr Inhalt aber inhaltlich geändert hat?

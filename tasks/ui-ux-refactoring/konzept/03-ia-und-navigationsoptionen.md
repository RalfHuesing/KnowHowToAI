# Informationsarchitektur und Navigationsoptionen

**Version:** 1.1 · **Status:** Hybrid für M2 verbindlich entschieden

## Bestehende Informationsarchitektur

Die Shell bietet vier stabile Ziele: `/` Start, `/search` Suche, `/transactions` Transactions und `/audiences` Zielgruppen. Weitere fachliche Seiten sind `/knowledge` und `/knowledge/{NodeId:guid}`, `/history` und `/transactions/{TransactionId:guid}`. Der Wissensarbeitsplatz kombiniert Tree, Breadcrumb und Node-Detail; Content ist keine eigene Route. `transactionId`, `snapshotId`, `releaseId` und `audienceId` rekonstruieren den Lesekontext über Query-Parameter. Genau ein ReadContext-Selektor ist erlaubt; `audienceId` ist bei zielgruppenaufgelösten Reads erforderlich.

## Option A – Wenige dichte Arbeitsbereiche

Dashboard, Knowledge, Search, History, Transactions und Audiences bleiben die primären Bereiche. Innerhalb eines Bereichs wechseln Abschnitte beziehungsweise Modi; globale Shell und Kontextleiste bleiben stabil.

**Vorteile:** kurze Wege für geübte Consultants, wenig Navigation, gemeinsamer Node-/Transaction-Kontext, gute Nutzung der bestehenden Routen.

**Risiken:** hohe Informationsdichte, Übergang Read-only → Working kann verborgen bleiben, lokale und globale Aktionen können konkurrieren.

## Option B – Mehr einfache Task-Seiten

„Finden“, „Lesen“, „Bearbeiten“, „Validieren“ und „Historie“ würden als stärker getrennte Seiten oder Unterziele geführt, mit mehr Breadcrumb-/Navigationswechseln.

**Vorteile:** einzelne Seiten sind leichter erklärbar und fokussierbar; Zustandsgrenzen sind sichtbar.

**Risiken:** mehr Navigation und Kontextverlust, doppelte Einstiege, schwieriger Erhalt der `NodeId`-/ReadContext-Parität, Gefahr neuer Routen ohne fachlichen Mehrwert.

## Festlegung: Hybrid ohne neue Routen

M2 adoptiert verbindlich den Hybrid: bestehende Fachrouten bleiben stabil;
gemeinsame Layoutprimitive, der node-lokale Einstieg und klar gruppierte lokale
Abschnitte reduzieren Dichte. Der Bearbeitungseinstieg bleibt auf der
Knowledge-Seite und öffnet nach expliziter Auswahl oder Start einer
Arbeitskopie den vorhandenen Editor derselben Node. Neue Routen oder ein neuer
Task-Seitenschnitt gehören nicht zu M2.

Für Support/Consultants schärft M2 zuerst den bestehenden Knowledge-
Arbeitsplatz und Transaction-Detailfluss. Die node-lokale
Bearbeitungsaffordance und ihre Arbeitskopienauswahl sind mit UX-001 verbindlich
gesetzt; eine neue Route oder ein separater Task-Seitenschnitt ist nicht Teil
dieser Etappe.

## Navigation und Aktionen

- Shell-Ziele sind global und bleiben vier vorhandene Links; der aktive Zustand ist sichtbar.
- ContextSelector ist global: Zielgruppe und ReadContext werden dort gewählt, nicht durch konkurrierende lokale Zielgruppenmechanismen.
- Node-Auswahl, Child/Root, Content-Speichern und Delete sind lokal am betroffenen Arbeitsabschnitt.
- History und Download sind lokale, seltene Folgewege.
- Der bestehende `PageActions`-Slot in `MainLayout` ist derzeit ungenutzt. Er ist nur Kandidat für echte seitenweite Aktionen, nicht für jede lokale Node-/Editoraktion. Es gibt keine Action Registry.
- `AudiencesPage` bleibt aus dem M2-Layout-Migrationsscope heraus; eine spätere IA-Entscheidung darf ihn nicht still mitändern.

## Erhaltungspflicht

Alle bestehenden Routen, stabilen IDs, Query-Kontexte, Links und Fachfunktionen bleiben erreichbar. IA-Korrekturen dürfen keinen zweiten fachlichen Zustand im Browser erzeugen. Route/Query bleiben rekonstruierbare Quelle; Circuit-State ist Cache und Auswahlhilfe.

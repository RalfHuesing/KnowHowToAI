# Informationsarchitektur und Navigationsoptionen

**Version:** 1.0 · **Status:** Optionen dokumentiert, Entscheidung offen

## Bestehende Informationsarchitektur

Die Shell bietet vier stabile Ziele: `/` Start, `/search` Suche, `/transactions` Transactions und `/roles` Rollen. Weitere fachliche Seiten sind `/knowledge` und `/knowledge/{NodeId:guid}`, `/history` und `/transactions/{TransactionId:guid}`. Der Wissensarbeitsplatz kombiniert Tree, Breadcrumb und Node-Detail; Content ist keine eigene Route. `transactionId`, `snapshotId`, `releaseId` und `roleId` rekonstruieren den Lesekontext über Query-Parameter. Genau ein ReadContext-Selektor ist erlaubt; `roleId` ist bei rollenaufgelösten Reads erforderlich.

## Option A – Wenige dichte Arbeitsbereiche

Dashboard, Knowledge, Search, History, Transactions und Roles bleiben die primären Bereiche. Innerhalb eines Bereichs wechseln Abschnitte beziehungsweise Modi; globale Shell und Kontextleiste bleiben stabil.

**Vorteile:** kurze Wege für geübte Consultants, wenig Navigation, gemeinsamer Node-/Transaction-Kontext, gute Nutzung der bestehenden Routen.

**Risiken:** hohe Informationsdichte, Übergang Read-only → Working kann verborgen bleiben, lokale und globale Aktionen können konkurrieren.

## Option B – Mehr einfache Task-Seiten

„Finden“, „Lesen“, „Bearbeiten“, „Validieren“ und „Historie“ würden als stärker getrennte Seiten oder Unterziele geführt, mit mehr Breadcrumb-/Navigationswechseln.

**Vorteile:** einzelne Seiten sind leichter erklärbar und fokussierbar; Zustandsgrenzen sind sichtbar.

**Risiken:** mehr Navigation und Kontextverlust, doppelte Einstiege, schwieriger Erhalt der `NodeId`-/ReadContext-Parität, Gefahr neuer Routen ohne fachlichen Mehrwert.

## Empfehlung: Hybrid, Entscheidung nach M2-Gate offen

M2 adoptiert einen Hybrid: bestehende Fachrouten bleiben stabil; gemeinsame Layoutprimitive, sichtbare Übergänge und klar gruppierte lokale Abschnitte reduzieren Dichte. Ein eigener Task darf eine neue Route oder einen neuen Task-Seitenschnitt erst nach dem Entscheidungsgate anlegen.

**Empfehlung:** Für Support/Consultants zuerst den bestehenden Knowledge-Arbeitsplatz und Transaction-Detailfluss schärfen. Eine node-lokale Bearbeitungsaffordance soll sichtbar werden; ob sie transaction-first oder node-lokal startet, bleibt fachlich offen.

## Navigation und Aktionen

- Shell-Ziele sind global und bleiben vier vorhandene Links; der aktive Zustand ist sichtbar.
- ContextSelector ist global: Rolle und ReadContext werden dort gewählt, nicht durch konkurrierende lokale Rollenmechanismen.
- Node-Auswahl, Child/Root, Content-Speichern und Delete sind lokal am betroffenen Arbeitsabschnitt.
- History und Download sind lokale, seltene Folgewege.
- Der bestehende `PageActions`-Slot in `MainLayout` ist derzeit ungenutzt. Er ist nur Kandidat für echte seitenweite Aktionen, nicht für jede lokale Node-/Editoraktion. Es gibt keine Action Registry.
- `RolesPage` bleibt aus dem M2-Layout-Migrationsscope heraus; eine spätere IA-Entscheidung darf ihn nicht still mitändern.

## Erhaltungspflicht

Alle bestehenden Routen, stabilen IDs, Query-Kontexte, Links und Fachfunktionen bleiben erreichbar. IA-Korrekturen dürfen keinen zweiten fachlichen Zustand im Browser erzeugen. Route/Query bleiben rekonstruierbare Quelle; Circuit-State ist Cache und Auswahlhilfe.

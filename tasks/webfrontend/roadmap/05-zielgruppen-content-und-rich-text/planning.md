# M5.0 – Planungs- und Konzeptgate

Stand: 2026-09-20

Status: abgeschlossen; M5.1–M5.5 freigegeben, M5.6 bleibt der manuelle Abschlussaudit.

## Ausgangslage

M4 ist nach der gezielten Korrekturrunde abgeschlossen. Seine Verträge zu `ChangeVersion`, parallelen Clients, Dirty-State, Navigation, Reconnect und `SnapshotConflict`-Reapply sind für M5 verbindlich. M5 ergänzt Zielgruppen-/Content-Webintegration; Backendverträge werden nur dort erweitert, wo die gemeinsame Schreib- und Sicherheitsbasis noch Parität benötigt.

## Geschlossene Entscheidungen

- Zielgruppenadministration bleibt Bestandteil von M5/V1. Die Ist-Dokumentation unter `docs/` wird erst in den Implementierungs-Leafs aktualisiert, wenn das Verhalten implementiert und belegt ist.
- Markdown-Quellmodus wird als kontrollierter zweiter Editorpfad aufgenommen. Er teilt Dirty-State, Servervalidierung und `ChangeVersion` mit WYSIWYG.
- O-029 ist entschieden: `src/KnowHowToAI.Server/Frontend/package.json`, `package-lock.json` und `build.mjs`; featurelokale Quelle unter `src/KnowHowToAI.Server/Web/Features/Content/`; deterministischer esbuild-Output unter `wwwroot/generated/content-editor`. `npm ci` stellt ausschließlich aus dem Lockfile wieder her, `npm run build` ruft die Builddefinition auf; beide Schritte werden in den normalen Build/Publish integriert. npm/Node werden nur beim Build/Publish benötigt; `node_modules` und Generated Output bleiben unversioniert; direkte und transitive Lizenzen werden in `THIRD-PARTY-NOTICES.md` erfasst.
- Fallback ist read-only. Eigener Content, bewusste Übernahme eines Fallback-Ausgangstextes, explizit leerer Content und Löschen sind getrennte, sichtbare Aktionen.
- `Independent`/`Derived` wechseln nur explizit. Derived erlaubt nur aktive explizite Sources, die der Benutzer über Node-Suche und Zielgruppe auswählt, und pinnt deren aktuelle Revision. Die Vorschau berechnet eine auswählbare Beispiel-Node; gespeicherte Resolution Orders werden nicht still umsortiert.
- Vitest bleibt konditional nach K-025 und wird nicht für dünnes Interop vorgezogen.

## Konzeptfolgen

- [Bedienkonzept und UI](../../konzept/02-bedienkonzept-und-ui.md) beschreibt Zielgruppenverwaltung, Editorquellmodus, Fallbackaktionen und Vorschau.
- [Content und Assets](../../konzept/03-content-und-assets.md) beschreibt die kanonische Markdown-/Sicherheitspolitik und die M5-Modussemantik.
- [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md) beschreibt die npm-/esbuild-Ablage und Testgrenzen.
- [Offene Fragen](../../konzept/07-entscheidungen-und-offene-fragen.md) enthält O-010 und O-029 nicht mehr; spätere Betriebs-/Assetentscheidungen bleiben offen.

## Freigabekriterien

- [x] Entscheidungen und Nicht-Ziele sind dokumentiert.
- [x] M5.1–M5.5 sind sequenziell geschnitten und enthalten ausführbare Leaf-Dateien im vollständigen Schema.
- [x] Keine verwaisten alten M5-Leaf-Dateien oder widersprüchlichen M5-Referenzen bleiben bestehen.
- [x] `docs/` wurde nicht vorzeitig als Ist-Zustand geändert.
- [x] Reine Dokumentationsprüfung mit `git diff --check` und Link-/Konsistenzprüfung ist vorgesehen.

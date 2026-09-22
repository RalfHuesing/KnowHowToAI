# M1-Audit – Arbeits- und Seitenfundament

Datum: 2026-09-23
Ergebnis: bestanden, keine Findings.

## Geprüfter Umfang

Begrenzter Abgleich von Konzept, M1-Roadmap und M1.1–M1.4-Leaves mit den
Projektregeln, dem aktuellen Code/Test-Diff und den dokumentierten Nachweisen.
Produktionscode wurde im Audit nicht geändert.

## Abnahmekriterien und Belege

- Beide Draft-Routen sind registriert: `/drafts` und
  `/drafts/{TransactionId:guid}`. `DraftPage` lädt die ID aus der Route und
  setzt nur diese offene Transaction als Arbeitskontext. Das Abschluss-Panel
  erhält genau dieses Transaction-Objekt; FastTests belegen Direktaufruf,
  Commit und Discard ausschließlich für die sichtbare ID sowie den
  unveränderten zweiten Entwurf (`DraftPagesTests`).
- Die Draft-Übersicht bietet bewusste Links zum Fortsetzen bzw. Prüfen. Der
  Browser-/Host-Smoke belegt Liste → Detail, die sichtbare aktive Draft-ID,
  Verwerfen, Reload und die anschließende leere Liste
  (`DraftWorkflowSmokeTests`). Fehlende IDs zeigen einen Fehler, ohne einen
  anderen Entwurf auszuwählen.
- `MainLayout` stellt genau ein Shell-`main` mit Skip-Link und globalem
  Feedback bereit. `PrimaryNavigation` enthält ausschließlich Wissen und
  Entwürfe; der ausgewählte Entwurf verlinkt direkt auf seine Detailroute.
  `MainLayoutTests` belegen Links, genau ein `main` und das Fehlen technischer
  Selektoren in der Shell-Navigation.
- Wissen und Draft-Seiten verwenden `PageFrame`. Die Browserprüfungen
  verifizieren genau ein `main` und `h1`, identische sichtbare Innenkanten,
  keine Page-Root-Overrides, Navigation offen/geschlossen sowie erreichbare
  Inhalte und Aktionen bei den dokumentierten Desktop-, Tablet- und
  Reflow-Breiten (`PageFrameSmokeTests`).
- Dirty-Navigation wird durch die gemeinsame `NavigationProtection` mit
  `NavigationLock` abgefangen. FastTests belegen Navigation im Dirty-Zustand,
  Bestätigung und Abbruch; externe Navigation erhält ebenfalls die Dirty-
  Bestätigung. Damit bleibt die gemeinsame Absicherung für Shell-Links und
  Browsernavigation am Shell-Rand verortet.
- Die M1.2-, M1.3- und M1.4-Abschlussnachweise melden erfolgreiche Builds,
  vollständige FastTests und AiNetLinter-Solution-Gates mit Score 10.0 und
  null Verstößen. Für M1.4 sind zudem Shell-/Redirect-, PageFrame- und
  Draft-Workflow-Browser-Smokes dokumentiert. Die dortigen neun
  `dead_code`-Advisories sind Razor-gebundene Members; sie sind keine
  Regelverstöße und ihre Markup-Verwendung ist im Leaf festgehalten.
- Die geplanten M2-/M3-Grenzen bleiben gewahrt: M1 baut Draft-Flächen und
  Shell auf; alter technischer Routenbestand bleibt bis M3 bestehen. Die noch
  von der bestehenden Knowledge-Seite benötigten Context-Dienste sind wie in
  M1.4 festgelegt weiterhin verfügbar. M2-Editor-/Baumarbeiten und M3-
  Routenbereinigung werden nicht vorgezogen.

## Entscheidung

Alle M1-Milestone-Kriterien sind durch Code und die vorgesehenen Nachweise
belegt. Keine Produktentscheidung oder Korrekturrunde ist erforderlich.
Milestone-Abnahme erteilt.

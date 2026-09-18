// Schmale Isolationsgrenze für den Ansichtsmodus des Hauptlayouts: meldet,
// ob die kompakte Breite unterhalb von 1280 CSS-Pixeln aktiv ist, in der
// Seitenbereiche über beschriftete Buttons ein- und ausgeklappt werden.
// Bewusst keine weitere Layout- oder Zustandslogik.

export function observeBreakpoint(dotNetReference) {
  const query = window.matchMedia("(min-width: 1280px)");
  const report = () => dotNetReference.invokeMethodAsync("NotifyCompactModeChangedAsync", !query.matches);
  report();
  query.addEventListener("change", report);
}

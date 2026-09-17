# Betrieb, Sicherheit und Risiken

## Zielbetrieb

- Ein ASP.NET-Core-Prozess hostet Blazor, technisch notwendige Web-Endpunkte, Assets und MCP.
- Eine Deployment-Einheit, eine Basiskonfiguration, standardmäßig ein HTTP(S)-Port.
- Zentraler Betrieb unter festgelegtem Hostnamen im Firmennetz.
- Kein Internet-Exposure.
- Reverse Proxy und Netzwerk unterstützen Blazor-SignalR/WebSockets und MCP Streamable HTTP.
- TLS ist auch intern anzustreben, spätestens bei sensiblen Kundendaten oder nicht vertrauenswürdigen Netzsegmenten.
- Fachlicher Zustand liegt nicht im Prozessspeicher; Mehrinstanzbetrieb bleibt dadurch später möglich.

Port- und Hostarchitektur: [Architektur, API und MCP](05-architektur-api-und-mcp.md#ein-prozess-und-ein-port).

## Explizite Auth-Abgrenzung

Der erste Stand besitzt **keine Authentifizierung und keine Autorisierung**. Auth ist kein versteckter Teil anderer Milestones, sondern ein separates späteres Vorhaben.

Bis dahin gilt:

- Zugriff ausschließlich aus explizit freigegebenen, vertrauenswürdigen Firmennetzsegmenten.
- Firewall und Netzwerksegmentierung bilden die Zugriffsschranke.
- `AllowedHosts` enthält nur tatsächliche Intranet-Hostnamen; keine Wildcard.
- Kein direkter Endkundenzugang.
- Keine Veröffentlichung von UI, Assets oder MCP im Internet.
- UI, Assets und MCP liegen im selben Origin.
- CORS wird nicht pauschal geöffnet; eine spätere externe Integrations-API benötigt ein eigenes Zugriffskonzept.
- Jeder erreichbare Client besitzt technisch Vollzugriff auf die angebotenen Endpunkte; dies wird nicht durch Content-Rollen eingeschränkt.

Vor Erweiterung des Nutzer- oder Netzwerkkreises folgt ein eigenes Konzept für Authentifizierung, Autorisierung, Audit und gegebenenfalls Mandantentrennung.

## Blazor-Betrieb

- Interactive Server verwendet SignalR und bevorzugt WebSockets.
- Jedes Browserfenster besitzt einen eigenen Circuit mit flüchtigem UI-Zustand.
- Circuit-Verlust darf keine fachlichen Änderungen verlieren, die bereits in der Working Transaction persistiert sind.
- Unpersistierter Editorzustand wird vor Navigation, Reconnect und Deployment bewusst behandelt.
- Bei späterem Mehrinstanzbetrieb werden Session Affinity oder eine alternative Renderstrategie separat bewertet.
- Lang laufende Publikationen blockieren keinen UI-Circuit; bei Bedarf werden sie als Hintergrundjob modelliert.

## Risiken und Gegenmaßnahmen

| Risiko | Gegenmaßnahme |
|---|---|
| UI umgeht Domainregeln | Ausschließlich Application Services; End-to-End-Vertragstests für UI-nahe Services und MCP |
| Benutzer verliert Arbeitskontext | Snapshot/Transaction/Rolle permanent sichtbar; Navigation und Circuit-Verlust absichern |
| Route-Kollision zwischen Blazor, Web-Endpunkten und MCP | Feste Präfixe; `/mcp` explizit; Routing-Smoke-Tests; `/api` für spätere Integration reserviert |
| Unterschiedliche DI-Scopes erzeugen versteckten Zustand | Stateless Application Services; fachlichen Kontext explizit übergeben |
| Proxy blockiert WebSockets oder Streaming | Intranet-Deploymenttest mit realem Proxy; Timeouts und Upgrade-Verhalten prüfen |
| Firmennetz wird mit Authentifizierung verwechselt | Kein Internet-Exposure; Netzgrenzen dokumentieren; Auth als separates Pflichtvorhaben vor Scope-Erweiterung |
| Tiefe Bäume werden langsam | Lazy Loading, Paging, virtuelle Darstellung, Suche und Breadcrumbs |
| Drag-and-drop erzeugt falsche Struktur | Zielvorschau, serverseitige Validierung, Transaction-Diff vor Commit |
| Rich-Text-Editor verändert Markdown | Roundtrip-Tests, Markdown-natives Modell, Quellmodus, keine HTML-first-Konvertierung |
| Bilder blähen Snapshots auf | Immutable, per Hash deduplizierte Assets; Snapshot referenziert statt kopiert |
| Content-Rollen werden als Rechte missverstanden | UI-Texte und Architektur trennen Zielgruppe strikt von Zugriffsschutz |
| Mehrere Clients committen parallel | `SnapshotConflict` sichtbar behandeln; geführtes manuelles Reapply statt implizitem Merge |
| MCP und UI weichen semantisch ab | Gemeinsame Application Services und Adaptertests gegen dieselben Use Cases |
| Frontend wird zum zweiten Produktkern | Fachlogik ausschließlich in Core/Application; dünne Adapter |

## Betriebsabnahme

- Alle Oberflächen sind unter einem Host und Port erreichbar.
- Root, `/mcp` und `/assets` kollidieren nicht; reserviertes `/api` wird nicht vom Blazor-Fallback verschluckt.
- Blazor funktioniert über den vorgesehenen Reverse Proxy per WebSocket und nach Reconnect.
- MCP-Streaming funktioniert über denselben Proxy.
- Nicht freigegebene Netzsegmente erreichen den Host nicht.
- Neustart des Prozesses lässt committed und offene persistierte Arbeitsstände fachlich rekonstruierbar.

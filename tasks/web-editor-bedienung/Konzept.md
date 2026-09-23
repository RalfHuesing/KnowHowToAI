---
status: ready
---

# Ruhiger Wissensbaum und fokussierter Node-Editor

## Intention

Der Wissensarbeitsplatz soll die fachliche Arbeit in den Vordergrund stellen:
Menschen erkennen Knoten am Titel statt an internen Kennungen, verschieben sie
mit der Maus direkt im Baum und bearbeiten ihren Inhalt in einer ausreichend
großen, übersichtlichen Fläche. Die Bedienung bleibt bei tiefen Bäumen und
ungespeicherten Eingaben verlässlich. Zielgruppe sind Desktopnutzer mit Maus.

## Belegter Ausgangspunkt

- [NodeDetails](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Node/NodeDetails.razor)
  zeigt die Node-GUID in „Technische Details“ und gekürzte Quell-Node-GUIDs
  unter „Herkunft des Inhalts“. Die Node-GUID in der Route dient weiterhin
  Direktaufruf und Reload; sie ist keine notwendige Beschriftung.
- [KnowledgeTree](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Tree/KnowledgeTree.razor)
  bietet bereits Pointer-Drag-and-drop sowie sichtbare Schaltflächen für
  „Verschieben“, „Davor“, „Darunter“ und „Danach“. Die letzteren bilden derzeit
  einen zweiten, hier nicht mehr gewünschten Verschiebeweg.
- [KnowledgeTreePageCache](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Tree/KnowledgeTreePageCache.cs)
  begrenzt geladene Seiten auf zehn. Beim Verdrängen eines Teilbaums werden
  dessen Kinder geleert und `IsExpanded` zurückgesetzt. Das erklärt ein
  unerwartetes Einklappen während der Arbeit. Kontextinitialisierung und
  Strukturänderungen sind daneben getrennt zu prüfen.
- [NodeDetailsPane](../../src/KnowHowToAI.Server/Web/Features/Knowledge/Node/NodeDetailsPane.razor)
  zeigt Lesetext, Kontext und technische Angaben über dem Bearbeitungsbereich;
  der [ContentEditor](../../src/KnowHowToAI.Server/Web/Features/Content/ContentEditor.razor)
  liegt darunter. Titel/Beschreibung und Inhalt haben heute getrennte
  Speichern-Aktionen. Fallback oder fehlender Inhalt erfordern bewusst das
  Erstellen einer eigenen Fassung; abgeleiteter Inhalt ist schreibgeschützt.

## Ziel und Scope

### Muss

- In nutzerlesbaren Web-Ansichten erscheinen keine rohen oder gekürzten
  Node-GUIDs. Knoten werden mit Titel und benötigtem Kontext bezeichnet,
  insbesondere im Dokument und bei der Herkunft abgeleiteter Inhalte.
  Interne IDs in Routen, DOM-Attributen und API-Verträgen bleiben funktionsfähig.
- Im Baum wird ein Knoten mit dem Zeiger vor, nach oder unter einen anderen
  gezogen. Die Drop-Zone und das erwartete Ergebnis sind währenddessen klar
  sichtbar; ungültige oder serverseitig abgelehnte Drops verändern die
  bestätigte Struktur nicht. Die sichtbaren Schaltflächen „Verschieben“,
  „Davor“, „Darunter“ und „Danach“ sowie ihr gesonderter Verschiebeablauf
  entfallen. Verschieben erfolgt ausschließlich per Maus-Drag-and-drop.
- Die eigens implementierte Tastaturnavigation und Tastaturverschiebung des
  Wissensbaums (unter anderem Pfeiltasten und Roving-Tabindex) entfallen mit
  ihren spezifischen Tests. Der Baum wird für Mausbedienung gestaltet; seine
  Rollen, `tabindex`-Werte und Ankündigungen dürfen danach keine nicht mehr
  vorhandene Interaktion versprechen.
- Auf-/Zuklappen, Anlegen, Paging und Auswahl bleiben als eigene
  Mausaktionen erhalten.
- Ein vom Nutzer geöffneter Zweig klappt während der Arbeit nicht allein durch
  Cache-Verdrängung, Auswahlwechsel oder einen Move zu. Die bestehende
  Begrenzung geladener Baumseiten darf nicht durch unbegrenztes Vorladen
  ausgehebelt werden. Bei verdrängten Daten bleibt die Aufklappabsicht
  erhalten; benötigte Kinder werden kontrolliert nachgeladen. Diese Absicht
  gilt nur für die laufende Wissensansicht. Vollständiger Reload oder Wechsel
  der Zielgruppe beginnt mit einem frischen Aufklappzustand. Es gibt dafür
  keine browserlokale oder serverseitige Präferenz.
- Der rechte Node-Bereich hat vier klar benannte Reiter: „Lesen“ zeigt den
  gerenderten Text, „Titel und Beschreibung“ enthält deren Eingabefelder,
  „Editor“ bietet dem Inhalt eine großzügige Bearbeitungsfläche und
  „Technische Details“ zeigt die ergänzenden Angaben ohne Node-GUID. Jeder
  Reiter zeigt nur die für seine Aufgabe nötigen Aktionen. Insbesondere haben
  „Titel und Beschreibung“ und „Editor“ jeweils ihre eigene explizite
  Speichern-Aktion; es gibt keinen gemeinsamen Speichern-Button für alle
  Felder. Nach Wechsel auf „Editor“ kann der Nutzer unmittelbar schreiben
  und dort speichern. Die bestehende Draft-Zuordnung und Dirty-Sicherung
  gelten auch beim Reiterwechsel; ungespeicherte Eingaben verschwinden nicht.
- Fallback, fehlender und abgeleiteter Inhalt bleiben korrekt eingeordnet:
  Keine automatische Kopie von Fallback-Text, kein Schreiben von Derived
  Content, kein stilles Überschreiben einer anderen Zielgruppe oder Revision.
  Bei Fallback oder fehlendem Inhalt beginnt der Editor eine leere eigene
  Fassung für die gewählte Zielgruppe; bei Derived zeigt er den Grund der
  Lesesperre und keine Speichern-Aktion. Titel und Beschreibung sind als
  Node-Metadaten in einem schreibbaren Current-/Draft-Kontext auch dann
  bearbeitbar, wenn nur der Inhalt abgeleitet ist. Die vier Reiter bleiben
  in allen Zuständen sichtbar; ihre Aktionen richten sich nach der
  Schreibbarkeit des jeweiligen Feldpakets.
- Die betroffenen Routen und Zustände erfüllen den gemeinsamen Layout-,
  `h1`- und Reflow-Vertrag für Desktop-Mausbedienung. Browsernachweise,
  Ist-Doku und auf den Verschiebeweg bezogene Test-/Regeltexte werden beim
  späteren Umsetzungsschritt passend aktualisiert.

## Umsetzungsschnitt und Architekturhinweise

Diese Hinweise definieren Verantwortungsgrenzen und schützen die fachlichen
Verträge; die spätere Roadmap teilt die Arbeit in ausführbare Einheiten.

1. **Kennungen und Herkunft:** Die sichtbare Node-ID in `NodeDetails.razor`
   und die gekürzte Quell-Node-ID in der Herkunftsdarstellung entfernen.
   `NodeId` bleibt für Route, Baumzuordnung und Mutation erhalten. Für eine
   Quellangabe den Knotentitel im selben Lesekontext auflösen; ist die Quelle
   dort nicht verfügbar, verständlich „Quellknoten nicht verfügbar“ anzeigen,
   niemals ersatzweise die GUID. Die Revisionskennung ist nicht die Node-ID;
   ihre Darstellung gehört ausschließlich in „Technische Details“.
2. **Baum-Interaktion:** `KnowledgeTree.razor` und
   `KnowledgeTree.razor.cs` besitzen Auswahl und Aktionen;
   `KnowledgeTree.razor.js` besitzt die Pointer-Geste und Drop-Indikatoren.
   Die vier Move-Buttons, `_movingNodeId`, `BeginMove` und
   `HandleKeyboardMoveAsync` entfallen. Ebenso entfallen die eigens
   implementierte Pfeiltastensteuerung, ihr JS-Keydown-Handler und der
   Roving-Tabindex. Vorhandene Drag-and-drop-Validierung und
   `TreeMoveCoordinator` werden weiterverwendet; keine zweite
   Verschiebelogik oder optimistische lokale Sortierung einführen.
   Visuelle Reiter sind native Schaltflächen mit eindeutigem aktivem Zustand;
   keine ARIA-Tabrollen behaupten, wenn kein entsprechendes Tastaturmuster
   angeboten wird. Diese bewusste Ausnahme betrifft die neue Baum- und
   Reiterbedienung; die übrige Shell wird nicht zurückgebaut.
3. **Aufklappzustand:** `KnowledgeTreeState` besitzt die flüchtige
   Aufklappabsicht; `KnowledgeTreePageCache` begrenzt weiterhin geladene
   Seiten auf zehn. Cache-Eviction darf Daten und Cursor freigeben, aber
   eine vom Nutzer gesetzte Aufklappabsicht nicht als Zuklappbefehl
   interpretieren. Ein geöffneter, aber aus dem Cache verdrängter Zweig
   zeigt seinen offenen Zustand und einen Ladeplatzhalter mit der Aktion
   „Unterknoten laden“. Deren Klick lädt nur diesen Zweig nach; nicht alle
   offenen Zweige zugleich nachladen. So bleibt
   die sichtbare Aufklappabsicht erhalten, ohne eine LRU-Schleife oder
   unbegrenzten Speicher zu erzeugen. Ein expliziter Zuklappklick entfernt
   die Absicht. `InitializeAsync` bei echtem neuen Arbeitskontext,
   vollständiger Reload und Zielgruppenwechsel setzen sie zurück. Dagegen
   muss der durch den ersten Write entstehende Wechsel von Current zum
   aktiven Draft sowie der Refresh nach Move die Aufklappabsicht erhalten.
   `KnowledgePage.RefreshKnowledgeTreeAfterMetadataChangeAsync` und die
   Move-Fehler-Recovery dürfen deshalb keinen gewöhnlichen Resetpfad
   verwenden. Nach einem Move werden Zielpfad und Auswahl wieder sichtbar,
   ohne andere Zweige zuzuklappen.
4. **Node-Bereich:** `NodeDetailsPane` besitzt den aktiven Reiter und den
   Node-/Zielgruppenwechsel; `NodeDetails` liefert Lese- und Detaildaten,
   `NodeMetadataEditor` speichert Titel/Beschreibung und `ContentEditor`
   speichert Inhalt. Der aktive Reiter startet bei einer neuen Node mit
   „Lesen“. Der Wechsel auf „Editor“ setzt den Cursor in den Inhaltseditor;
   „Titel und Beschreibung“ hat seine eigenen Formularaktionen. Während
   derselben Node und Zielgruppe bleiben einmal geöffnete
   Bearbeitungskomponenten beim Reiterwechsel montiert und werden nur
   visuell verborgen. Das ist nötig, weil `ContentEditor.DisposeAsync`
   andernfalls seinen Dirty-Eintrag entfernt und den lokalen Text verliert.
   Der Editor wird erst beim ersten Öffnen seines Reiters montiert und beim
   Wiederanzeigen korrekt vermessen und fokussiert. Kein Neuladen
   überschreibt seinen lokalen Zustand. Node-, Zielgruppen- und
   Routenwechsel nutzen den
   vorhandenen Dirty-Schutz. Gespeichert wird weiterhin nur das jeweils
   ausdrücklich bestätigte Feldpaket im aktiven Entwurf.
5. **Darstellung und Nachweis:** Das Node-`h1` bleibt über alle Reiter
   dasselbe; kein Reiter erzeugt ein zweites `h1`. Der Editor bekommt die
   verfügbare rechte Arbeitsfläche, während Prosa eine lesbare Breite haben
   darf. Fallback-/Derived-Hinweise bleiben dort sichtbar, wo die jeweilige
   Handlung sonst missverständlich wäre. `docs/WebUi.md`, die betroffenen
   UI-Regeln und Tests werden mit dem tatsächlich implementierten Verhalten
   synchronisiert. Das frühere Konzept unter `tasks/web-editor-neustart/`
   und dessen offener menschlicher Abnahmezustand werden nicht verändert.

## Nicht-Ziele

- Keine Änderung an Node-Identität, Routenformat, Core-/MCP-Verträgen,
  Snapshot-/Transaktionsmodell oder Persistenz nur wegen dieser UI-Änderung.
- Kein Autosave, automatisches Merge, neuer Rich-Text-Editor oder neue
  Web-Funktionen außerhalb von Baum und Node-Dokument.
- Keine eigene Tastaturbedienung als Produktfunktion für Baum oder Reiter.
  Die HTML-Grundfunktionen für Texteingabe und normale Browserbedienung werden
  nicht absichtlich blockiert.
- Keine allgemeine Entfernung von Shell, Skip-Link oder Browser-Fokusmechanik
  außerhalb des hier geänderten Baum- und Node-Bereichs.

## Verifikation

- Eine menschliche Sichtprüfung aller betroffenen Node-Ansichten findet keine
  sichtbare Node-GUID; Direktaufruf und Reload über die bestehende URL bleiben
  funktionsfähig. Herkunft abgeleiteter Inhalte bleibt verständlich.
- Browserabläufe prüfen Drop vor/nach/unter, ungültigen und abgelehnten Drop,
  Auswahl und Pfad nach Move. Ein Baum über der Cache-Grenze prüft, dass
  ausdrücklich geöffnete Zweige nicht überraschend zuklappen und der Speicher
  begrenzt bleibt. Es gibt keinen Abnahmenachweis für die bisherige
  Baum-Tastaturnavigation oder Tastaturverschiebung.
- Alle vier Reiter, ihre jeweils passenden Aktionen, das getrennte Speichern,
  der Wechsel mit ungespeicherten Eingaben, Fallback/Derived sowie Node- und
  Zielgruppenwechsel werden auf den betroffenen Routen geprüft. Der Editor
  nutzt die verfügbare rechte Fläche sinnvoll und bleibt bei den
  vereinbarten Viewports/Zoomstufen bedienbar.
- Änderungen an Layout und Semantik folgen
  [den Web-UI-Regeln](../../.agents/rules/WebUiHtmlCss.mdc), soweit sie nicht
  der hier ausdrücklich beschlossenen Mausbedienung widersprechen, und
  werden gegen [den Ist-Zustand](../../docs/WebUi.md) abgeglichen.

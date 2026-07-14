using System.ComponentModel;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Cli.McpTools;

// Liefert dem LLM das Datei-Format, wenn es (z.B. in einem leeren docs-root eines
// fremden Projekts) noch keine Doku-Beispiele zum Nachahmen vorfindet.
[McpServerResourceType]
public static class DocsMcpResources
{
    public const string ServerInstructions =
        "KnowHowToAI: durchsuchbare Wissensdatenbank. Lesen: list_children/search_docs/get_doc. " +
        "Neue oder geänderte Doku als .md-Datei im docs-root anlegen (Format siehe Resource " +
        "docs://authoring-guide), danach 'validate' und 'import' per CLI ausführen.";

    [McpServerResource(UriTemplate = "docs://authoring-guide", Name = "authoring-guide", MimeType = "text/markdown"),
     Description("Front-Matter-Format, Slug-Regeln und Hierarchie-Regeln für .md-Dateien im docs-root.")]
    public static string AuthoringGuide() => AuthoringGuideText;

    private const string AuthoringGuideText = """
        # Autoren-Guide

        Neue/geänderte Doku entsteht als .md-Datei im docs-root (Dateisystem), nicht
        direkt in der Datenbank. Nach dem Schreiben: `validate`, dann `import` per CLI
        ausführen, erst danach sehen list_children/search_docs/get_doc den neuen Stand.

        ## Datei = ein Dokument

        Pfad relativ zum docs-root, ohne `.md`, ist der Slug (z.B. `it/netzwerk/routing.md`
        -> Slug `it/netzwerk/routing`). Ordner bilden die Hierarchie.

        ## Front Matter (Pflicht)

        ```
        ---
        title: "Lesbarer Titel, Umlaute erlaubt"
        tags: [tag-eins, tag-zwei]
        synonyms: [suchbegriff-a, suchbegriff-b]
        ---
        Eigentlicher Markdown-Inhalt ab hier.
        ```

        `title` ist Pflicht, `tags`/`synonyms` optional. Nur diese drei Werte dürfen
        normales Deutsch inkl. Umlauten enthalten - nicht der Dateipfad.

        ## Slug-Regeln (strikt, pro Pfadsegment)

        Nur `a-z`, `0-9`, `-`. Kein Großbuchstabe, kein Umlaut, kein Leerzeichen, kein
        `_`, keine führenden/doppelten Bindestriche.
        Gültig: `it`, `netzwerk-routing`. Ungültig: `IT`, `Änderung`, `netzwerk_routing`.

        ## Hierarchie (Orphan-Check)

        Für Slug `a/b/c` müssen auch `a.md` und `a/b.md` existieren, sonst schlägt
        `validate` fehl. `it.md` plus Ordner `it/` mit weiteren Dateien ist normal und
        kein Konflikt.
        """;
}

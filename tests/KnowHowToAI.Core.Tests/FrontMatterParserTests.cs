using KnowHowToAI.Core.Documents;

namespace KnowHowToAI.Core.Tests;

public class FrontMatterParserTests
{
    [Fact]
    public void Parse_ValidFile_ReturnsDocumentWithParsedFields()
    {
        const string fileContent = """
            ---
            title: "Routing-Tabelle Core-Switch"
            tags: [netzwerk, switch, cisco]
            synonyms: [routing, gateway, statische-route]
            ---
            # Routing-Tabelle Core-Switch

            Hier steht der eigentliche Dokumenteninhalt.
            """;

        var document = FrontMatterParser.Parse("it/netzwerk/routing", fileContent);

        Assert.Equal("it/netzwerk/routing", document.Slug);
        Assert.Equal("it/netzwerk", document.ParentSlug);
        Assert.Equal("Routing-Tabelle Core-Switch", document.Title);
        Assert.Equal(["netzwerk", "switch", "cisco"], document.Tags);
        Assert.Equal(["routing", "gateway", "statische-route"], document.Synonyms);
        Assert.Contains("# Routing-Tabelle Core-Switch", document.Content);
    }

    [Fact]
    public void Parse_RootSlug_HasNoParentSlug()
    {
        const string fileContent = """
            ---
            title: "IT"
            ---
            # IT
            """;

        var document = FrontMatterParser.Parse("it", fileContent);

        Assert.Null(document.ParentSlug);
    }

    [Fact]
    public void Parse_WithoutTagsAndSynonyms_DefaultsToEmptyLists()
    {
        const string fileContent = """
            ---
            title: "Minimal"
            ---
            Inhalt.
            """;

        var document = FrontMatterParser.Parse("minimal", fileContent);

        Assert.Empty(document.Tags);
        Assert.Empty(document.Synonyms);
    }

    [Fact]
    public void Parse_MissingTitle_Throws()
    {
        const string fileContent = """
            ---
            tags: [a]
            ---
            Inhalt ohne Titel.
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => FrontMatterParser.Parse("kein-titel", fileContent));
        Assert.Contains("title", exception.Message);
    }

    [Fact]
    public void Parse_MissingOpeningDelimiter_Throws()
    {
        const string fileContent = "Kein Front Matter hier.";

        Assert.Throws<InvalidOperationException>(() => FrontMatterParser.Parse("ohne-front-matter", fileContent));
    }

    [Fact]
    public void Parse_UnclosedFrontMatter_Throws()
    {
        const string fileContent = """
            ---
            title: "Offen"
            Inhalt ohne schließendes Trennzeichen.
            """;

        Assert.Throws<InvalidOperationException>(() => FrontMatterParser.Parse("offen", fileContent));
    }

    [Fact]
    public void Parse_InvalidYaml_Throws()
    {
        const string fileContent = """
            ---
            title: "Kaputt
            tags: [a, b
            ---
            Inhalt.
            """;

        Assert.Throws<InvalidOperationException>(() => FrontMatterParser.Parse("kaputt", fileContent));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(" \t  ")]
    public void Parse_WhitespaceOnlyTitle_Throws(string title)
    {
        var fileContent = $"---\ntitle: \"{title}\"\n---\nInhalt.";

        var exception = Assert.Throws<InvalidOperationException>(() => FrontMatterParser.Parse("ws-titel", fileContent));
        Assert.Contains("title", exception.Message);
    }

    [Fact]
    public void Parse_FileWithUtf8Bom_Throws()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var body = System.Text.Encoding.UTF8.GetBytes("---\ntitle: \"Bom\"\n---\nInhalt.");
        var withBom = new byte[bom.Length + body.Length];
        bom.CopyTo(withBom, 0);
        body.CopyTo(withBom, bom.Length);
        var fileContent = System.Text.Encoding.UTF8.GetString(withBom);

        var exception = Assert.Throws<InvalidOperationException>(() => FrontMatterParser.Parse("mit-bom", fileContent));
        Assert.Contains("YAML Front Matter", exception.Message);
    }
}

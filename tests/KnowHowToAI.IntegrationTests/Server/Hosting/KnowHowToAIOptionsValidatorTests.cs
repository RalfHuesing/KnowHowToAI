using KnowHowToAI.Server.Configuration;
using Microsoft.Extensions.Options;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Unit-Tests für KnowHowToAIOptionsValidator.
/// Prüfen alle Validierungsregeln direkt über den Validator ohne Host-Overhead.
/// </summary>
[Trait("Category", "Unit")]
public sealed class KnowHowToAIOptionsValidatorTests
{
    private static readonly KnowHowToAIOptionsValidator Sut = new();

    private static KnowHowToAIOptions ValidOptions() => new()
    {
        Validation = new() { ContentSizeWarningBytes = 4096, ChildCountWarning = 25, HierarchyDepthWarning = 8, PossibleEmbeddedHeadingWarning = true },
        Retrieval = new() { DefaultPageSize = 20, MaximumPageSize = 100, SearchPageSize = 10, SearchMaximumPageSize = 50, SnippetMaximumCharacters = 300 },
        Storage = new() { CommandTimeoutSeconds = 30 },
        Migrations = new() { LockTimeoutSeconds = 60, ApplyOnStartup = true }
    };

    [Fact]
    public void ValidOptions_ReturnsSuccess()
    {
        var result = Sut.Validate(null, ValidOptions());
        Assert.True(result.Succeeded);
    }

    // --- Validation ---

    [Theory]
    [InlineData(511)]
    [InlineData(1_048_577)]
    public void ContentSizeWarningBytes_OutOfRange_Fails(int value)
    {
        var opts = ValidOptions() with { Validation = ValidOptions().Validation with { ContentSizeWarningBytes = value } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("ContentSizeWarningBytes", string.Join(" ", result.Failures ?? []));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10_001)]
    public void ChildCountWarning_OutOfRange_Fails(int value)
    {
        var opts = ValidOptions() with { Validation = ValidOptions().Validation with { ChildCountWarning = value } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("ChildCountWarning", string.Join(" ", result.Failures ?? []));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(257)]
    public void HierarchyDepthWarning_OutOfRange_Fails(int value)
    {
        var opts = ValidOptions() with { Validation = ValidOptions().Validation with { HierarchyDepthWarning = value } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("HierarchyDepthWarning", string.Join(" ", result.Failures ?? []));
    }

    // --- Retrieval ---

    [Fact]
    public void MaximumPageSize_Zero_Fails()
    {
        var opts = ValidOptions() with { Retrieval = ValidOptions().Retrieval with { MaximumPageSize = 0 } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("MaximumPageSize", string.Join(" ", result.Failures ?? []));
    }

    [Fact]
    public void DefaultPageSize_ExceedsMaximumPageSize_Fails()
    {
        var opts = ValidOptions() with { Retrieval = ValidOptions().Retrieval with { DefaultPageSize = 200 } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        var msg = string.Join(" ", result.Failures ?? []);
        Assert.Contains("DefaultPageSize", msg);
        Assert.Contains("MaximumPageSize", msg);
    }

    [Fact]
    public void SearchPageSize_ExceedsSearchMaximumPageSize_Fails()
    {
        var opts = ValidOptions() with { Retrieval = ValidOptions().Retrieval with { SearchPageSize = 100 } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("SearchPageSize", string.Join(" ", result.Failures ?? []));
    }

    [Theory]
    [InlineData(49)]
    [InlineData(4001)]
    public void SnippetMaximumCharacters_OutOfRange_Fails(int value)
    {
        var opts = ValidOptions() with { Retrieval = ValidOptions().Retrieval with { SnippetMaximumCharacters = value } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("SnippetMaximumCharacters", string.Join(" ", result.Failures ?? []));
    }

    // --- Storage ---

    [Theory]
    [InlineData(0)]
    [InlineData(601)]
    public void CommandTimeoutSeconds_OutOfRange_Fails(int value)
    {
        var opts = ValidOptions() with { Storage = new() { CommandTimeoutSeconds = value } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("CommandTimeoutSeconds", string.Join(" ", result.Failures ?? []));
    }

    // --- Migrations ---

    [Theory]
    [InlineData(0)]
    [InlineData(601)]
    public void LockTimeoutSeconds_OutOfRange_Fails(int value)
    {
        var opts = ValidOptions() with { Migrations = new() { LockTimeoutSeconds = value, ApplyOnStartup = true } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("LockTimeoutSeconds", string.Join(" ", result.Failures ?? []));
    }

    // --- Grenzwerte ---

    [Theory]
    [InlineData(512)]
    [InlineData(1_048_576)]
    public void ContentSizeWarningBytes_AtBoundary_Succeeds(int value)
    {
        var opts = ValidOptions() with { Validation = ValidOptions().Validation with { ContentSizeWarningBytes = value } };
        var result = Sut.Validate(null, opts);
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void MaximumPageSize_AtBoundary_WithMatchingDefault_Succeeds(int max)
    {
        var opts = ValidOptions() with
        {
            Retrieval = ValidOptions().Retrieval with
            {
                MaximumPageSize = max,
                DefaultPageSize = 1,
                SearchMaximumPageSize = 50,
                SearchPageSize = 1
            }
        };
        var result = Sut.Validate(null, opts);
        Assert.True(result.Succeeded);
    }

    // --- Auth ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DummyUserName_EmptyOrWhitespace_Fails(string userName)
    {
        var opts = ValidOptions() with { Auth = new() { DummyUserName = userName } };
        var result = Sut.Validate(null, opts);
        Assert.False(result.Succeeded);
        Assert.Contains("DummyUserName", string.Join(" ", result.Failures ?? []));
    }

    [Fact]
    public void DummyUserName_NonEmpty_Succeeds()
    {
        var opts = ValidOptions() with { Auth = new() { DummyUserName = "CustomActor" } };
        var result = Sut.Validate(null, opts);
        Assert.True(result.Succeeded);
    }
}

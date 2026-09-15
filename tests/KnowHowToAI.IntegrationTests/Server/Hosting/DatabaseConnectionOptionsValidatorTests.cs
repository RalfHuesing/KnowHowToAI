using KnowHowToAI.Server.Configuration;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Verhaltenstests für die separate Datenbankverbindungs-Konfiguration.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DatabaseConnectionOptionsValidatorTests
{
    private static readonly DatabaseConnectionOptionsValidator Sut = new();

    [Fact]
    public void SqlAuthentication_WithCompleteConfiguration_Succeeds()
    {
        var result = Sut.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void SqlAuthentication_WithoutCredentials_FailsWithoutExposingPassword()
    {
        const string password = "secret-password";
        var result = Sut.Validate(null, ValidOptions() with { UserName = string.Empty, Password = password });

        Assert.False(result.Succeeded);
        var failures = string.Join(" ", result.Failures ?? []);
        Assert.Contains("UserName", failures);
        Assert.DoesNotContain(password, failures);
    }

    [Fact]
    public void WindowsAuthentication_AllowsEmptySqlCredentials()
    {
        var result = Sut.Validate(null, ValidOptions() with
        {
            UseWindowsAuthentication = true,
            UserName = string.Empty,
            Password = string.Empty
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void UnresolvedServerPlaceholder_FailsWithoutExposingPassword()
    {
        const string password = "secret-password";
        var result = Sut.Validate(null, ValidOptions() with
        {
            Server = "%KNOWHOWTOAI_UNKNOWN_SERVER%",
            Password = password
        });

        Assert.False(result.Succeeded);
        var failures = string.Join(" ", result.Failures ?? []);
        Assert.Contains("Server", failures);
        Assert.DoesNotContain(password, failures);
    }

    private static DatabaseConnectionOptions ValidOptions() => new()
    {
        Server = "sqlserver",
        Database = "KnowHowToAi",
        UserName = "user",
        Password = "secret-password",
        UseWindowsAuthentication = false
    };
}

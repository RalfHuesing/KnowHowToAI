using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Connections;

/// <summary>
/// Unit-Tests für den festen ApplicationName in den von der Factory erzeugten
/// Connection Strings (Profiler-Zuordnung ohne Datenbankzugriff prüfbar).
/// </summary>
[Trait("Category", "Unit")]
public sealed class SqlConnectionFactoryTests
{
    [Fact]
    public void BuildConnectionString_WithoutApplicationName_SetsFixedApplicationName()
    {
        var raw = "Server=localhost\\SQLEXPRESS;Database=KnowHowToAi;Integrated Security=true;TrustServerCertificate=true";

        var built = new SqlConnectionStringBuilder(SqlConnectionFactory.BuildConnectionString(raw));

        Assert.Equal(SqlConnectionFactory.ApplicationName, built.ApplicationName);
        Assert.Equal("localhost\\SQLEXPRESS", built.DataSource);
        Assert.Equal("KnowHowToAi", built.InitialCatalog);
        Assert.True(built.IntegratedSecurity);
    }

    [Fact]
    public void BuildConnectionString_WithExistingApplicationName_OverridesIt()
    {
        var raw = "Server=localhost;Database=KnowHowToAi;Integrated Security=true;Application Name=FremdApp";

        var built = new SqlConnectionStringBuilder(SqlConnectionFactory.BuildConnectionString(raw));

        Assert.Equal(SqlConnectionFactory.ApplicationName, built.ApplicationName);
        Assert.NotEqual("FremdApp", built.ApplicationName);
    }
}

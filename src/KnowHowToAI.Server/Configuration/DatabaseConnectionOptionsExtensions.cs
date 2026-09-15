using KnowHowToAI.Storage.SqlServer.Configuration;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Server.Configuration;

internal static class DatabaseConnectionOptionsExtensions
{
    public static SqlStorageConnectionString ToStorageConnectionString(this DatabaseConnectionOptions options)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Environment.ExpandEnvironmentVariables(options.Server),
            InitialCatalog = options.Database,
            IntegratedSecurity = options.UseWindowsAuthentication,
            TrustServerCertificate = true
        };

        if (!options.UseWindowsAuthentication)
        {
            builder.UserID = options.UserName;
            builder.Password = options.Password;
        }

        return new SqlStorageConnectionString { Value = builder.ConnectionString };
    }
}

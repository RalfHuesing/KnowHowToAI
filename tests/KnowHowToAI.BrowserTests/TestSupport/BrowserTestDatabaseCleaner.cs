using Microsoft.Data.SqlClient;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Bereinigt ausschließlich die explizit ausgewählte Browser-Testdatenbank vor
/// einem Hoststart. Die Produktsektion <c>DatabaseConnection</c> wird hier nicht
/// gelesen und kann über diesen Pfad nicht zum Ziel werden.
/// </summary>
internal static class BrowserTestDatabaseCleaner
{
    public static async Task CleanSchemaAsync(
        BrowserTestDatabaseSettings settings,
        CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = settings.Server,
            InitialCatalog = settings.Database,
            IntegratedSecurity = settings.UseWindowsAuthentication,
            TrustServerCertificate = true
        };

        if (!settings.UseWindowsAuthentication)
        {
            builder.UserID = settings.UserName;
            builder.Password = settings.Password;
        }

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(CleanupSql, connection, transaction);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private const string CleanupSql = """
        DECLARE @sql nvarchar(max);

        SELECT @sql = STRING_AGG(
            N'ALTER TABLE ' + QUOTENAME(schemaInfo.name) + N'.' + QUOTENAME(parentTable.name) + N' DROP CONSTRAINT ' + QUOTENAME(foreignKeyInfo.name) + N';',
            CHAR(10))
            WITHIN GROUP (ORDER BY foreignKeyInfo.name)
        FROM sys.foreign_keys AS foreignKeyInfo
        INNER JOIN sys.tables AS parentTable ON parentTable.object_id = foreignKeyInfo.parent_object_id
        INNER JOIN sys.tables AS referencedTable ON referencedTable.object_id = foreignKeyInfo.referenced_object_id
        INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = parentTable.schema_id
        INNER JOIN sys.schemas AS referencedSchemaInfo ON referencedSchemaInfo.schema_id = referencedTable.schema_id
        WHERE schemaInfo.name = N'dbo'
          AND referencedSchemaInfo.name = N'dbo'
          AND parentTable.name LIKE N'KnowHowToAI[_]%'
          AND referencedTable.name LIKE N'KnowHowToAI[_]%'
          AND foreignKeyInfo.name LIKE N'%KnowHowToAI[_]%';
        IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;

        SELECT @sql = STRING_AGG(
            N'DROP TRIGGER ' + QUOTENAME(schemaInfo.name) + N'.' + QUOTENAME(triggerInfo.name) + N';',
            CHAR(10))
            WITHIN GROUP (ORDER BY triggerInfo.name)
        FROM sys.triggers AS triggerInfo
        INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = triggerInfo.parent_id
        INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
        WHERE schemaInfo.name = N'dbo'
          AND triggerInfo.name LIKE N'KnowHowToAI[_]%'
          AND tableInfo.name LIKE N'KnowHowToAI[_]%';
        IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;

        SELECT @sql = STRING_AGG(
            N'DROP TABLE ' + QUOTENAME(schemaInfo.name) + N'.' + QUOTENAME(tableInfo.name) + N';',
            CHAR(10))
            WITHIN GROUP (ORDER BY tableInfo.name DESC)
        FROM sys.tables AS tableInfo
        INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
        WHERE schemaInfo.name = N'dbo'
          AND tableInfo.name LIKE N'KnowHowToAI[_]%';
        IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;
        """;
}

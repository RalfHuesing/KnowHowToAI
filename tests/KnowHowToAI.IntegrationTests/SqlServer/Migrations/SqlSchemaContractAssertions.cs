using KnowHowToAI.IntegrationTests.TestSupport;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Migrations;

/// <summary>
/// Prüft den durch die Migrationen erzeugten SQL-Vertrag anhand der SQL-Server-Kataloge.
/// </summary>
internal static class SqlSchemaContractAssertions
{
    public static async Task AssertAsync(SqlTestDatabase database)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await AssertColumnsAsync(connection);
        await AssertAudienceCollationsAsync(connection);
        await AssertConstraintsAsync(connection);
        await AssertForeignKeysAsync(connection);
        await AssertIndexesAsync(connection);
    }

    private static async Task AssertAudienceCollationsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT tableInfo.name + N'|' + columnInfo.name + N'|' + columnInfo.collation_name
            FROM sys.columns AS columnInfo
            INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = columnInfo.object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            WHERE schemaInfo.name = N'dbo'
              AND (
                    (tableInfo.name = N'KnowHowToAI_Audience' AND columnInfo.name = N'AudienceId')
                 OR (tableInfo.name = N'KnowHowToAI_AudienceResolution' AND columnInfo.name = N'RequestedAudienceId')
                 OR (tableInfo.name = N'KnowHowToAI_AudienceResolution' AND columnInfo.name = N'CandidateAudienceId')
                 OR (tableInfo.name = N'KnowHowToAI_NodeContent' AND columnInfo.name = N'AudienceId')
                 OR (tableInfo.name = N'KnowHowToAI_ContentDependency' AND columnInfo.name = N'TargetAudienceId')
                 OR (tableInfo.name = N'KnowHowToAI_ContentDependency' AND columnInfo.name = N'SourceAudienceId')
              )
            ORDER BY tableInfo.name, columnInfo.name;
            """;
        var actual = await ReadRowsAsync(connection, sql);
        Assert.Equal(
            [
                "KnowHowToAI_Audience|AudienceId|Latin1_General_100_BIN2",
                "KnowHowToAI_AudienceResolution|CandidateAudienceId|Latin1_General_100_BIN2",
                "KnowHowToAI_AudienceResolution|RequestedAudienceId|Latin1_General_100_BIN2",
                "KnowHowToAI_ContentDependency|SourceAudienceId|Latin1_General_100_BIN2",
                "KnowHowToAI_ContentDependency|TargetAudienceId|Latin1_General_100_BIN2",
                "KnowHowToAI_NodeContent|AudienceId|Latin1_General_100_BIN2"
            ],
            actual);
    }

    private static async Task AssertColumnsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT tableInfo.name, columnInfo.name, typeInfo.name, columnInfo.max_length,
                   columnInfo.precision, columnInfo.scale, columnInfo.is_nullable,
                   CASE WHEN defaultInfo.object_id IS NULL THEN N'' ELSE N'DEFAULT' END
            FROM sys.columns AS columnInfo
            INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = columnInfo.object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            INNER JOIN sys.types AS typeInfo ON typeInfo.user_type_id = columnInfo.user_type_id
            LEFT JOIN sys.default_constraints AS defaultInfo ON defaultInfo.object_id = columnInfo.default_object_id
            WHERE schemaInfo.name = N'dbo' AND tableInfo.name LIKE N'KnowHowToAI[_]%'
            ORDER BY tableInfo.name, columnInfo.column_id;
            """;
        var actual = await ReadRowsAsync(connection, sql);
        Assert.Equal(ExpectedColumns.OrderBy(value => value, StringComparer.Ordinal), actual.OrderBy(value => value, StringComparer.Ordinal));
    }

    private static async Task AssertConstraintsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT tableInfo.name + N'|' + constraintInfo.name
            FROM sys.check_constraints AS constraintInfo
            INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = constraintInfo.parent_object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            WHERE schemaInfo.name = N'dbo' AND tableInfo.name LIKE N'KnowHowToAI[_]%'
            ORDER BY tableInfo.name, constraintInfo.name;
            """;
        var actual = await ReadRowsAsync(connection, sql);
        Assert.Equal(ExpectedChecks.OrderBy(value => value, StringComparer.Ordinal), actual.OrderBy(value => value, StringComparer.Ordinal));
    }

    private static async Task AssertForeignKeysAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT parentTable.name + N'|' + foreignKeyInfo.name + N'|' + referencedTable.name
            FROM sys.foreign_keys AS foreignKeyInfo
            INNER JOIN sys.tables AS parentTable ON parentTable.object_id = foreignKeyInfo.parent_object_id
            INNER JOIN sys.tables AS referencedTable ON referencedTable.object_id = foreignKeyInfo.referenced_object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = parentTable.schema_id
            WHERE schemaInfo.name = N'dbo' AND parentTable.name LIKE N'KnowHowToAI[_]%'
            ORDER BY parentTable.name, foreignKeyInfo.name;
            """;
        var actual = await ReadRowsAsync(connection, sql);
        Assert.Equal(ExpectedForeignKeys.OrderBy(value => value, StringComparer.Ordinal), actual.OrderBy(value => value, StringComparer.Ordinal));
    }

    private static async Task AssertIndexesAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT tableInfo.name + N'|' + indexInfo.name + N'|' +
                   keyColumns.Value + N'|' + includedColumns.Value + N'|' +
                   ISNULL(indexInfo.filter_definition, N'')
            FROM sys.indexes AS indexInfo
            INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = indexInfo.object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            OUTER APPLY (
                SELECT STRING_AGG(columnInfo.name + CASE WHEN indexColumnInfo.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END, N',')
                    WITHIN GROUP (ORDER BY indexColumnInfo.key_ordinal) AS Value
                FROM sys.index_columns AS indexColumnInfo
                INNER JOIN sys.columns AS columnInfo ON columnInfo.object_id = indexColumnInfo.object_id
                    AND columnInfo.column_id = indexColumnInfo.column_id
                WHERE indexColumnInfo.object_id = indexInfo.object_id AND indexColumnInfo.index_id = indexInfo.index_id
                    AND indexColumnInfo.is_included_column = 0
            ) AS keyColumns
            OUTER APPLY (
                SELECT ISNULL(STRING_AGG(columnInfo.name, N',')
                    WITHIN GROUP (ORDER BY indexColumnInfo.index_column_id), N'') AS Value
                FROM sys.index_columns AS indexColumnInfo
                INNER JOIN sys.columns AS columnInfo ON columnInfo.object_id = indexColumnInfo.object_id
                    AND columnInfo.column_id = indexColumnInfo.column_id
                WHERE indexColumnInfo.object_id = indexInfo.object_id AND indexColumnInfo.index_id = indexInfo.index_id
                    AND indexColumnInfo.is_included_column = 1
            ) AS includedColumns
            WHERE schemaInfo.name = N'dbo'
              AND (indexInfo.name LIKE N'IX[_]KnowHowToAI[_]%' OR indexInfo.name LIKE N'UQ[_]KnowHowToAI[_]%')
            ORDER BY tableInfo.name, indexInfo.name;
            """;
        var actual = await ReadRowsAsync(connection, sql);
        Assert.Equal(ExpectedIndexes.OrderBy(value => value, StringComparer.Ordinal), actual.OrderBy(value => value, StringComparer.Ordinal));
    }

    private static async Task<List<string>> ReadRowsAsync(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var rows = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? string.Empty : reader.GetValue(index).ToString());
            rows.Add(string.Join("|", values));
        }
        return rows;
    }

    private static readonly string[] ExpectedColumns =
    [
        "KnowHowToAI_ContentDependency|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_ContentDependency|TargetNodeId|uniqueidentifier|16|0|0|False|", "KnowHowToAI_ContentDependency|TargetAudienceId|nvarchar|100|0|0|False|", "KnowHowToAI_ContentDependency|SourceNodeId|uniqueidentifier|16|0|0|False|", "KnowHowToAI_ContentDependency|SourceAudienceId|nvarchar|100|0|0|False|", "KnowHowToAI_ContentDependency|SourceContentRevisionId|uniqueidentifier|16|0|0|False|",
        "KnowHowToAI_Node|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_Node|NodeId|uniqueidentifier|16|0|0|False|", "KnowHowToAI_Node|ParentNodeId|uniqueidentifier|16|0|0|True|", "KnowHowToAI_Node|Title|nvarchar|400|0|0|False|", "KnowHowToAI_Node|Description|nvarchar|2000|0|0|True|", "KnowHowToAI_Node|SortOrder|int|4|10|0|False|DEFAULT", "KnowHowToAI_Node|IsDeleted|bit|1|1|0|False|DEFAULT",
        "KnowHowToAI_NodeContent|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_NodeContent|NodeId|uniqueidentifier|16|0|0|False|", "KnowHowToAI_NodeContent|AudienceId|nvarchar|100|0|0|False|", "KnowHowToAI_NodeContent|ContentRevisionId|uniqueidentifier|16|0|0|False|", "KnowHowToAI_NodeContent|ContentMode|varchar|20|0|0|False|", "KnowHowToAI_NodeContent|ContentMd|nvarchar|-1|0|0|False|", "KnowHowToAI_NodeContent|IsDeleted|bit|1|1|0|False|DEFAULT",
        "KnowHowToAI_Release|ReleaseId|bigint|8|19|0|False|", "KnowHowToAI_Release|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_Release|Name|nvarchar|200|0|0|False|", "KnowHowToAI_Release|Description|nvarchar|1000|0|0|True|", "KnowHowToAI_Release|ReleasedAtUtc|datetime2|8|27|7|False|DEFAULT",
        "KnowHowToAI_Audience|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_Audience|AudienceId|nvarchar|100|0|0|False|", "KnowHowToAI_Audience|Name|nvarchar|200|0|0|False|", "KnowHowToAI_Audience|Description|nvarchar|1000|0|0|True|", "KnowHowToAI_Audience|IsDeleted|bit|1|1|0|False|DEFAULT",
        "KnowHowToAI_AudienceResolution|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_AudienceResolution|RequestedAudienceId|nvarchar|100|0|0|False|", "KnowHowToAI_AudienceResolution|CandidateAudienceId|nvarchar|100|0|0|False|", "KnowHowToAI_AudienceResolution|Priority|int|4|10|0|False|",
        "KnowHowToAI_SchemaMigration|Version|int|4|10|0|False|", "KnowHowToAI_SchemaMigration|Name|nvarchar|520|0|0|False|", "KnowHowToAI_SchemaMigration|ChecksumSha256|binary|32|0|0|False|", "KnowHowToAI_SchemaMigration|AppliedAtUtc|datetime2|8|27|7|False|DEFAULT",
        "KnowHowToAI_Snapshot|SnapshotId|bigint|8|19|0|False|", "KnowHowToAI_Snapshot|BaseSnapshotId|bigint|8|19|0|True|", "KnowHowToAI_Snapshot|State|varchar|20|0|0|False|", "KnowHowToAI_Snapshot|CreatedAtUtc|datetime2|8|27|7|False|DEFAULT", "KnowHowToAI_Snapshot|CommittedAtUtc|datetime2|8|27|7|True|",
        "KnowHowToAI_SystemState|Id|int|4|10|0|False|DEFAULT", "KnowHowToAI_SystemState|CurrentSnapshotId|bigint|8|19|0|False|", "KnowHowToAI_SystemState|LastUpdatedUtc|datetime2|8|27|7|False|DEFAULT",
        "KnowHowToAI_Transaction|TransactionId|uniqueidentifier|16|0|0|False|", "KnowHowToAI_Transaction|BaseSnapshotId|bigint|8|19|0|False|", "KnowHowToAI_Transaction|WorkingSnapshotId|bigint|8|19|0|False|", "KnowHowToAI_Transaction|State|varchar|20|0|0|False|", "KnowHowToAI_Transaction|ChangeVersion|bigint|8|19|0|False|DEFAULT", "KnowHowToAI_Transaction|CreatedAtUtc|datetime2|8|27|7|False|DEFAULT", "KnowHowToAI_Transaction|CommittedAtUtc|datetime2|8|27|7|True|", "KnowHowToAI_Transaction|Purpose|nvarchar|1000|0|0|True|", "KnowHowToAI_Transaction|Actor|nvarchar|400|0|0|True|", "KnowHowToAI_Transaction|Client|nvarchar|400|0|0|True|", "KnowHowToAI_Transaction|CommitMessage|nvarchar|2000|0|0|True|"
    ];

    private static readonly string[] ExpectedChecks =
    [
        "KnowHowToAI_ContentDependency|CK_KnowHowToAI_ContentDependency_NotSelf", "KnowHowToAI_Node|CK_KnowHowToAI_Node_Parent", "KnowHowToAI_Node|CK_KnowHowToAI_Node_SortOrder", "KnowHowToAI_Node|CK_KnowHowToAI_Node_Title", "KnowHowToAI_NodeContent|CK_KnowHowToAI_NodeContent_Mode", "KnowHowToAI_Release|CK_KnowHowToAI_Release_Name", "KnowHowToAI_Audience|CK_KnowHowToAI_Audience_Name", "KnowHowToAI_Audience|CK_KnowHowToAI_Audience_AudienceId", "KnowHowToAI_AudienceResolution|CK_KnowHowToAI_AudienceResolution_Priority", "KnowHowToAI_SchemaMigration|CK_KnowHowToAI_SchemaMigration_Name", "KnowHowToAI_SchemaMigration|CK_KnowHowToAI_SchemaMigration_Version", "KnowHowToAI_Snapshot|CK_KnowHowToAI_Snapshot_Base", "KnowHowToAI_Snapshot|CK_KnowHowToAI_Snapshot_State", "KnowHowToAI_Snapshot|CK_KnowHowToAI_Snapshot_Timestamps", "KnowHowToAI_Snapshot|CK_KnowHowToAI_Snapshot_WorkingBase", "KnowHowToAI_SystemState|CK_KnowHowToAI_SystemState_Singleton", "KnowHowToAI_Transaction|CK_KnowHowToAI_Transaction_ChangeVersion", "KnowHowToAI_Transaction|CK_KnowHowToAI_Transaction_Snapshots", "KnowHowToAI_Transaction|CK_KnowHowToAI_Transaction_State", "KnowHowToAI_Transaction|CK_KnowHowToAI_Transaction_Timestamps"
    ];

    private static readonly string[] ExpectedForeignKeys =
    [
        "KnowHowToAI_ContentDependency|FK_KnowHowToAI_ContentDependency_Source|KnowHowToAI_NodeContent", "KnowHowToAI_ContentDependency|FK_KnowHowToAI_ContentDependency_Target|KnowHowToAI_NodeContent", "KnowHowToAI_Node|FK_KnowHowToAI_Node_Parent|KnowHowToAI_Node", "KnowHowToAI_Node|FK_KnowHowToAI_Node_Snapshot|KnowHowToAI_Snapshot", "KnowHowToAI_NodeContent|FK_KnowHowToAI_NodeContent_Node|KnowHowToAI_Node", "KnowHowToAI_NodeContent|FK_KnowHowToAI_NodeContent_Audience|KnowHowToAI_Audience", "KnowHowToAI_Release|FK_KnowHowToAI_Release_Snapshot|KnowHowToAI_Snapshot", "KnowHowToAI_Audience|FK_KnowHowToAI_Audience_Snapshot|KnowHowToAI_Snapshot", "KnowHowToAI_AudienceResolution|FK_KnowHowToAI_AudienceResolution_CandidateAudience|KnowHowToAI_Audience", "KnowHowToAI_AudienceResolution|FK_KnowHowToAI_AudienceResolution_RequestedAudience|KnowHowToAI_Audience", "KnowHowToAI_AudienceResolution|FK_KnowHowToAI_AudienceResolution_Snapshot|KnowHowToAI_Snapshot", "KnowHowToAI_Snapshot|FK_KnowHowToAI_Snapshot_BaseSnapshot|KnowHowToAI_Snapshot", "KnowHowToAI_SystemState|FK_KnowHowToAI_SystemState_Snapshot|KnowHowToAI_Snapshot", "KnowHowToAI_Transaction|FK_KnowHowToAI_Transaction_BaseSnapshot|KnowHowToAI_Snapshot", "KnowHowToAI_Transaction|FK_KnowHowToAI_Transaction_WorkingSnapshot|KnowHowToAI_Snapshot"
    ];

    private static readonly string[] ExpectedIndexes =
    [
        "KnowHowToAI_ContentDependency|IX_KnowHowToAI_ContentDependency_Source|SnapshotId ASC,SourceNodeId ASC,SourceAudienceId ASC|SourceContentRevisionId,TargetNodeId,TargetAudienceId|", "KnowHowToAI_Node|IX_KnowHowToAI_Node_NodeId|NodeId ASC,SnapshotId ASC|ParentNodeId,SortOrder,IsDeleted|", "KnowHowToAI_Node|UQ_KnowHowToAI_Node_ActiveRoot|SnapshotId ASC||([ParentNodeId] IS NULL AND [IsDeleted]=(0))", "KnowHowToAI_Node|UQ_KnowHowToAI_Node_ActiveSiblingSortOrder|SnapshotId ASC,ParentNodeId ASC,SortOrder ASC|NodeId,Title,Description|([ParentNodeId] IS NOT NULL AND [IsDeleted]=(0))", "KnowHowToAI_NodeContent|IX_KnowHowToAI_NodeContent_Revision|ContentRevisionId ASC,SnapshotId ASC|NodeId,AudienceId,ContentMode,IsDeleted|", "KnowHowToAI_NodeContent|IX_KnowHowToAI_NodeContent_Audience|SnapshotId ASC,AudienceId ASC,IsDeleted ASC|NodeId,ContentRevisionId,ContentMode|", "KnowHowToAI_Release|IX_KnowHowToAI_Release_Snapshot|SnapshotId ASC,ReleasedAtUtc ASC|Name|", "KnowHowToAI_Release|UQ_KnowHowToAI_Release_Name|Name ASC||", "KnowHowToAI_Audience|IX_KnowHowToAI_Audience_AudienceId|AudienceId ASC,SnapshotId ASC|Name,IsDeleted|", "KnowHowToAI_AudienceResolution|IX_KnowHowToAI_AudienceResolution_Candidate|SnapshotId ASC,CandidateAudienceId ASC|RequestedAudienceId,Priority|", "KnowHowToAI_AudienceResolution|UQ_KnowHowToAI_AudienceResolution_Candidate|SnapshotId ASC,RequestedAudienceId ASC,CandidateAudienceId ASC||", "KnowHowToAI_SchemaMigration|UQ_KnowHowToAI_SchemaMigration_Name|Name ASC||", "KnowHowToAI_Snapshot|IX_KnowHowToAI_Snapshot_BaseSnapshot|BaseSnapshotId ASC||([BaseSnapshotId] IS NOT NULL)", "KnowHowToAI_Snapshot|IX_KnowHowToAI_Snapshot_State|State ASC,CreatedAtUtc ASC|BaseSnapshotId,CommittedAtUtc|", "KnowHowToAI_Transaction|IX_KnowHowToAI_Transaction_State|State ASC,CreatedAtUtc ASC|BaseSnapshotId,WorkingSnapshotId,ChangeVersion,CommittedAtUtc|", "KnowHowToAI_Transaction|UQ_KnowHowToAI_Transaction_WorkingSnapshot|WorkingSnapshotId ASC||"
    ];
}

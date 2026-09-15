-- 0003_create_nodes_and_content.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Erstellt die globale Node-Hierarchie, Node-Content und Content-Abhängigkeiten.

IF OBJECT_ID(N'dbo.KnowHowToAI_Node', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_Node (
        SnapshotId          BIGINT          NOT NULL,
        NodeId              UNIQUEIDENTIFIER NOT NULL,
        ParentNodeId        UNIQUEIDENTIFIER NULL,
        Title               NVARCHAR(200)   NOT NULL,
        Description         NVARCHAR(1000)  NULL,
        SortOrder           INT             NOT NULL CONSTRAINT DF_KnowHowToAI_Node_SortOrder DEFAULT 0,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_KnowHowToAI_Node_IsDeleted DEFAULT 0,
        CONSTRAINT PK_KnowHowToAI_Node PRIMARY KEY CLUSTERED (SnapshotId, NodeId),
        CONSTRAINT FK_KnowHowToAI_Node_Snapshot FOREIGN KEY (SnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId),
        CONSTRAINT FK_KnowHowToAI_Node_Parent FOREIGN KEY (SnapshotId, ParentNodeId)
            REFERENCES dbo.KnowHowToAI_Node (SnapshotId, NodeId)
    );

    CREATE NONCLUSTERED INDEX IX_KnowHowToAI_Node_Parent
        ON dbo.KnowHowToAI_Node (SnapshotId, ParentNodeId, IsDeleted)
        INCLUDE (Title, SortOrder);

    CREATE NONCLUSTERED INDEX IX_KnowHowToAI_Node_NodeId
        ON dbo.KnowHowToAI_Node (NodeId, SnapshotId);
END;

IF OBJECT_ID(N'dbo.KnowHowToAI_NodeContent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_NodeContent (
        SnapshotId          BIGINT          NOT NULL,
        NodeId              UNIQUEIDENTIFIER NOT NULL,
        RoleId              NVARCHAR(50)    NOT NULL,
        ContentRevisionId   UNIQUEIDENTIFIER NOT NULL,
        ContentMode         VARCHAR(20)     NOT NULL,
        ContentMd           NVARCHAR(MAX)   NOT NULL,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_KnowHowToAI_NodeContent_IsDeleted DEFAULT 0,
        CONSTRAINT PK_KnowHowToAI_NodeContent PRIMARY KEY CLUSTERED (SnapshotId, NodeId, RoleId),
        CONSTRAINT CK_KnowHowToAI_NodeContent_Mode CHECK (ContentMode IN ('Independent', 'Derived')),
        CONSTRAINT FK_KnowHowToAI_NodeContent_Node FOREIGN KEY (SnapshotId, NodeId)
            REFERENCES dbo.KnowHowToAI_Node (SnapshotId, NodeId),
        CONSTRAINT FK_KnowHowToAI_NodeContent_Role FOREIGN KEY (SnapshotId, RoleId)
            REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId)
    );

    CREATE NONCLUSTERED INDEX IX_KnowHowToAI_NodeContent_Revision
        ON dbo.KnowHowToAI_NodeContent (ContentRevisionId);
END;

IF OBJECT_ID(N'dbo.KnowHowToAI_ContentDependency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_ContentDependency (
        SnapshotId                  BIGINT          NOT NULL,
        TargetNodeId                UNIQUEIDENTIFIER NOT NULL,
        TargetRoleId                NVARCHAR(50)    NOT NULL,
        SourceNodeId                UNIQUEIDENTIFIER NOT NULL,
        SourceRoleId                NVARCHAR(50)    NOT NULL,
        SourceContentRevisionId     UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_KnowHowToAI_ContentDependency PRIMARY KEY CLUSTERED (SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId),
        CONSTRAINT FK_KnowHowToAI_ContentDependency_Target FOREIGN KEY (SnapshotId, TargetNodeId, TargetRoleId)
            REFERENCES dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId),
        CONSTRAINT FK_KnowHowToAI_ContentDependency_SourceNode FOREIGN KEY (SnapshotId, SourceNodeId)
            REFERENCES dbo.KnowHowToAI_Node (SnapshotId, NodeId),
        CONSTRAINT FK_KnowHowToAI_ContentDependency_SourceRole FOREIGN KEY (SnapshotId, SourceRoleId)
            REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId)
    );
END;

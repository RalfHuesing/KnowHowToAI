-- 0003_create_nodes_and_content.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Erstellt die globale Node-Hierarchie, Node-Content und Content-Abhängigkeiten.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

CREATE TABLE dbo.KnowHowToAI_Node (
    SnapshotId          BIGINT           NOT NULL,
    NodeId              UNIQUEIDENTIFIER NOT NULL,
    ParentNodeId        UNIQUEIDENTIFIER NULL,
    Title               NVARCHAR(200)    NOT NULL,
    Description         NVARCHAR(1000)   NULL,
    SortOrder           INT              NOT NULL
        CONSTRAINT DF_KnowHowToAI_Node_SortOrder DEFAULT 0,
    IsDeleted           BIT              NOT NULL
        CONSTRAINT DF_KnowHowToAI_Node_IsDeleted DEFAULT 0,
    CONSTRAINT PK_KnowHowToAI_Node PRIMARY KEY CLUSTERED (SnapshotId, NodeId),
    CONSTRAINT CK_KnowHowToAI_Node_Title CHECK (LEN(Title) > 0),
    CONSTRAINT CK_KnowHowToAI_Node_SortOrder CHECK (SortOrder >= 0),
    CONSTRAINT CK_KnowHowToAI_Node_Parent CHECK (
        ParentNodeId IS NULL OR ParentNodeId <> NodeId
    ),
    CONSTRAINT FK_KnowHowToAI_Node_Snapshot FOREIGN KEY (SnapshotId)
        REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId),
    CONSTRAINT FK_KnowHowToAI_Node_Parent FOREIGN KEY (SnapshotId, ParentNodeId)
        REFERENCES dbo.KnowHowToAI_Node (SnapshotId, NodeId)
);

CREATE UNIQUE NONCLUSTERED INDEX UQ_KnowHowToAI_Node_ActiveRoot
    ON dbo.KnowHowToAI_Node (SnapshotId)
    WHERE ParentNodeId IS NULL AND IsDeleted = 0;

CREATE UNIQUE NONCLUSTERED INDEX UQ_KnowHowToAI_Node_ActiveSiblingSortOrder
    ON dbo.KnowHowToAI_Node (SnapshotId, ParentNodeId, SortOrder)
    INCLUDE (NodeId, Title, Description)
    WHERE ParentNodeId IS NOT NULL AND IsDeleted = 0;

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_Node_NodeId
    ON dbo.KnowHowToAI_Node (NodeId, SnapshotId)
    INCLUDE (ParentNodeId, SortOrder, IsDeleted);

CREATE TABLE dbo.KnowHowToAI_NodeContent (
    SnapshotId          BIGINT           NOT NULL,
    NodeId              UNIQUEIDENTIFIER NOT NULL,
    RoleId              NVARCHAR(50)     COLLATE Latin1_General_100_BIN2 NOT NULL,
    ContentRevisionId   UNIQUEIDENTIFIER NOT NULL,
    ContentMode         VARCHAR(20)      NOT NULL,
    ContentMd           NVARCHAR(MAX)    NOT NULL,
    IsDeleted           BIT              NOT NULL
        CONSTRAINT DF_KnowHowToAI_NodeContent_IsDeleted DEFAULT 0,
    CONSTRAINT PK_KnowHowToAI_NodeContent
        PRIMARY KEY CLUSTERED (SnapshotId, NodeId, RoleId),
    CONSTRAINT CK_KnowHowToAI_NodeContent_Mode CHECK (
        ContentMode IN ('Independent', 'Derived')
    ),
    CONSTRAINT FK_KnowHowToAI_NodeContent_Node FOREIGN KEY (SnapshotId, NodeId)
        REFERENCES dbo.KnowHowToAI_Node (SnapshotId, NodeId),
    CONSTRAINT FK_KnowHowToAI_NodeContent_Role FOREIGN KEY (SnapshotId, RoleId)
        REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId)
);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_NodeContent_Revision
    ON dbo.KnowHowToAI_NodeContent (ContentRevisionId, SnapshotId)
    INCLUDE (NodeId, RoleId, ContentMode, IsDeleted);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_NodeContent_Role
    ON dbo.KnowHowToAI_NodeContent (SnapshotId, RoleId, IsDeleted)
    INCLUDE (NodeId, ContentRevisionId, ContentMode);

CREATE TABLE dbo.KnowHowToAI_ContentDependency (
    SnapshotId                  BIGINT           NOT NULL,
    TargetNodeId                UNIQUEIDENTIFIER NOT NULL,
    TargetRoleId                NVARCHAR(50)     COLLATE Latin1_General_100_BIN2 NOT NULL,
    SourceNodeId                UNIQUEIDENTIFIER NOT NULL,
    SourceRoleId                NVARCHAR(50)     COLLATE Latin1_General_100_BIN2 NOT NULL,
    SourceContentRevisionId     UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_KnowHowToAI_ContentDependency
        PRIMARY KEY CLUSTERED (
            SnapshotId,
            TargetNodeId,
            TargetRoleId,
            SourceNodeId,
            SourceRoleId
        ),
    CONSTRAINT CK_KnowHowToAI_ContentDependency_NotSelf CHECK (
        TargetNodeId <> SourceNodeId OR TargetRoleId <> SourceRoleId
    ),
    CONSTRAINT FK_KnowHowToAI_ContentDependency_Target
        FOREIGN KEY (SnapshotId, TargetNodeId, TargetRoleId)
        REFERENCES dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId),
    CONSTRAINT FK_KnowHowToAI_ContentDependency_Source
        FOREIGN KEY (SnapshotId, SourceNodeId, SourceRoleId)
        REFERENCES dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId)
);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_ContentDependency_Source
    ON dbo.KnowHowToAI_ContentDependency (SnapshotId, SourceNodeId, SourceRoleId)
    INCLUDE (SourceContentRevisionId, TargetNodeId, TargetRoleId);

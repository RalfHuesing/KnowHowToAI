-- 0002_create_roles.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Erstellt versionierte Rollen und Role Resolution Orders.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

CREATE TABLE dbo.KnowHowToAI_Role (
    SnapshotId          BIGINT          NOT NULL,
    RoleId              NVARCHAR(50)    COLLATE Latin1_General_100_BIN2 NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Description         NVARCHAR(500)   NULL,
    IsDeleted           BIT             NOT NULL
        CONSTRAINT DF_KnowHowToAI_Role_IsDeleted DEFAULT 0,
    CONSTRAINT PK_KnowHowToAI_Role PRIMARY KEY CLUSTERED (SnapshotId, RoleId),
    CONSTRAINT CK_KnowHowToAI_Role_RoleId CHECK (
        LEN(RoleId) > 0
        AND DATALENGTH(RoleId) = DATALENGTH(LTRIM(RTRIM(RoleId)))
    ),
    CONSTRAINT CK_KnowHowToAI_Role_Name CHECK (LEN(Name) > 0),
    CONSTRAINT FK_KnowHowToAI_Role_Snapshot FOREIGN KEY (SnapshotId)
        REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_Role_RoleId
    ON dbo.KnowHowToAI_Role (RoleId, SnapshotId)
    INCLUDE (Name, IsDeleted);

CREATE TABLE dbo.KnowHowToAI_RoleResolution (
    SnapshotId          BIGINT          NOT NULL,
    RequestedRoleId     NVARCHAR(50)    COLLATE Latin1_General_100_BIN2 NOT NULL,
    CandidateRoleId     NVARCHAR(50)    COLLATE Latin1_General_100_BIN2 NOT NULL,
    Priority            INT             NOT NULL,
    CONSTRAINT PK_KnowHowToAI_RoleResolution
        PRIMARY KEY CLUSTERED (SnapshotId, RequestedRoleId, Priority),
    CONSTRAINT UQ_KnowHowToAI_RoleResolution_Candidate
        UNIQUE (SnapshotId, RequestedRoleId, CandidateRoleId),
    CONSTRAINT CK_KnowHowToAI_RoleResolution_Priority CHECK (Priority > 0),
    CONSTRAINT FK_KnowHowToAI_RoleResolution_Snapshot FOREIGN KEY (SnapshotId)
        REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId),
    CONSTRAINT FK_KnowHowToAI_RoleResolution_RequestedRole
        FOREIGN KEY (SnapshotId, RequestedRoleId)
        REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId),
    CONSTRAINT FK_KnowHowToAI_RoleResolution_CandidateRole
        FOREIGN KEY (SnapshotId, CandidateRoleId)
        REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId)
);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_RoleResolution_Candidate
    ON dbo.KnowHowToAI_RoleResolution (SnapshotId, CandidateRoleId)
    INCLUDE (RequestedRoleId, Priority);

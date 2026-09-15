-- 0002_create_roles.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Erstellt versionierte Rollen und Role Resolution Orders.

IF OBJECT_ID(N'dbo.KnowHowToAI_Role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_Role (
        SnapshotId          BIGINT          NOT NULL,
        RoleId              NVARCHAR(50)    NOT NULL,
        Name                NVARCHAR(100)   NOT NULL,
        Description         NVARCHAR(500)   NULL,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_KnowHowToAI_Role_IsDeleted DEFAULT 0,
        CONSTRAINT PK_KnowHowToAI_Role PRIMARY KEY CLUSTERED (SnapshotId, RoleId),
        CONSTRAINT FK_KnowHowToAI_Role_Snapshot FOREIGN KEY (SnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
    );
END;

IF OBJECT_ID(N'dbo.KnowHowToAI_RoleResolution', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_RoleResolution (
        SnapshotId          BIGINT          NOT NULL,
        RequestedRoleId     NVARCHAR(50)    NOT NULL,
        CandidateRoleId     NVARCHAR(50)    NOT NULL,
        Priority            INT             NOT NULL,
        CONSTRAINT PK_KnowHowToAI_RoleResolution PRIMARY KEY CLUSTERED (SnapshotId, RequestedRoleId, Priority),
        CONSTRAINT UQ_KnowHowToAI_RoleResolution_Candidate UNIQUE (SnapshotId, RequestedRoleId, CandidateRoleId),
        CONSTRAINT FK_KnowHowToAI_RoleResolution_Snapshot FOREIGN KEY (SnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId),
        CONSTRAINT FK_KnowHowToAI_RoleResolution_RequestedRole FOREIGN KEY (SnapshotId, RequestedRoleId)
            REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId),
        CONSTRAINT FK_KnowHowToAI_RoleResolution_CandidateRole FOREIGN KEY (SnapshotId, CandidateRoleId)
            REFERENCES dbo.KnowHowToAI_Role (SnapshotId, RoleId)
    );
END;

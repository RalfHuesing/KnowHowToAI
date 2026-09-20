-- 0002_create_audiences.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Erstellt versionierte Zielgruppen und Audience Resolution Orders.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

CREATE TABLE dbo.KnowHowToAI_Audience (
    SnapshotId          BIGINT          NOT NULL,
    AudienceId              NVARCHAR(50)    COLLATE Latin1_General_100_BIN2 NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Description         NVARCHAR(500)   NULL,
    IsDeleted           BIT             NOT NULL
        CONSTRAINT DF_KnowHowToAI_Audience_IsDeleted DEFAULT 0,
    CONSTRAINT PK_KnowHowToAI_Audience PRIMARY KEY CLUSTERED (SnapshotId, AudienceId),
    CONSTRAINT CK_KnowHowToAI_Audience_AudienceId CHECK (
        LEN(AudienceId) > 0
        AND DATALENGTH(AudienceId) = DATALENGTH(LTRIM(RTRIM(AudienceId)))
    ),
    CONSTRAINT CK_KnowHowToAI_Audience_Name CHECK (LEN(Name) > 0),
    CONSTRAINT FK_KnowHowToAI_Audience_Snapshot FOREIGN KEY (SnapshotId)
        REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_Audience_AudienceId
    ON dbo.KnowHowToAI_Audience (AudienceId, SnapshotId)
    INCLUDE (Name, IsDeleted);

CREATE TABLE dbo.KnowHowToAI_AudienceResolution (
    SnapshotId          BIGINT          NOT NULL,
    RequestedAudienceId     NVARCHAR(50)    COLLATE Latin1_General_100_BIN2 NOT NULL,
    CandidateAudienceId     NVARCHAR(50)    COLLATE Latin1_General_100_BIN2 NOT NULL,
    Priority            INT             NOT NULL,
    CONSTRAINT PK_KnowHowToAI_AudienceResolution
        PRIMARY KEY CLUSTERED (SnapshotId, RequestedAudienceId, Priority),
    CONSTRAINT UQ_KnowHowToAI_AudienceResolution_Candidate
        UNIQUE (SnapshotId, RequestedAudienceId, CandidateAudienceId),
    CONSTRAINT CK_KnowHowToAI_AudienceResolution_Priority CHECK (Priority > 0),
    CONSTRAINT FK_KnowHowToAI_AudienceResolution_Snapshot FOREIGN KEY (SnapshotId)
        REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId),
    CONSTRAINT FK_KnowHowToAI_AudienceResolution_RequestedAudience
        FOREIGN KEY (SnapshotId, RequestedAudienceId)
        REFERENCES dbo.KnowHowToAI_Audience (SnapshotId, AudienceId),
    CONSTRAINT FK_KnowHowToAI_AudienceResolution_CandidateAudience
        FOREIGN KEY (SnapshotId, CandidateAudienceId)
        REFERENCES dbo.KnowHowToAI_Audience (SnapshotId, AudienceId)
);

CREATE NONCLUSTERED INDEX IX_KnowHowToAI_AudienceResolution_Candidate
    ON dbo.KnowHowToAI_AudienceResolution (SnapshotId, CandidateAudienceId)
    INCLUDE (RequestedAudienceId, Priority);

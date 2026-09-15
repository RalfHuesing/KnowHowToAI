-- 0004_seed_initial_state.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Initialisiert den leeren committed Snapshot, SystemState und die Rolle 'Default'.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @InitialSnapshot TABLE (
    SnapshotId BIGINT NOT NULL
);

DECLARE @SeededAtUtc DATETIME2(7) = SYSUTCDATETIME();

INSERT INTO dbo.KnowHowToAI_Snapshot (
    BaseSnapshotId,
    State,
    CreatedAtUtc,
    CommittedAtUtc
)
OUTPUT INSERTED.SnapshotId INTO @InitialSnapshot (SnapshotId)
VALUES (
    NULL,
    'Committed',
    @SeededAtUtc,
    @SeededAtUtc
);

DECLARE @InitialSnapshotId BIGINT = (
    SELECT SnapshotId
    FROM @InitialSnapshot
);

INSERT INTO dbo.KnowHowToAI_SystemState (
    Id,
    CurrentSnapshotId,
    LastUpdatedUtc
)
VALUES (
    1,
    @InitialSnapshotId,
    @SeededAtUtc
);

INSERT INTO dbo.KnowHowToAI_Role (
    SnapshotId,
    RoleId,
    Name,
    Description,
    IsDeleted
)
VALUES (
    @InitialSnapshotId,
    N'Default',
    N'Default',
    N'Allgemeine, rollenunabhängige Standardinhalte',
    0
);

INSERT INTO dbo.KnowHowToAI_RoleResolution (
    SnapshotId,
    RequestedRoleId,
    CandidateRoleId,
    Priority
)
VALUES (
    @InitialSnapshotId,
    N'Default',
    N'Default',
    1
);

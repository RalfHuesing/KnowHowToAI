-- 0001_create_system_and_snapshots.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Erstellt Snapshots, Systemstatus, Transaktionen und Releases.

IF OBJECT_ID(N'dbo.KnowHowToAI_Snapshot', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_Snapshot (
        SnapshotId          BIGINT          IDENTITY(1, 1) NOT NULL,
        BaseSnapshotId      BIGINT          NULL,
        State               VARCHAR(20)     NOT NULL,
        CreatedAtUtc        DATETIME2(7)    NOT NULL CONSTRAINT DF_KnowHowToAI_Snapshot_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CommittedAtUtc      DATETIME2(7)    NULL,
        CONSTRAINT PK_KnowHowToAI_Snapshot PRIMARY KEY CLUSTERED (SnapshotId),
        CONSTRAINT CK_KnowHowToAI_Snapshot_State CHECK (State IN ('Working', 'Committed', 'Discarded')),
        CONSTRAINT FK_KnowHowToAI_Snapshot_BaseSnapshot FOREIGN KEY (BaseSnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
    );
END;

IF OBJECT_ID(N'dbo.KnowHowToAI_SystemState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_SystemState (
        Id                  INT             NOT NULL CONSTRAINT DF_KnowHowToAI_SystemState_Id DEFAULT 1,
        CurrentSnapshotId   BIGINT          NOT NULL,
        LastUpdatedUtc      DATETIME2(7)    NOT NULL CONSTRAINT DF_KnowHowToAI_SystemState_LastUpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_KnowHowToAI_SystemState PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_KnowHowToAI_SystemState_Singleton CHECK (Id = 1),
        CONSTRAINT FK_KnowHowToAI_SystemState_Snapshot FOREIGN KEY (CurrentSnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
    );
END;

IF OBJECT_ID(N'dbo.KnowHowToAI_Transaction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_Transaction (
        TransactionId       UNIQUEIDENTIFIER NOT NULL,
        BaseSnapshotId      BIGINT          NOT NULL,
        WorkingSnapshotId   BIGINT          NOT NULL,
        State               VARCHAR(20)     NOT NULL,
        CreatedAtUtc        DATETIME2(7)    NOT NULL CONSTRAINT DF_KnowHowToAI_Transaction_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CommittedAtUtc      DATETIME2(7)    NULL,
        Purpose             NVARCHAR(500)   NULL,
        Actor               NVARCHAR(200)   NULL,
        Client              NVARCHAR(200)   NULL,
        CommitMessage       NVARCHAR(1000)  NULL,
        CONSTRAINT PK_KnowHowToAI_Transaction PRIMARY KEY CLUSTERED (TransactionId),
        CONSTRAINT CK_KnowHowToAI_Transaction_State CHECK (State IN ('Open', 'Committed', 'Discarded')),
        CONSTRAINT FK_KnowHowToAI_Transaction_BaseSnapshot FOREIGN KEY (BaseSnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId),
        CONSTRAINT FK_KnowHowToAI_Transaction_WorkingSnapshot FOREIGN KEY (WorkingSnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
    );
END;

IF OBJECT_ID(N'dbo.KnowHowToAI_Release', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_Release (
        ReleaseId           BIGINT          IDENTITY(1, 1) NOT NULL,
        SnapshotId          BIGINT          NOT NULL,
        Name                NVARCHAR(100)   NOT NULL,
        Description         NVARCHAR(500)   NULL,
        ReleasedAtUtc       DATETIME2(7)    NOT NULL CONSTRAINT DF_KnowHowToAI_Release_ReleasedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_KnowHowToAI_Release PRIMARY KEY CLUSTERED (ReleaseId),
        CONSTRAINT UQ_KnowHowToAI_Release_Name UNIQUE (Name),
        CONSTRAINT FK_KnowHowToAI_Release_Snapshot FOREIGN KEY (SnapshotId)
            REFERENCES dbo.KnowHowToAI_Snapshot (SnapshotId)
    );
END;

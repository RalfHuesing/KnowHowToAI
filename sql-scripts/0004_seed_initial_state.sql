-- 0004_seed_initial_state.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Initialisiert den initialen Committed Snapshot (ID 1), SystemState und Basisrolle 'Default'.

-- 1. Initialer Snapshot (ID 1)
IF NOT EXISTS (SELECT 1 FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = 1)
BEGIN
    SET IDENTITY_INSERT dbo.KnowHowToAI_Snapshot ON;

    INSERT INTO dbo.KnowHowToAI_Snapshot (SnapshotId, BaseSnapshotId, State, CreatedAtUtc, CommittedAtUtc)
    VALUES (1, NULL, 'Committed', SYSUTCDATETIME(), SYSUTCDATETIME());

    SET IDENTITY_INSERT dbo.KnowHowToAI_Snapshot OFF;
END;

-- 2. Systemstatus auf Snapshot 1
IF NOT EXISTS (SELECT 1 FROM dbo.KnowHowToAI_SystemState WHERE Id = 1)
BEGIN
    INSERT INTO dbo.KnowHowToAI_SystemState (Id, CurrentSnapshotId, LastUpdatedUtc)
    VALUES (1, 1, SYSUTCDATETIME());
END;

-- 3. Standard-Rolle 'Default' in Snapshot 1
IF NOT EXISTS (SELECT 1 FROM dbo.KnowHowToAI_Role WHERE SnapshotId = 1 AND RoleId = N'Default')
BEGIN
    INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, RoleId, Name, Description, IsDeleted)
    VALUES (1, N'Default', N'Default', N'Allgemeine, rollenunabhängige Standardinhalte', 0);
END;

-- 4. Role Resolution Order für 'Default' (Default -> Default, Priority 1)
IF NOT EXISTS (SELECT 1 FROM dbo.KnowHowToAI_RoleResolution WHERE SnapshotId = 1 AND RequestedRoleId = N'Default' AND CandidateRoleId = N'Default')
BEGIN
    INSERT INTO dbo.KnowHowToAI_RoleResolution (SnapshotId, RequestedRoleId, CandidateRoleId, Priority)
    VALUES (1, N'Default', N'Default', 1);
END;

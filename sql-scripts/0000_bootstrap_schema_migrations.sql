-- 0000_bootstrap_schema_migrations.sql
-- Ziel: Microsoft SQL Server >= 2019
-- Reentrantes Bootstrap-Skript für das Migrationsjournal.
-- Dieses Skript wird vor dem eigentlichen, versionierten Migrationslauf ausgeführt
-- und selbst nicht im Journal erfasst.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

IF OBJECT_ID(N'dbo.KnowHowToAI_SchemaMigration', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowHowToAI_SchemaMigration (
        Version             INT             NOT NULL,
        Name                NVARCHAR(260)   NOT NULL,
        ChecksumSha256      BINARY(32)      NOT NULL,
        AppliedAtUtc        DATETIME2(7)    NOT NULL
            CONSTRAINT DF_KnowHowToAI_SchemaMigration_AppliedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_KnowHowToAI_SchemaMigration PRIMARY KEY CLUSTERED (Version),
        CONSTRAINT UQ_KnowHowToAI_SchemaMigration_Name UNIQUE (Name),
        CONSTRAINT CK_KnowHowToAI_SchemaMigration_Version CHECK (Version > 0),
        CONSTRAINT CK_KnowHowToAI_SchemaMigration_Name CHECK (
            LEN(Name) > 0
            AND DATALENGTH(Name) = DATALENGTH(LTRIM(RTRIM(Name)))
        )
    );
END;

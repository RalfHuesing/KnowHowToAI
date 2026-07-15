IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{{DocumentsTableName}}' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.{{DocumentsTableName}} (
        slug        NVARCHAR(450)   NOT NULL,
        parent_slug NVARCHAR(450)   NULL,
        title       NVARCHAR(400)   NOT NULL,
        content     NVARCHAR(MAX)   NOT NULL,
        tags        NVARCHAR(MAX)   NULL,
        synonyms    NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_{{DocumentsTableName}} PRIMARY KEY (slug),
        CONSTRAINT FK_{{DocumentsTableName}}_parent
            FOREIGN KEY (parent_slug) REFERENCES dbo.{{DocumentsTableName}}(slug)
    );

    CREATE INDEX IX_{{DocumentsTableName}}_parent_slug ON dbo.{{DocumentsTableName}}(parent_slug);
END

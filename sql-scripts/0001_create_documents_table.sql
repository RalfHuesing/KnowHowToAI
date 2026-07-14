IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'documents' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.documents (
        slug        NVARCHAR(450)   NOT NULL,
        parent_slug NVARCHAR(450)   NULL,
        title       NVARCHAR(400)   NOT NULL,
        content     NVARCHAR(MAX)   NOT NULL,
        tags        NVARCHAR(MAX)   NULL,
        synonyms    NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_documents PRIMARY KEY (slug),
        CONSTRAINT FK_documents_parent
            FOREIGN KEY (parent_slug) REFERENCES dbo.documents(slug)
    );

    CREATE INDEX IX_documents_parent_slug ON dbo.documents(parent_slug);
END

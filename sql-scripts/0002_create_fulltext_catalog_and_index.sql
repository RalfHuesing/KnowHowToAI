-- Voraussetzung: Full-Text-Feature muss auf der SQL-Server-Instanz installiert sein.
-- Siehe docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 1.

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'KnowHowToAiCatalog')
BEGIN
    CREATE FULLTEXT CATALOG KnowHowToAiCatalog AS DEFAULT;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes fi
    JOIN sys.tables t ON t.object_id = fi.object_id
    WHERE t.name = 'documents'
)
BEGIN
    CREATE FULLTEXT INDEX ON dbo.documents(title, content, tags, synonyms)
        KEY INDEX PK_documents
        ON KnowHowToAiCatalog
        WITH CHANGE_TRACKING AUTO;
END

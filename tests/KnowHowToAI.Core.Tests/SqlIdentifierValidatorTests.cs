using KnowHowToAI.Core.Sync;

namespace KnowHowToAI.Core.Tests;

public class SqlIdentifierValidatorTests
{
    [Theory]
    [InlineData("documents")]
    [InlineData("sage100_documents")]
    [InlineData("_hr")]
    [InlineData("A")]
    public void EnsureValid_AcceptsValidIdentifiers(string tableName) =>
        SqlIdentifierValidator.EnsureValid(tableName);

    [Theory]
    [InlineData("")]
    [InlineData("1documents")]
    [InlineData("documents; DROP TABLE dbo.documents --")]
    [InlineData("documents-table")]
    [InlineData("documents table")]
    [InlineData("dbo.documents")]
    public void EnsureValid_RejectsInvalidIdentifiers(string tableName) =>
        Assert.Throws<ArgumentException>(() => SqlIdentifierValidator.EnsureValid(tableName));
}

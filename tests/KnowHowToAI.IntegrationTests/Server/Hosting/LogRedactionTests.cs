using KnowHowToAI.Server.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Belegt, dass Geheimnisse und vollständige Content-Payloads aus strukturierten
/// Protokoll-Events redigiert werden, bevor ein Sink schreibt.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LogRedactionTests
{
    [Fact]
    public void SecretProperties_AreRedacted()
    {
        const string password = "correct-horse-battery-staple";
        var collected = CollectRenderedEvents();
        var logger = CreateEnrichingLogger(collected);

        logger.Information(
            "Verbindung für {UserName} mit {Password} und {DatabaseConnectionString}",
            "alice",
            password,
            "Server=sqlserver;Password=" + password);

        var rendered = collected.Events[0].Rendered;
        Assert.Contains("alice", rendered);
        Assert.Contains(SecretRedactionEnricher.RedactedValue, rendered);
        Assert.DoesNotContain(password, rendered);
        Assert.DoesNotContain("sqlserver", rendered);
    }

    [Fact]
    public void ContentPayloadProperties_AreRedacted()
    {
        const string payload = "# Keine echten Überschriften in Logs";
        var collected = CollectRenderedEvents();
        var logger = CreateEnrichingLogger(collected);

        logger.Information("Node {NodeId} aktualisiert mit {ContentMd}", "node-1", payload);

        var rendered = collected.Events[0].Rendered;
        Assert.Contains("node-1", rendered);
        Assert.Contains(SecretRedactionEnricher.RedactedValue, rendered);
        Assert.DoesNotContain(payload, rendered);
    }

    [Fact]
    public void NonSensitiveProperties_StayVisible()
    {
        var collected = CollectRenderedEvents();
        var logger = CreateEnrichingLogger(collected);

        logger.Information("Transaktion {TransactionId} für Rolle {RoleId}", "tx-1", "role-7");

        var rendered = collected.Events[0].Rendered;
        Assert.Contains("tx-1", rendered);
        Assert.Contains("role-7", rendered);
        Assert.DoesNotContain(SecretRedactionEnricher.RedactedValue, rendered);
    }

    [Theory]
    [InlineData("Password", true)]
    [InlineData("UserPassword", true)]
    [InlineData("PasswordHash", false)]
    [InlineData("Secret", true)]
    [InlineData("ClientSecret", true)]
    [InlineData("Token", true)]
    [InlineData("ConnectionString", true)]
    [InlineData("Content", true)]
    [InlineData("ContentMd", true)]
    [InlineData("ContentLength", false)]
    [InlineData("NodeId", false)]
    public void IsSensitive_ClassifiesPropertyNames(string propertyName, bool expected)
    {
        Assert.Equal(expected, SecretRedactionEnricher.IsSensitive(propertyName));
    }

    private static Serilog.ILogger CreateEnrichingLogger(CollectingSink sink) =>
        new LoggerConfiguration()
            .Enrich.With(new SecretRedactionEnricher())
            .WriteTo.Sink(sink)
            .CreateLogger();

    private static CollectingSink CollectRenderedEvents() => new();

    private sealed class CollectingSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        public IReadOnlyList<RenderedEvent> Events =>
            [.. _events.Select(logEvent => new RenderedEvent(logEvent.RenderMessage()))];

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }

    private sealed record RenderedEvent(string Rendered);
}

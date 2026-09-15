using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Storage.SqlServer.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Führt bei aktivierter Policy ausstehende Migrationen vor der Betriebsbereitschaft aus.
/// </summary>
internal sealed class SchemaMigrationHostedService(
    ISchemaMigrator schemaMigrator,
    MigrationPolicy migrationPolicy,
    ILogger<SchemaMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!migrationPolicy.ApplyOnStartup)
        {
            logger.LogInformation("Schema-Migrationen beim Start gemäß Konfiguration übersprungen.");
            return;
        }

        try
        {
            var count = await schemaMigrator.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Schema-Migrationen vor Betriebsbereitschaft abgeschlossen ({Count} angewendet).", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Schema-Migrationen konnten vor Betriebsbereitschaft nicht abgeschlossen werden ({ExceptionType}).",
                exception.GetType().Name);
            throw new InvalidOperationException(
                "Der Server konnte wegen eines Fehlers bei der Schema-Migration nicht gestartet werden.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

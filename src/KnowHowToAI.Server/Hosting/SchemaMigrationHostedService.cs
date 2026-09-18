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
    ILogger<SchemaMigrationHostedService> logger) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken cancellationToken)
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Schema-Migrationsdienst wird gestartet.");
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Schema-Migrationsdienst wurde gestartet.");
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Schema-Migrationsdienst wird kontrolliert beendet.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Schema-Migrationsdienst wurde beendet.");
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Schema-Migrationsdienst ist beendet.");
        return Task.CompletedTask;
    }
}

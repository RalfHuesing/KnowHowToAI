namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Port für das Anwenden ausstehender Schema-Migrationen.
/// </summary>
public interface ISchemaMigrator
{
    /// <summary>
    /// Wendet alle ausstehenden Migrationen an und gibt die Anzahl der tatsächlich
    /// angewandten Skripte zurück. Parallele Runner werden serialisiert.
    /// Wirft bei Fehler; kein Result&lt;T&gt;, da Migration ein exogener IO-Vorgang ist.
    /// </summary>
    Task<int> MigrateAsync(CancellationToken cancellationToken = default);
}

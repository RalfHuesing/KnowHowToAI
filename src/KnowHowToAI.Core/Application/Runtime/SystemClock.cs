using KnowHowToAI.Core.Application.Abstractions.Runtime;

namespace KnowHowToAI.Core.Application.Runtime;

/// <summary>
/// Liefert die aktuelle UTC-Systemzeit. Produktive Implementierung des Ports;
/// Tests verwenden deterministische Fakes.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

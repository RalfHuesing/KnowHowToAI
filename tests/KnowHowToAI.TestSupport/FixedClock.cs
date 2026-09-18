using KnowHowToAI.Core.Application.Abstractions.Runtime;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// <see cref="IClock"/> mit fest eingefrorener Zeit für deterministische Tests.
/// </summary>
public sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    /// <summary>Die eingefrorene Zeit.</summary>
    public DateTimeOffset UtcNow { get; } = utcNow;
}

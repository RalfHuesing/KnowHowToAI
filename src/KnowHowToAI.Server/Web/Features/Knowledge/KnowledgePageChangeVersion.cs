using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>Behält bei konkurrierenden Route-Auflösungen die neueste Draft-Version.</summary>
internal static class KnowledgePageChangeVersion
{
    public static long? Resolve(
        ReadContext resolvedContext,
        TransactionId? activeTransactionId,
        long? currentChangeVersion,
        long? resolvedChangeVersion)
    {
        ArgumentNullException.ThrowIfNull(resolvedContext);

        return resolvedContext.TransactionId is { } resolvedTransactionId
            && resolvedTransactionId == activeTransactionId
            && currentChangeVersion is { } currentVersion
            && resolvedChangeVersion is { } resolvedVersion
            && resolvedVersion < currentVersion
                ? currentVersion
                : resolvedChangeVersion;
    }
}

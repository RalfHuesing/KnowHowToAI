using KnowHowToAI.Core.Documents;

namespace KnowHowToAI.Core.Logging;

public static class ResponseSize
{
    public static int Measure<T>(T response) => response switch
    {
        IReadOnlyCollection<DocumentSummary> summaries => summaries.Count,
        DocumentDetail detail => detail.Content?.Length ?? 0,
        null => 0,
        _ => 0,
    };
}

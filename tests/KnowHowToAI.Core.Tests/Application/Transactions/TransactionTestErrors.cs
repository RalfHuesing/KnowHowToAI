using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.Transactions;

internal static class TransactionTestErrors
{
    public static DomainError NotFound(TransactionId transactionId) =>
        new(
            TransactionValidationErrorCodes.TransactionNotFound,
            $"Die Transaction '{transactionId.Value}' existiert nicht.",
            new Dictionary<string, string> { [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString() });

    public static DomainError Closed(TransactionId transactionId) =>
        new(
            TransactionValidationErrorCodes.TransactionClosed,
            $"Die Transaction '{transactionId.Value}' ist nicht offen.",
            new Dictionary<string, string> { [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString() });
}

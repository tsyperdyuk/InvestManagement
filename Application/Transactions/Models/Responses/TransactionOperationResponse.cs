using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Transactions.Models.Responses;

public sealed record TransactionOperationResponse(
    Guid OperationId,
    TransactionOperationStatus Status,
    Guid? TransactionId = null,
    string? FailureReason = null);

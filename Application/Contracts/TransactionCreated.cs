namespace InvestManagement.Application.Contracts;

public sealed record TransactionCreated(
    Guid EventId,
    Guid TransactionId,
    Guid PortfolioId,
    Guid UserId,
    string Type,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc);

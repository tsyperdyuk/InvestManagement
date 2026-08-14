using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Transactions.Models.Responses;

public sealed record TransactionResponse(
    Guid Id,
    Guid PortfolioId,
    Guid? SecurityId,
    TransactionType Type,
    decimal Amount,
    decimal? Quantity,
    decimal? Price,
    DateTime CreatedAt);
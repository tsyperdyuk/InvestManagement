namespace InvestManagement.Application.Transactions.Models.Requests;

public sealed class DepositTransactionRequest
{
    public Guid PortfolioId { get; init; }

    public decimal Amount { get; init; }
}
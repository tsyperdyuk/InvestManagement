namespace InvestManagement.Application.Transactions.Models.Requests;

public sealed class WithdrawalTransactionRequest
{
    public Guid PortfolioId { get; init; }

    public decimal Amount { get; init; }
}

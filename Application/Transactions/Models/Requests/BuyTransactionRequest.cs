namespace InvestManagement.Application.Transactions.Models.Requests;

public sealed class BuyTransactionRequest
{
    public Guid PortfolioId { get; init; }

    public Guid SecurityId { get; init; }

    public decimal Quantity { get; init; }
}
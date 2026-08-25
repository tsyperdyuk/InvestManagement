namespace InvestManagement.Application.Securities.Models;

public sealed class SecuritySummary
{
    public Guid SecurityId { get; init; }

    public string Ticker { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal CostBasis { get; init; }

    public decimal AveragePrice { get; init; }

    public decimal CurrentPrice { get; init; }

    public decimal MarketValue { get; init; }
}
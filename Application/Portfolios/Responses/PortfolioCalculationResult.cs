using InvestManagement.Application.Securities.Models;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Responses;

public class PortfolioCalculationResult
{
    public Guid PortfolioId { get; init; }

    public string PortfolioName { get; init; } = string.Empty;

    public PortfolioCurrency Currency { get; init; }

    public decimal CashBalance { get; init; }

    public decimal TotalMarketValue { get; init; }

    public decimal TotalPortfolioValue { get; init; }

    public decimal TotalCostBasis { get; init; }

    public IReadOnlyCollection<SecuritySummary> Securities { get; init; } = [];
}

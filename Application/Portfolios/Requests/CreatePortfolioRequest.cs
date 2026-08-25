using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Requests;

public record CreatePortfolioRequest
{
    public required string Name { get; init; }

    public PortfolioCurrency Currency { get; init; } = PortfolioCurrency.USD;
}

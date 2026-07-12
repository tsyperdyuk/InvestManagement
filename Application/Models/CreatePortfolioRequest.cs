using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Models;

public record CreatePortfolioRequest
{
    public required Guid UserId { get; init; }

    public required string Name { get; init; }

    public PortfolioCurrency Currency { get; init; } = PortfolioCurrency.USD;
}

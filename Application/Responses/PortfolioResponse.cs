using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Responses;

public class PortfolioResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public PortfolioCurrency Currency { get; init; }
}

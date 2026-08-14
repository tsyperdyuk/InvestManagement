using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Models;

public class PortfolioModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public PortfolioCurrency Currency { get; init; }
}

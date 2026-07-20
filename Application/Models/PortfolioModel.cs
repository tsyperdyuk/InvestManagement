using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Models;

public class PortfolioModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public PortfolioCurrency Currency { get; init; }
}

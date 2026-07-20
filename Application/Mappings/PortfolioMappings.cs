using InvestManagement.Application.Models;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Mappings;

public static class PortfolioMappings
{
    public static PortfolioModel ToResponse(this Portfolio portfolio)
    {
        return new PortfolioModel
        {
            Id = portfolio.Id,
            Name = portfolio.Name,
            Currency = portfolio.Currency
        };
    }
}

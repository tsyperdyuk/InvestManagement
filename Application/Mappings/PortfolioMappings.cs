using InvestManagement.Application.Responses;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Mappings;

public static class PortfolioMappings
{
    public static PortfolioResponse ToResponse(this Portfolio portfolio)
    {
        return new PortfolioResponse
        {
            Id = portfolio.Id,
            Name = portfolio.Name,
            Currency = portfolio.Currency
        };
    }
}

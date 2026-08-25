using InvestManagement.Application.Portfolios.Models;

namespace InvestManagement.Application.Portfolios.Interfaces;

public interface IPortfolioSummaryService
{
    Task<PortfolioSummary?> GetDetailsAsync(Guid portfolioId, CancellationToken cancellationToken);

    Task<PortfolioSummary?> GetSummaryAsync(Guid portfolioId, CancellationToken cancellationToken);
}

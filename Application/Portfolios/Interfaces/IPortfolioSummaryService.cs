using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Interfaces;

public interface IPortfolioSummaryService
{
    Task<PortfolioSummary?> GetDetailsAsync(Guid portfolioId, CancellationToken cancellationToken);    
    Task<PortfolioSummary?> GetSummaryAsync(Guid portfolioId, CancellationToken cancellationToken);    
}

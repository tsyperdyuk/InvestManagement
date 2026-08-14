using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Interfaces;

public interface IPortfolioRepository
{
    Task<Portfolio> AddAsync(Portfolio portfolio, CancellationToken cancellationToken);

    Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Portfolio>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task UpdateAsync(Portfolio portfolio,  CancellationToken cancellationToken);

    Task DeleteAsync(Portfolio portfolio, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<Portfolio?> GetSummaryDataAsync(Guid portfolioId, CancellationToken cancellationToken);
}
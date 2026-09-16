using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Interfaces;

public interface IPortfolioRepository
{
    Task<Portfolio> AddAsync(Portfolio portfolio, CancellationToken cancellationToken);

    Task<Portfolio?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Portfolio>> GetPagedAsync(int pageNumber, int pageSize, Guid userId, CancellationToken cancellationToken);

    Task UpdateAsync(Portfolio portfolio,  CancellationToken cancellationToken);

    Task DeleteAsync(Portfolio portfolio, CancellationToken cancellationToken);

    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken);

    Task<Portfolio?> GetSummaryDataAsync(Guid portfolioId, Guid userId, CancellationToken cancellationToken);
}
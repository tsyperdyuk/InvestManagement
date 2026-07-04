using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Interfaces;

public interface IPortfolioRepository
{
    Task<Portfolio> AddAsync(Portfolio portfolio, CancellationToken cancellationToken);

    Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Portfolio>> GetAllAsync(CancellationToken cancellationToken);

    Task UpdateAsync(Portfolio portfolio,  CancellationToken cancellationToken);

    Task DeleteAsync(Portfolio portfolio, CancellationToken cancellationToken);
}
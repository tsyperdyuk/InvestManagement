using InvestManagement.Domain.Model;
using InvestManagement.Application.Models;

namespace InvestManagement.Application.Interfaces;

public interface IPortfolioService
{
    Task<Portfolio> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<Portfolio>> GetAllAsync(Guid userId, CancellationToken cancellationToken);

    Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

using InvestManagement.Models;

namespace InvestManagement.Interfaces;

public interface IPortfolioService
{
    Task<Portfolio> CreateAsync(CreatePortfolioRequest request);

    Task<IReadOnlyList<Portfolio>> GetAllAsync(Guid userId);

    Task<Portfolio?> GetByIdAsync(Guid id);

    Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request);

    Task<bool> DeleteAsync(Guid id);
}

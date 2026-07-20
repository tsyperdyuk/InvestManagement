using InvestManagement.Domain.Model;
using InvestManagement.Application.Models;

namespace InvestManagement.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioModel> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken);

    Task<PagedResult<PortfolioModel>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken);

    Task<PortfolioModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

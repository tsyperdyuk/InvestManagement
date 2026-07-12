using InvestManagement.Domain.Model;
using InvestManagement.Application.Models;
using InvestManagement.Application.Responses;

namespace InvestManagement.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioResponse> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<PortfolioResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<PortfolioResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

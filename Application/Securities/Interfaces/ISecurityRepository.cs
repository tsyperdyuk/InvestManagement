using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Securities.Interfaces;

public interface ISecurityRepository
{
    Task<Security?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

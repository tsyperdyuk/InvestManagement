using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Interfaces;

public interface IScenarioRepository
{
    Task AddAsync(Scenario scenario, CancellationToken cancellationToken);

    Task AddOperationAsync(ScenarioOperation operation, CancellationToken cancellationToken);

    Task<Scenario?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Scenario>> GetByPortfolioIdAsync(Guid portfolioId, Guid userId, CancellationToken cancellationToken);

    void Delete(Scenario scenario);
}

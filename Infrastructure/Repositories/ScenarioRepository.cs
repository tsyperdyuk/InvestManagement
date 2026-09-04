using InvestManagement.Application.Scenarios.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Infrastructure.Repositories;

public sealed class ScenarioRepository : IScenarioRepository
{
    private readonly InvestManagementDbContext _dbContext;

    public ScenarioRepository(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Scenario scenario,
        CancellationToken cancellationToken)
    {
        await _dbContext.Scenarios.AddAsync(scenario, cancellationToken);
    }

    public async Task AddOperationAsync(
        ScenarioOperation operation,
        CancellationToken cancellationToken)
    {
        await _dbContext.ScenarioOperations.AddAsync(
            operation,
            cancellationToken);
    }

    public Task<Scenario?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Scenarios
            .Include(scenario => scenario.Operations)
            .SingleOrDefaultAsync(
                scenario => scenario.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Scenario>> GetByPortfolioIdAsync(
        Guid portfolioId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Scenarios
            .AsNoTracking()
            .Include(scenario => scenario.Operations)
            .Where(scenario => scenario.PortfolioId == portfolioId)
            .OrderByDescending(scenario => scenario.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Delete(Scenario scenario)
    {
        _dbContext.Scenarios.Remove(scenario);
    }
}

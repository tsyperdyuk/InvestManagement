using InvestManagement.Application.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Application.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly InvestManagementDbContext _dbContext;

    public PortfolioRepository(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Portfolio> AddAsync(Portfolio portfolio,  CancellationToken cancellationToken)
    {
        await _dbContext.Portfolios.AddAsync(portfolio, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return portfolio;
    }

    public async Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Portfolios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Portfolio>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Portfolios
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Portfolio portfolio, CancellationToken cancellationToken)
    {
        _dbContext.Portfolios.Update(portfolio);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Portfolio portfolio, CancellationToken cancellationToken)
    {
        _dbContext.Portfolios.Remove(portfolio);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Application.Portfolios.Repositories;

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

    public async Task<IReadOnlyList<Portfolio>> GetPagedAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken)
    {
        return await _dbContext.Portfolios
            .AsNoTracking()
            .OrderBy(portfolio => portfolio.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Portfolios
            .CountAsync(cancellationToken);
    }

    public async Task<Portfolio?> GetSummaryDataAsync(Guid portfolioId,  CancellationToken cancellationToken)
    {
        return await _dbContext.Portfolios
            .AsNoTracking()
            .Include(x => x.Transactions)
                .ThenInclude(x => x.Security)
            .SingleOrDefaultAsync(
                x => x.Id == portfolioId,
                cancellationToken);
    }
}

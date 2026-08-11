using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Application.Transactions.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly InvestManagementDbContext _dbContext;

    public TransactionRepository(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Transaction>> GetByPortfolioIdAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        return await _dbContext.Transactions
            .Where(x => x.PortfolioId == portfolioId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }
}
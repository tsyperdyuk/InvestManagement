using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Infrastructure.Repositories;

public sealed class TransactionOperationRepository : ITransactionOperationRepository
{
    private readonly InvestManagementDbContext _dbContext;

    public TransactionOperationRepository(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(TransactionOperation operation)
    {
        _dbContext.TransactionOperations.Add(operation);
    }

    public Task<TransactionOperation?> GetByIdAsync(
        Guid operationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TransactionOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                operation => operation.Id == operationId && operation.UserId == userId,
                cancellationToken);
    }
}

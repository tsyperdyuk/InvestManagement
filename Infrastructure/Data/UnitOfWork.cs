using InvestManagement.Application.Common.Interfaces;

namespace InvestManagement.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly InvestManagementDbContext _dbContext;

    public UnitOfWork(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

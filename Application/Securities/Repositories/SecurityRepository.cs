using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Application.Securities.Repositories;

public sealed class SecurityRepository : ISecurityRepository
{
    private readonly InvestManagementDbContext _dbContext;

    public SecurityRepository(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Security?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Securities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

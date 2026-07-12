using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Infrastructure.Data;

public class InvestManagementDbContext : DbContext
{
    public InvestManagementDbContext(DbContextOptions<InvestManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
}

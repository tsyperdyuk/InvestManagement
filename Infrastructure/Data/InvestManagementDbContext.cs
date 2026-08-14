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

    public DbSet<Security> Securities => Set<Security>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InvestManagementDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}

using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Identity;
using InvestManagement.Infrastructure.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Infrastructure.Data;

public class InvestManagementDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public InvestManagementDbContext(DbContextOptions<InvestManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<Security> Securities => Set<Security>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Scenario> Scenarios => Set<Scenario>();

    public DbSet<ScenarioOperation> ScenarioOperations => Set<ScenarioOperation>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<TransactionOperation> TransactionOperations => Set<TransactionOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InvestManagementDbContext).Assembly);
    }
}

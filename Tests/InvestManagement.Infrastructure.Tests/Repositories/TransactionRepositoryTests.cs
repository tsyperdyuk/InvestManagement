using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Repositories;
using Xunit;

namespace InvestManagement.Infrastructure.Tests.Repositories;

public sealed class TransactionRepositoryTests
{
    [Fact]
    public async Task GetByPortfolioIdAsync_ReturnsOnlyPortfolioTransactionsInOrder()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var portfolio = new Portfolio("Main", PortfolioCurrency.GBP);
        var otherPortfolio = new Portfolio("Other", PortfolioCurrency.GBP);
        var later = Transaction.CreateDeposit(
            portfolio.Id, 200m, DateTime.UtcNow);
        var earlier = Transaction.CreateDeposit(
            portfolio.Id, 100m, DateTime.UtcNow.AddMinutes(-1));
        var unrelated = Transaction.CreateDeposit(
            otherPortfolio.Id, 300m, DateTime.UtcNow);
        database.DbContext.Portfolios.AddRange(portfolio, otherPortfolio);
        database.DbContext.Transactions.AddRange(later, earlier, unrelated);
        await database.DbContext.SaveChangesAsync();
        var repository = new TransactionRepository(database.DbContext);

        var result = await repository.GetByPortfolioIdAsync(
            portfolio.Id, CancellationToken.None);

        Assert.Equal([earlier.Id, later.Id], result.Select(x => x.Id));
    }

    [Fact]
    public async Task AddAsync_WithUnitOfWork_PersistsTransaction()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var portfolio = new Portfolio("Main", PortfolioCurrency.GBP);
        database.DbContext.Portfolios.Add(portfolio);
        await database.DbContext.SaveChangesAsync();
        var repository = new TransactionRepository(database.DbContext);
        var unitOfWork = new UnitOfWork(database.DbContext);
        var transaction = Transaction.CreateDeposit(
            portfolio.Id, 500m, DateTime.UtcNow);

        await repository.AddAsync(transaction, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(transaction.Id, database.DbContext.Transactions.Single().Id);
    }
}

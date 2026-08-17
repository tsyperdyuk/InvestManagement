using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvestManagement.Infrastructure.Tests.Repositories;

public sealed class PortfolioRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsPortfolio()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var repository = new PortfolioRepository(database.DbContext);
        var portfolio = new Portfolio("Retirement", PortfolioCurrency.GBP);

        await repository.AddAsync(portfolio, CancellationToken.None);
        database.DbContext.ChangeTracker.Clear();

        var persisted = await database.DbContext.Portfolios.SingleAsync();
        Assert.Equal(portfolio.Id, persisted.Id);
        Assert.Equal("Retirement", persisted.Name);
        Assert.Equal(PortfolioCurrency.GBP, persisted.Currency);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPage()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var portfolios = Enumerable.Range(1, 5)
            .Select(index => new Portfolio($"Portfolio {index}", PortfolioCurrency.GBP))
            .OrderBy(x => x.Id)
            .ToList();
        database.DbContext.Portfolios.AddRange(portfolios);
        await database.DbContext.SaveChangesAsync();
        var repository = new PortfolioRepository(database.DbContext);

        var result = await repository.GetPagedAsync(2, 2, CancellationToken.None);

        Assert.Equal(portfolios.Skip(2).Take(2).Select(x => x.Id), result.Select(x => x.Id));
        Assert.Equal(5, await repository.CountAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetSummaryDataAsync_LoadsTransactionsAndSecurity()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var portfolio = new Portfolio("Main", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME", "Acme Corporation", SecurityType.Stock, PortfolioCurrency.GBP);
        var transaction = Transaction.CreateBuy(
            portfolio.Id, security.Id, 2m, 100m, DateTime.UtcNow);
        database.DbContext.AddRange(portfolio, security, transaction);
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
        var repository = new PortfolioRepository(database.DbContext);

        var result = await repository.GetSummaryDataAsync(
            portfolio.Id, CancellationToken.None);

        Assert.NotNull(result);
        var loadedTransaction = Assert.Single(result.Transactions);
        Assert.Equal(security.Id, loadedTransaction.Security?.Id);
    }
}

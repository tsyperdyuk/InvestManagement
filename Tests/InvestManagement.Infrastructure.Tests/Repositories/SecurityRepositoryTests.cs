using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Repositories;
using Xunit;

namespace InvestManagement.Infrastructure.Tests.Repositories;

public sealed class SecurityRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_WhenSecurityExists_ReturnsSecurity()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var security = new Security(
            "ACME", "Acme Corporation", SecurityType.Stock, PortfolioCurrency.GBP);
        security.UpdatePrice(125.50m);
        database.DbContext.Securities.Add(security);
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
        var repository = new SecurityRepository(database.DbContext);

        var result = await repository.GetByIdAsync(
            security.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ACME", result.Ticker);
        Assert.Equal(125.50m, result.CurrentPrice);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSecurities()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        database.DbContext.Securities.AddRange(
            new Security("ACME", "Acme", SecurityType.Stock, PortfolioCurrency.GBP),
            new Security("MSFT", "Microsoft", SecurityType.Stock, PortfolioCurrency.USD));
        await database.DbContext.SaveChangesAsync();
        var repository = new SecurityRepository(database.DbContext);

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}

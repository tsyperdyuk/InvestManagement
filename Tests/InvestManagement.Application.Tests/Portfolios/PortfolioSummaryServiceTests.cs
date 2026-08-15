using InvestManagement.Application.Portfolios.Services;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvestManagement.Application.Tests.Portfolios;

public sealed class PortfolioSummaryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_WithDeposit_ReturnsDepositedAmountAsCashBalance()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            1_000m,
            DateTime.UtcNow);

        dbContext.Portfolios.Add(portfolio);
        dbContext.Transactions.Add(deposit);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioRepository(dbContext);
        var service = new PortfolioSummaryService(repository);

        var summary = await service.GetSummaryAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(1_000m, summary.CashBalance);
        Assert.Equal(0m, summary.TotalMarketValue);
        Assert.Equal(1_000m, summary.TotalPortfolioValue);
        Assert.Empty(summary.Securities);
    }

    [Fact]
    public async Task GetSummaryAsync_WithWithdrawal_SubtractsAmountFromCashBalance()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            1_000m,
            DateTime.UtcNow.AddMinutes(-1));
        var withdrawal = Transaction.CreateWithdrawal(
            portfolio.Id,
            250m,
            DateTime.UtcNow);

        dbContext.Portfolios.Add(portfolio);
        dbContext.Transactions.AddRange(deposit, withdrawal);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioRepository(dbContext);
        var service = new PortfolioSummaryService(repository);

        var summary = await service.GetSummaryAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(750m, summary.CashBalance);
        Assert.Equal(0m, summary.TotalMarketValue);
        Assert.Equal(750m, summary.TotalPortfolioValue);
        Assert.Empty(summary.Securities);
    }

    [Fact]
    public async Task GetSummaryAsync_WithBuy_CalculatesPositionAndPortfolioValues()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(120m);

        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            2_000m,
            DateTime.UtcNow.AddMinutes(-1));
        var buy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            10m,
            100m,
            DateTime.UtcNow);

        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.AddRange(deposit, buy);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioRepository(dbContext);
        var service = new PortfolioSummaryService(repository);

        var summary = await service.GetDetailsAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(1_000m, summary.CashBalance);
        Assert.Equal(1_200m, summary.TotalMarketValue);
        Assert.Equal(2_200m, summary.TotalPortfolioValue);
        Assert.Equal(1_000m, summary.TotalCostBasis);

        var position = Assert.Single(summary.Securities);
        Assert.Equal(security.Id, position.SecurityId);
        Assert.Equal("ACME", position.Ticker);
        Assert.Equal(10m, position.Quantity);
        Assert.Equal(1_000m, position.CostBasis);
        Assert.Equal(100m, position.AveragePrice);
        Assert.Equal(120m, position.CurrentPrice);
        Assert.Equal(1_200m, position.MarketValue);
    }

    [Fact]
    public async Task GetSummaryAsync_WithPartialSell_ReducesQuantityAndCostBasis()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(120m);

        var transactionTime = DateTime.UtcNow;
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            2_000m,
            transactionTime.AddMinutes(-2));
        var buy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            10m,
            100m,
            transactionTime.AddMinutes(-1));
        var sell = Transaction.CreateSell(
            portfolio.Id,
            security.Id,
            4m,
            130m,
            transactionTime);

        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.AddRange(deposit, buy, sell);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioRepository(dbContext);
        var service = new PortfolioSummaryService(repository);

        var summary = await service.GetDetailsAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(1_520m, summary.CashBalance);
        Assert.Equal(720m, summary.TotalMarketValue);
        Assert.Equal(2_240m, summary.TotalPortfolioValue);
        Assert.Equal(600m, summary.TotalCostBasis);

        var position = Assert.Single(summary.Securities);
        Assert.Equal(6m, position.Quantity);
        Assert.Equal(600m, position.CostBasis);
        Assert.Equal(100m, position.AveragePrice);
        Assert.Equal(120m, position.CurrentPrice);
        Assert.Equal(720m, position.MarketValue);
    }

    [Fact]
    public async Task GetSummaryAsync_WithBuysAtDifferentPrices_CalculatesWeightedAveragePrice()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(150m);

        var transactionTime = DateTime.UtcNow;
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            3_000m,
            transactionTime.AddMinutes(-3));
        var firstBuy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            10m,
            100m,
            transactionTime.AddMinutes(-2));
        var secondBuy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            5m,
            160m,
            transactionTime.AddMinutes(-1));

        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.AddRange(deposit, firstBuy, secondBuy);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioRepository(dbContext);
        var service = new PortfolioSummaryService(repository);

        var summary = await service.GetDetailsAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(1_200m, summary.CashBalance);
        Assert.Equal(2_250m, summary.TotalMarketValue);
        Assert.Equal(3_450m, summary.TotalPortfolioValue);
        Assert.Equal(1_800m, summary.TotalCostBasis);

        var position = Assert.Single(summary.Securities);
        Assert.Equal(15m, position.Quantity);
        Assert.Equal(1_800m, position.CostBasis);
        Assert.Equal(120m, position.AveragePrice);
        Assert.Equal(150m, position.CurrentPrice);
        Assert.Equal(2_250m, position.MarketValue);
    }

    [Fact]
    public async Task GetSummaryAsync_WithFullSell_RemovesClosedPosition()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(120m);

        var transactionTime = DateTime.UtcNow;
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            2_000m,
            transactionTime.AddMinutes(-3));
        var buy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            10m,
            100m,
            transactionTime.AddMinutes(-2));
        var sell = Transaction.CreateSell(
            portfolio.Id,
            security.Id,
            10m,
            130m,
            transactionTime.AddMinutes(-1));

        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.AddRange(deposit, buy, sell);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioRepository(dbContext);
        var service = new PortfolioSummaryService(repository);

        var summary = await service.GetDetailsAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(2_300m, summary.CashBalance);
        Assert.Equal(0m, summary.TotalMarketValue);
        Assert.Equal(2_300m, summary.TotalPortfolioValue);
        Assert.Equal(0m, summary.TotalCostBasis);
        Assert.Empty(summary.Securities);
    }

    private static InvestManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InvestManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InvestManagementDbContext(options);
    }
}

using InvestManagement.Application.Common.Interfaces;
using System.Reflection;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Services;
using Moq;
using InvestManagement.Domain.Model;
using Xunit;

namespace InvestManagement.Application.Tests.Portfolios;

public sealed class PortfolioSummaryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_WithDeposit_ReturnsDepositedAmountAsCashBalance()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, Guid.NewGuid());
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            1_000m,
            DateTime.UtcNow);

        var service = CreateService(portfolio, [deposit]);

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
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, Guid.NewGuid());
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            1_000m,
            DateTime.UtcNow.AddMinutes(-1));
        var withdrawal = Transaction.CreateWithdrawal(
            portfolio.Id,
            250m,
            DateTime.UtcNow);

        var service = CreateService(portfolio, [deposit, withdrawal]);

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
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, Guid.NewGuid());
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

        var service = CreateService(portfolio, [deposit, buy], [security]);

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
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, Guid.NewGuid());
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

        var service = CreateService(portfolio, [deposit, buy, sell], [security]);

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
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, Guid.NewGuid());
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

        var service = CreateService(portfolio, [deposit, firstBuy, secondBuy], [security]);

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
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, Guid.NewGuid());
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

        var service = CreateService(portfolio, [deposit, buy, sell], [security]);

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

    private static PortfolioSummaryService CreateService(
        Portfolio portfolio,
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyCollection<Security>? securities = null)
    {
        PopulateNavigationProperties(portfolio, transactions, securities ?? []);

        var repository = new Mock<IPortfolioRepository>();
        repository
            .Setup(x => x.GetSummaryDataAsync(
                portfolio.Id,
                portfolio.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        return new PortfolioSummaryService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == portfolio.UserId));
    }

    private static void PopulateNavigationProperties(
        Portfolio portfolio,
        IEnumerable<Transaction> transactions,
        IReadOnlyCollection<Security> securities)
    {
        var portfolioTransactions = (List<Transaction>)typeof(Portfolio)
            .GetField("_transactions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(portfolio)!;

        foreach (var transaction in transactions)
        {
            if (transaction.SecurityId is Guid securityId)
            {
                typeof(Transaction)
                    .GetProperty(nameof(Transaction.Security))!
                    .SetValue(transaction, securities.Single(x => x.Id == securityId));
            }

            portfolioTransactions.Add(transaction);
        }
    }
}

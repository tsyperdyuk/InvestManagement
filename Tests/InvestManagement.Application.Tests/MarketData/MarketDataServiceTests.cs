using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.MarketData.Interfaces;
using InvestManagement.Application.MarketData.Services;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Domain.Model;
using Moq;
using Xunit;

namespace InvestManagement.Application.Tests.MarketData;

public sealed class MarketDataServiceTests
{
    [Fact]
    public async Task UpdateSecurityPricesAsync_WhenPricesAreAvailable_UpdatesEverySecurityAndSavesChanges()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var apple = CreateSecurity("AAPL", 190m);
        var microsoft = CreateSecurity("MSFT", 410m);
        var securityRepository = new Mock<ISecurityRepository>();
        var marketDataProvider = new Mock<IMarketDataProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();

        securityRepository
            .Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync([apple, microsoft]);
        marketDataProvider
            .Setup(x => x.GetLatestPriceAsync("AAPL", cancellationToken))
            .ReturnsAsync(195m);
        marketDataProvider
            .Setup(x => x.GetLatestPriceAsync("MSFT", cancellationToken))
            .ReturnsAsync(420m);

        var service = new MarketDataService(
            securityRepository.Object,
            marketDataProvider.Object,
            unitOfWork.Object);

        await service.UpdateSecurityPricesAsync(cancellationToken);

        Assert.Equal(195m, apple.CurrentPrice);
        Assert.Equal(190m, apple.PreviousPrice);
        Assert.Equal(420m, microsoft.CurrentPrice);
        Assert.Equal(410m, microsoft.PreviousPrice);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateSecurityPricesAsync_WhenProviderReturnsNoPrice_KeepsExistingPriceAndStillSavesChanges()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var security = CreateSecurity("AAPL", 190m);
        var securityRepository = new Mock<ISecurityRepository>();
        var marketDataProvider = new Mock<IMarketDataProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();

        securityRepository
            .Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync([security]);
        marketDataProvider
            .Setup(x => x.GetLatestPriceAsync("AAPL", cancellationToken))
            .ReturnsAsync((decimal?)null);

        var service = new MarketDataService(
            securityRepository.Object,
            marketDataProvider.Object,
            unitOfWork.Object);

        await service.UpdateSecurityPricesAsync(cancellationToken);

        Assert.Equal(190m, security.CurrentPrice);
        Assert.Null(security.PreviousPrice);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateSecurityPricesAsync_WhenThereAreNoSecurities_DoesNotCallProviderAndSavesChanges()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var securityRepository = new Mock<ISecurityRepository>();
        var marketDataProvider = new Mock<IMarketDataProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();

        securityRepository
            .Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync([]);

        var service = new MarketDataService(
            securityRepository.Object,
            marketDataProvider.Object,
            unitOfWork.Object);

        await service.UpdateSecurityPricesAsync(cancellationToken);

        marketDataProvider.Verify(
            x => x.GetLatestPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    private static Security CreateSecurity(string ticker, decimal currentPrice)
    {
        var security = new Security(
            ticker,
            $"{ticker} security",
            SecurityType.Stock,
            PortfolioCurrency.USD);
        security.UpdatePrice(currentPrice);
        return security;
    }
}

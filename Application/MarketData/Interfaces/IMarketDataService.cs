namespace InvestManagement.Application.MarketData.Interfaces;

public interface IMarketDataService
{
    Task UpdateSecurityPricesAsync(CancellationToken cancellationToken);
}

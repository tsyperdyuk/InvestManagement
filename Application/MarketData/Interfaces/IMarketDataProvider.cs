namespace InvestManagement.Application.MarketData.Interfaces;

public interface IMarketDataProvider
{
    Task<decimal?> GetLatestPriceAsync(string ticker, CancellationToken cancellationToken);
}

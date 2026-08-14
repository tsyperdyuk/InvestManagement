using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.MarketData.Interfaces;
using InvestManagement.Application.Securities.Interfaces;

namespace InvestManagement.Application.MarketData.Services;

public sealed class MarketDataService : IMarketDataService
{
    private readonly ISecurityRepository _securityRepository;
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarketDataService(ISecurityRepository securityRepository, IMarketDataProvider marketDataProvider, IUnitOfWork unitOfWork)
    {
        _securityRepository = securityRepository;
        _marketDataProvider = marketDataProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task UpdateSecurityPricesAsync(CancellationToken cancellationToken)
    {
        var securities = await _securityRepository.GetAllAsync(cancellationToken);

        foreach (var security in securities)
        {
            var latestPrice = await _marketDataProvider.GetLatestPriceAsync(security.Ticker, cancellationToken);

            if (latestPrice is null)
            {
                continue;
            }

            security.UpdatePrice(latestPrice.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

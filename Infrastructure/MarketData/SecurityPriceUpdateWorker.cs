using InvestManagement.Application.MarketData.Interfaces;
using InvestManagement.Infrastructure.MarketData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InvestManagement.Application.MarketData;

public sealed class SecurityPriceUpdateWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MarketDataOptions _options;

    public SecurityPriceUpdateWorker(IServiceScopeFactory scopeFactory, IOptions<MarketDataOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddHours(_options.PriceUpdateHourUtc);

            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            using var scope = _scopeFactory.CreateScope();

            var marketDataService = scope.ServiceProvider.GetRequiredService<IMarketDataService>();

            await marketDataService.UpdateSecurityPricesAsync(stoppingToken);
        }
    }
}

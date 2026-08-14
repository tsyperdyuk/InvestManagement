using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace InvestManagement.Infrastructure.Data;

public sealed class DatabaseInitializer
{
    private readonly InvestManagementDbContext _dbContext;

    public DatabaseInitializer(InvestManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Securities.AnyAsync(cancellationToken))
        {
            return;
        }

        var securities = new List<Security>
        {
            new("AAPL", "Apple Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("MSFT", "Microsoft Corporation", SecurityType.Stock, PortfolioCurrency.USD),
            new("NVDA", "NVIDIA Corporation", SecurityType.Stock, PortfolioCurrency.USD),
            new("AMZN", "Amazon.com, Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("GOOGL", "Alphabet Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("META", "Meta Platforms, Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("TSLA", "Tesla, Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("AVGO", "Broadcom Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("JPM", "JPMorgan Chase & Co.", SecurityType.Stock, PortfolioCurrency.USD),
            new("WMT", "Walmart Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("V", "Visa Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("MA", "Mastercard Incorporated", SecurityType.Stock, PortfolioCurrency.USD),
            new("NFLX", "Netflix, Inc.", SecurityType.Stock, PortfolioCurrency.USD),
            new("COST", "Costco Wholesale Corporation", SecurityType.Stock, PortfolioCurrency.USD),
            new("AMD", "Advanced Micro Devices, Inc.", SecurityType.Stock, PortfolioCurrency.USD)
        };

        await _dbContext.Securities.AddRangeAsync(securities, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
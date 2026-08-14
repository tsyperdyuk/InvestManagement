namespace InvestManagement.Infrastructure.MarketData;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    public int PriceUpdateHourUtc { get; init; }
}

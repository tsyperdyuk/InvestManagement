namespace InvestManagement.Infrastructure.MarketData;

public sealed class AlphaVantageOptions
{
    public const string SectionName = "AlphaVantage";

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://www.alphavantage.co";
}

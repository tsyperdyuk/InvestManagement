using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using InvestManagement.Application.MarketData.Interfaces;

namespace InvestManagement.Infrastructure.MarketData;

public sealed class AlphaVantageMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly AlphaVantageOptions _options;
    private const string GlobalQuoteProperty = "Global Quote";
    private const string PriceProperty = "05. price";

    public AlphaVantageMarketDataProvider(HttpClient httpClient, IOptions<AlphaVantageOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<decimal?> GetLatestPriceAsync(string ticker, CancellationToken cancellationToken)
    {
        var url = $"/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={_options.ApiKey}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty(GlobalQuoteProperty, out var globalQuote))
        {
            return null;
        }

        if (!globalQuote.TryGetProperty(PriceProperty, out var priceElement))
        {
            return null;
        }

        var priceText = priceElement.GetString();

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return null;
        }

        return price;
    }
}

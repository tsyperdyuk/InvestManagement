using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvestManagement.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public sealed class MarketDataEndpointTests
{
    private readonly InvestManagementApiFactory _factory;
    private readonly HttpClient _client;

    public MarketDataEndpointTests(InvestManagementApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task UpdatePrices_UpdatesSecuritiesAndReturnsNoContent()
    {
        var response = await _client.PostAsync("/api/market-data/update-prices", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(225m, await _factory.GetSecurityPriceAsync("AAPL"));
    }
}

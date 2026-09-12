using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Transactions.Models.Responses;
using InvestManagement.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvestManagement.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public sealed class TransactionsEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly InvestManagementApiFactory _factory;
    private readonly HttpClient _client;

    public TransactionsEndpointTests(InvestManagementApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public Task InitializeAsync() => TestAuthentication.SignInAsync(_client);

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DepositAndWithdraw_WithValidAmounts_PersistTransactions()
    {
        var portfolio = await CreatePortfolioAsync();

        var deposit = await PostTransactionAsync(
            portfolio.Id,
            "deposit",
            new { Amount = 1_000m });
        var withdrawal = await PostTransactionAsync(
            portfolio.Id,
            "withdraw",
            new { Amount = 250m });

        Assert.Equal(TransactionType.Deposit, deposit.Type);
        Assert.Equal(1_000m, deposit.Amount);
        Assert.Equal(TransactionType.Withdrawal, withdrawal.Type);
        Assert.Equal(250m, withdrawal.Amount);

        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(750m, details.CashBalance);
    }

    [Fact]
    public async Task BuyAndSell_WithPricedSecurity_PersistTransactionsAndPosition()
    {
        var portfolio = await CreatePortfolioAsync();
        var securityId = await _factory.SetSecurityPriceAsync("AAPL", 100m);
        await PostTransactionAsync(portfolio.Id, "deposit", new { Amount = 1_000m });

        var buy = await PostTransactionAsync(
            portfolio.Id,
            "buy",
            new { SecurityId = securityId, Quantity = 5m });
        var sell = await PostTransactionAsync(
            portfolio.Id,
            "sell",
            new { SecurityId = securityId, Quantity = 2m });

        Assert.Equal(TransactionType.Buy, buy.Type);
        Assert.Equal(500m, buy.Amount);
        Assert.Equal(TransactionType.Sell, sell.Type);
        Assert.Equal(200m, sell.Amount);

        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(700m, details.CashBalance);
        Assert.Equal(3m, Assert.Single(details.Securities).Quantity);
    }

    private async Task<PortfolioModel> CreatePortfolioAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolios", new
        {
            Name = $"Transactions portfolio {Guid.NewGuid():N}",
            Currency = PortfolioCurrency.USD
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PortfolioModel>(JsonOptions))!;
    }

    private async Task<TransactionResponse> PostTransactionAsync(
        Guid portfolioId,
        string operation,
        object request)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolioId}/transactions/{operation}",
            request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions))!;
    }
}

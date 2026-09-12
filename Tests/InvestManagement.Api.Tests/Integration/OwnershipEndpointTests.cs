using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvestManagement.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public sealed class OwnershipEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _owner;
    private readonly HttpClient _other;
    private readonly HttpClient _anonymous;

    public OwnershipEndpointTests(InvestManagementApiFactory factory)
    {
        var options = new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        };
        _owner = factory.CreateClient(options);
        _other = factory.CreateClient(options);
        _anonymous = factory.CreateClient(options);
    }

    public async Task InitializeAsync()
    {
        await TestAuthentication.SignInAsync(_owner);
        await TestAuthentication.SignInAsync(_other);
    }

    public Task DisposeAsync()
    {
        _owner.Dispose();
        _other.Dispose();
        _anonymous.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Portfolios_OnlyOwnerCanReadModifyAndCountThem()
    {
        var portfolioId = await CreatePortfolioAsync(_owner);
        var otherPortfolioId = await CreatePortfolioAsync(_other);
        var path = $"/api/portfolios/{portfolioId}";

        Assert.Equal(HttpStatusCode.NotFound, (await _other.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.GetAsync($"{path}/details")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _other.PutAsJsonAsync($"{path}/rename", new { Name = "Hijacked" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.DeleteAsync(path)).StatusCode);

        var page = await _other.GetFromJsonAsync<JsonElement>("/api/portfolios?pageNumber=1&pageSize=1");
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(otherPortfolioId, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var emptyPage = await _other.GetFromJsonAsync<JsonElement>("/api/portfolios?pageNumber=2&pageSize=1");
        Assert.Empty(emptyPage.GetProperty("items").EnumerateArray());

        Assert.Equal(HttpStatusCode.NoContent,
            (await _owner.PutAsJsonAsync($"{path}/rename", new { Name = "Owner renamed" })).StatusCode);
        var updated = await _owner.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal("Owner renamed", updated.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await _owner.DeleteAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Transactions_RejectOtherUsersBeforeChangingBalance()
    {
        var portfolioId = await CreatePortfolioAsync(_owner);
        var path = $"/api/portfolios/{portfolioId}/transactions";
        var deposit = await _owner.PostAsJsonAsync($"{path}/deposit", new { Amount = 100m });
        Assert.Equal(HttpStatusCode.Created, deposit.StatusCode);

        foreach (var operation in new[] { "deposit", "withdraw" })
        {
            var response = await _other.PostAsJsonAsync($"{path}/{operation}", new { Amount = 10m });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        foreach (var operation in new[] { "buy", "sell" })
        {
            var response = await _other.PostAsJsonAsync($"{path}/{operation}",
                new { SecurityId = Guid.NewGuid(), Quantity = 1m });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        var details = await _owner.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolioId}/details");
        Assert.Equal(100m, details.GetProperty("cashBalance").GetDecimal());
    }

    [Fact]
    public async Task Scenarios_RejectOtherUsersForEveryOperation_AndOwnerCanApply()
    {
        var portfolioId = await CreatePortfolioAsync(_owner);
        var listPath = $"/api/portfolios/{portfolioId}/scenarios";
        var created = await _owner.PostAsJsonAsync(listPath, new { Name = "Owner scenario" });
        created.EnsureSuccessStatusCode();
        var scenario = await created.Content.ReadFromJsonAsync<JsonElement>();
        var path = $"/api/scenarios/{scenario.GetProperty("id").GetGuid()}";
        var cashOperation = new { Type = "Deposit", Amount = 50m };
        var added = await _owner.PostAsJsonAsync($"{path}/operations", cashOperation);
        added.EnsureSuccessStatusCode();
        var operation = await added.Content.ReadFromJsonAsync<JsonElement>();
        var operationPath = $"{path}/operations/{operation.GetProperty("id").GetGuid()}";

        Assert.Equal(HttpStatusCode.NotFound, (await _other.GetAsync(listPath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _other.PostAsJsonAsync(listPath, new { Name = "Foreign scenario" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.GetAsync($"{path}/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.DeleteAsync(operationPath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _other.PostAsJsonAsync($"{path}/operations", cashOperation)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _other.PostAsync($"{path}/apply", null)).StatusCode);

        var visible = await _owner.GetFromJsonAsync<JsonElement>(listPath);
        Assert.Single(visible.EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await _owner.GetAsync($"{path}/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _owner.PostAsync($"{path}/apply", null)).StatusCode);
        var details = await _owner.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolioId}/details");
        Assert.Equal(50m, details.GetProperty("cashBalance").GetDecimal());
    }

    [Theory]
    [InlineData("GET", "/api/portfolios")]
    [InlineData("GET", "/api/portfolios/{id}/details")]
    [InlineData("POST", "/api/portfolios/{id}/transactions/deposit")]
    [InlineData("GET", "/api/portfolios/{id}/scenarios")]
    [InlineData("GET", "/api/scenarios/{id}")]
    [InlineData("POST", "/api/scenarios/{id}/apply")]
    public async Task ProtectedEndpoints_WithoutToken_ReturnUnauthorized(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", Guid.NewGuid().ToString()));
        request.Content = JsonContent.Create(new { Amount = 10m });
        var response = await _anonymous.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/portfolios", new { Name = "Private portfolio", Currency = "GBP" });
        response.EnsureSuccessStatusCode();
        var portfolio = await response.Content.ReadFromJsonAsync<JsonElement>();
        return portfolio.GetProperty("id").GetGuid();
    }
}

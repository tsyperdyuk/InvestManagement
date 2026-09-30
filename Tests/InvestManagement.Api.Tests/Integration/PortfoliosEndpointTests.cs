using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestManagement.Application.Common;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Portfolios.Requests;
using InvestManagement.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvestManagement.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public sealed class PortfoliosEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly InvestManagementApiFactory _factory;

    public PortfoliosEndpointTests(InvestManagementApiFactory factory)
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
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync("/api/portfolios", new
        {
            Name = "Anonymous portfolio",
            Currency = PortfolioCurrency.GBP
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetById_WithValidRequest_PersistsPortfolioInPostgreSql()
    {
        var request = new CreatePortfolioRequest
        {
            Name = $"Integration portfolio {Guid.NewGuid():N}",
            Currency = PortfolioCurrency.GBP
        };

        var createResponse = await _client.PostAsJsonAsync("/api/portfolios", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PortfolioModel>(JsonOptions);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.Name, created.Name);
        Assert.Equal(request.Currency, created.Currency);
        Assert.Equal($"/api/portfolios/{created.Id}", createResponse.Headers.Location?.AbsolutePath);

        var persisted = await _client.GetFromJsonAsync<PortfolioModel>(
            $"/api/portfolios/{created.Id}", JsonOptions);

        Assert.NotNull(persisted);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal(request.Name, persisted.Name);
        Assert.Equal(request.Currency, persisted.Currency);

        var currentUser = await _client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var ownerId = await _factory.GetPortfolioOwnerIdAsync(created.Id);
        Assert.Equal(currentUser.GetProperty("userId").GetGuid(), ownerId);
    }

    [Fact]
    public async Task GetAll_AfterCreatingPortfolio_ReturnsItFromPostgreSql()
    {
        var name = $"Paged portfolio {Guid.NewGuid():N}";
        var createResponse = await _client.PostAsJsonAsync("/api/portfolios", new
        {
            Name = name,
            Currency = PortfolioCurrency.USD
        });
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/api/portfolios?pageNumber=1&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PortfolioModel>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Contains(page.Items, portfolio => portfolio.Name == name);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/portfolios", new
        {
            Name = string.Empty,
            Currency = PortfolioCurrency.EUR
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PendingDeposit_ThenGetDetails_DoesNotChangeCashBalance()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/portfolios", new
        {
            Name = $"Cash portfolio {Guid.NewGuid():N}",
            Currency = PortfolioCurrency.GBP
        });
        var portfolio = await createResponse.Content.ReadFromJsonAsync<PortfolioModel>(JsonOptions);
        Assert.NotNull(portfolio);

        var depositResponse = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/deposit",
            new { Amount = 1_250m });

        Assert.Equal(HttpStatusCode.Accepted, depositResponse.StatusCode);

        var detailsResponse = await _client.GetAsync(
            $"/api/portfolios/{portfolio.Id}/details");
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

        var details = await detailsResponse.Content.ReadFromJsonAsync<PortfolioSummary>(JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(0m, details.CashBalance);
        Assert.Equal(0m, details.TotalPortfolioValue);
    }

    [Fact]
    public async Task RenameAndDelete_WithExistingPortfolio_PersistsBothChanges()
    {
        var created = await CreatePortfolioAsync("Original name");

        var renameResponse = await _client.PutAsJsonAsync(
            $"/api/portfolios/{created.Id}/rename",
            new { Name = "Renamed portfolio" });

        Assert.Equal(HttpStatusCode.NoContent, renameResponse.StatusCode);
        var renamed = await _client.GetFromJsonAsync<PortfolioModel>(
            $"/api/portfolios/{created.Id}", JsonOptions);
        Assert.NotNull(renamed);
        Assert.Equal("Renamed portfolio", renamed.Name);

        var deleteResponse = await _client.DeleteAsync($"/api/portfolios/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/portfolios/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task PortfolioEndpoints_WithUnknownId_ReturnNotFound()
    {
        var id = Guid.NewGuid();

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/portfolios/{id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/portfolios/{id}/details")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.PutAsJsonAsync(
                $"/api/portfolios/{id}/rename",
                new { Name = "Missing" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.DeleteAsync($"/api/portfolios/{id}")).StatusCode);
    }

    private async Task<PortfolioModel> CreatePortfolioAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/portfolios", new
        {
            Name = name,
            Currency = PortfolioCurrency.GBP
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PortfolioModel>(JsonOptions))!;
    }
}

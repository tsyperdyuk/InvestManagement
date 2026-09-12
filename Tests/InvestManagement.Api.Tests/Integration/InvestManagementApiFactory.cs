using DotNet.Testcontainers.Builders;
using InvestManagement.Application.MarketData.Interfaces;
using InvestManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace InvestManagement.Api.Tests.Integration;

public sealed class InvestManagementApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("invest_management_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-U", "postgres"))
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<InvestManagementDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var dbContext = new InvestManagementDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task<Guid> GetPortfolioOwnerIdAsync(Guid portfolioId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        return await dbContext.Portfolios
            .Where(portfolio => portfolio.Id == portfolioId)
            .Select(portfolio => portfolio.UserId)
            .SingleAsync();
    }

    public async Task<Guid> SetSecurityPriceAsync(string ticker, decimal price)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var security = await dbContext.Securities.SingleAsync(x => x.Ticker == ticker);
        security.UpdatePrice(price);
        await dbContext.SaveChangesAsync();
        return security.Id;
    }

    public async Task<decimal> GetSecurityPriceAsync(string ticker)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        return await dbContext.Securities
            .Where(x => x.Ticker == ticker)
            .Select(x => x.CurrentPrice)
            .SingleAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<InvestManagementDbContext>>();
            services.RemoveAll<InvestManagementDbContext>();
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IMarketDataProvider>();

            services.AddDbContext<InvestManagementDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
            services.AddSingleton<IMarketDataProvider, StubMarketDataProvider>();
        });
    }

    private sealed class StubMarketDataProvider : IMarketDataProvider
    {
        public Task<decimal?> GetLatestPriceAsync(
            string ticker,
            CancellationToken cancellationToken) =>
            Task.FromResult<decimal?>(ticker == "AAPL" ? 225m : null);
    }
}

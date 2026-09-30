using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Transactions.Models.Responses;
using InvestManagement.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using InvestManagement.Application.Contracts;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task Deposit_WithValidAmount_PersistsPendingOperationAndCommandWithoutChangingBalance()
    {
        var portfolio = await CreatePortfolioAsync();
        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/deposit",
            new { Amount = 1_000m });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(TransactionOperationStatus.Pending, result.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var operation = await db.TransactionOperations.SingleAsync(x => x.Id == result.OperationId);
        Assert.Equal(portfolio.Id, operation.PortfolioId);
        Assert.Equal(await _factory.GetPortfolioOwnerIdAsync(portfolio.Id), operation.UserId);
        Assert.Equal(1_000m, operation.Amount);
        Assert.Equal(TransactionOperationStatus.Pending, operation.Status);
        Assert.Null(operation.TransactionId);
        Assert.False(await db.Transactions.AnyAsync(x => x.PortfolioId == portfolio.Id));
        var commands = await db.OutboxMessages.Where(x => x.Type == "ProcessTransaction.v1").ToListAsync();
        var message = Assert.Single(commands, x =>
            JsonSerializer.Deserialize<ProcessTransaction>(x.Payload)!.OperationId == operation.Id);
        Assert.Null(message.ProcessedAtUtc);

        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(0m, details.CashBalance);
    }

    [Fact]
    public async Task ProcessDeposit_Twice_CreditsPortfolioOnlyOnce()
    {
        var portfolio = await CreatePortfolioAsync();
        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/deposit",
            new { Amount = 100m });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var request = await response.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(request);

        Guid? firstTransactionId;
        DateTime? firstFinishedAt;
        await using (var firstScope = _factory.Services.CreateAsyncScope())
        {
            var processor = firstScope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>();
            await processor.ProcessAsync(request.OperationId, CancellationToken.None);
            var db = firstScope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            var operation = await db.TransactionOperations.AsNoTracking()
                .SingleAsync(x => x.Id == request.OperationId);
            Assert.Equal(TransactionOperationStatus.Completed, operation.Status);
            Assert.NotNull(operation.TransactionId);
            Assert.NotNull(operation.FinishedAtUtc);
            firstTransactionId = operation.TransactionId;
            firstFinishedAt = operation.FinishedAtUtc;
        }

        // A redelivery uses a fresh scope and database context.
        await using (var secondScope = _factory.Services.CreateAsyncScope())
        {
            var processor = secondScope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>();
            await processor.ProcessAsync(request.OperationId, CancellationToken.None);
        }

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var savedOperation = await verificationDb.TransactionOperations.AsNoTracking()
            .SingleAsync(x => x.Id == request.OperationId);
        Assert.Equal(TransactionOperationStatus.Completed, savedOperation.Status);
        Assert.Equal(firstTransactionId, savedOperation.TransactionId);
        Assert.Equal(firstFinishedAt, savedOperation.FinishedAtUtc);
        var transaction = Assert.Single(await verificationDb.Transactions.AsNoTracking()
            .Where(x => x.PortfolioId == portfolio.Id).ToListAsync());
        Assert.Equal(firstTransactionId, transaction.Id);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(100m, transaction.Amount);

        var status = await _client.GetFromJsonAsync<TransactionOperationResponse>(
            $"/api/transaction-operations/{request.OperationId}", JsonOptions);
        Assert.NotNull(status);
        Assert.Equal(TransactionOperationStatus.Completed, status.Status);
        Assert.Equal(firstTransactionId, status.TransactionId);
        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(100m, details.CashBalance);
    }

    [Theory]
    [InlineData(250, true, 750)]
    [InlineData(1000, true, 0)]
    [InlineData(1001, false, 1000)]
    public async Task Withdraw_ProcessesOnceUsingAvailableBalance(decimal amount, bool succeeds, decimal expectedBalance)
    {
        var portfolio = await CreatePortfolioAsync();
        await _factory.SeedCompletedDepositAsync(portfolio.Id, 1_000m);
        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/withdraw", new { Amount = amount });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var operation = await response.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(operation);
        Assert.Equal(TransactionOperationStatus.Pending, operation.Status);

        var pendingDetails = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(pendingDetails);
        Assert.Equal(1000m, pendingDetails.CashBalance);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            var commands = await db.OutboxMessages.Where(x => x.Type == "ProcessTransaction.v1").ToListAsync();
            Assert.Single(commands, x => JsonSerializer.Deserialize<ProcessTransaction>(x.Payload)!.OperationId == operation.OperationId);
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>()
                .ProcessAsync(operation.OperationId, CancellationToken.None);
        }

        var status = await _client.GetFromJsonAsync<TransactionOperationResponse>(
            $"/api/transaction-operations/{operation.OperationId}", JsonOptions);
        Assert.NotNull(status);
        Assert.Equal(succeeds ? TransactionOperationStatus.Completed : TransactionOperationStatus.Failed, status.Status);
        Assert.Equal(succeeds ? null : "Insufficient cash balance.", status.FailureReason);
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var withdrawals = await verificationDb.Transactions.Where(x =>
            x.PortfolioId == portfolio.Id && x.Type == TransactionType.Withdrawal).ToListAsync();
        if (succeeds)
        {
            var withdrawal = Assert.Single(withdrawals);
            Assert.Equal(amount, withdrawal.Amount);
            Assert.Equal(withdrawal.Id, status.TransactionId);
        }
        else
        {
            Assert.Empty(withdrawals);
            Assert.Null(status.TransactionId);
        }

        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(expectedBalance, details.CashBalance);
    }

    [Fact]
    public async Task BuyAndSell_WithPricedSecurity_PersistTransactionsAndPosition()
    {
        var portfolio = await CreatePortfolioAsync();
        var securityId = await _factory.SetSecurityPriceAsync("AAPL", 100m);
        await _factory.SeedCompletedDepositAsync(portfolio.Id, 1_000m);

        var buyResponse = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/buy",
            new { SecurityId = securityId, Quantity = 5m });
        Assert.Equal(HttpStatusCode.Accepted, buyResponse.StatusCode);
        var operation = await buyResponse.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(operation);
        Assert.Equal(TransactionOperationStatus.Pending, operation.Status);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>()
                .ProcessAsync(operation.OperationId, CancellationToken.None);
        }
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var buy = Assert.Single(await db.Transactions.Where(x =>
            x.PortfolioId == portfolio.Id && x.Type == TransactionType.Buy).ToListAsync());
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

    [Theory]
    [InlineData(100, 1000, PortfolioCurrency.USD, null)]
    [InlineData(100, 50, PortfolioCurrency.USD, "Insufficient cash balance.")]
    [InlineData(0, 1000, PortfolioCurrency.USD, "Security price is not available.")]
    [InlineData(100, 1000, PortfolioCurrency.EUR, "Security currency must match portfolio currency.")]
    public async Task Buy_UsesExecutionPriceAndValidatesFundsAndCurrency(
        decimal executionPrice, decimal funds, PortfolioCurrency currency, string? failure)
    {
        var portfolio = await CreatePortfolioAsync();
        await _factory.SeedCompletedDepositAsync(portfolio.Id, funds);
        var security = new Security("TEST", "Test security", SecurityType.Stock, currency);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            db.Securities.Add(security);
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/buy",
            new { SecurityId = security.Id, Quantity = 2m });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var operation = await response.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(operation);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            var messages = await db.OutboxMessages.Where(x => x.Type == "ProcessTransaction.v1").ToListAsync();
            Assert.Single(messages, x => JsonSerializer.Deserialize<ProcessTransaction>(x.Payload)!.OperationId == operation.OperationId);
            Assert.False(await db.Transactions.AnyAsync(x => x.PortfolioId == portfolio.Id && x.Type == TransactionType.Buy));
            if (executionPrice > 0)
            {
                var savedSecurity = await db.Securities.SingleAsync(x => x.Id == security.Id);
                savedSecurity.UpdatePrice(executionPrice);
                await db.SaveChangesAsync();
            }
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>()
                .ProcessAsync(operation.OperationId, CancellationToken.None);
        }

        var status = await _client.GetFromJsonAsync<TransactionOperationResponse>(
            $"/api/transaction-operations/{operation.OperationId}", JsonOptions);
        Assert.NotNull(status);
        Assert.Equal(failure is null ? TransactionOperationStatus.Completed : TransactionOperationStatus.Failed, status.Status);
        Assert.Equal(failure, status.FailureReason);
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var buys = await verificationDb.Transactions.Where(x => x.PortfolioId == portfolio.Id && x.Type == TransactionType.Buy).ToListAsync();
        if (failure is null)
        {
            var buy = Assert.Single(buys);
            Assert.Equal(buy.Id, status.TransactionId);
            Assert.Equal(executionPrice, buy.Price);
            Assert.Equal(2m * executionPrice, buy.Amount);
        }
        else
        {
            Assert.Empty(buys);
            Assert.Null(status.TransactionId);
        }
        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(failure is null ? funds - 2m * executionPrice : funds, details.CashBalance);
    }

    [Theory]
    [InlineData(2, null, 3)]
    [InlineData(5, null, 0)]
    [InlineData(6, "Insufficient security quantity.", 5)]
    public async Task Sell_ValidatesPositionAndDoesNotExecuteTwice(
        decimal quantity, string? failure, decimal remaining)
    {
        var portfolio = await CreatePortfolioAsync();
        var securityId = await _factory.SetSecurityPriceAsync("AAPL", 100m);
        await _factory.SeedCompletedDepositAsync(portfolio.Id, 1000m);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            db.Transactions.Add(Transaction.CreateBuy(portfolio.Id, securityId, 5m, 100m, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio.Id}/transactions/sell",
            new { SecurityId = securityId, Quantity = quantity });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var operation = await response.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(operation);
        Assert.Equal(TransactionOperationStatus.Pending, operation.Status);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            Assert.False(await db.Transactions.AnyAsync(x => x.PortfolioId == portfolio.Id && x.Type == TransactionType.Sell));
            var messages = await db.OutboxMessages.Where(x => x.Type == "ProcessTransaction.v1").ToListAsync();
            Assert.Single(messages, x => JsonSerializer.Deserialize<ProcessTransaction>(x.Payload)!.OperationId == operation.OperationId);
        }
        await _factory.SetSecurityPriceAsync("AAPL", 120m);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>()
                .ProcessAsync(operation.OperationId, CancellationToken.None);
        }
        var status = await _client.GetFromJsonAsync<TransactionOperationResponse>(
            $"/api/transaction-operations/{operation.OperationId}", JsonOptions);
        Assert.NotNull(status);
        Assert.Equal(failure is null ? TransactionOperationStatus.Completed : TransactionOperationStatus.Failed, status.Status);
        Assert.Equal(failure, status.FailureReason);
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var sales = await verificationDb.Transactions.Where(x => x.PortfolioId == portfolio.Id && x.Type == TransactionType.Sell).ToListAsync();
        if (failure is null)
        {
            var sale = Assert.Single(sales);
            Assert.Equal(sale.Id, status.TransactionId);
            Assert.Equal(120m, sale.Price);
            Assert.Equal(quantity, sale.Quantity);
            Assert.Equal(quantity * 120m, sale.Amount);
        }
        else
        {
            Assert.Empty(sales);
            Assert.Null(status.TransactionId);
        }
        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(failure is null ? 500m + quantity * 120m : 500m, details.CashBalance);
        Assert.Equal(remaining, details.Securities.Sum(x => x.Quantity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentOperations_ConflictThenRetry_OnlyOneCanSpend(bool sell)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var token = timeout.Token;
        var portfolio = await CreatePortfolioAsync();
        var ownerId = await _factory.GetPortfolioOwnerIdAsync(portfolio.Id);
        var securityId = await _factory.SetSecurityPriceAsync("AAPL", 10m);
        TransactionOperation CreateOperation() => sell
            ? TransactionOperation.CreateSell(portfolio.Id, ownerId, securityId, 4m)
            : TransactionOperation.CreateWithdrawal(portfolio.Id, ownerId, 80m);
        var first = CreateOperation();
        var second = CreateOperation();
        string connectionString;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            connectionString = db.Database.GetConnectionString()!;
            db.Transactions.Add(Transaction.CreateDeposit(portfolio.Id, 100m, DateTime.UtcNow));
            if (sell)
            {
                db.Transactions.Add(Transaction.CreateBuy(portfolio.Id, securityId, 5m, 10m, DateTime.UtcNow));
            }
            db.TransactionOperations.AddRange(first, second);
            await db.SaveChangesAsync(token);
        }

        // Both contexts must read the original balance and reach SaveChanges before either writes.
        var barrier = new TwoSavesBarrier();
        var options = new DbContextOptionsBuilder<InvestManagementDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(barrier)
            .Options;
        async Task<Exception?> ProcessConcurrently(Guid id)
        {
            await using var db = new InvestManagementDbContext(options);
            var processor = new TransactionOperationProcessor(db, NullLogger<TransactionOperationProcessor>.Instance);
            return await Record.ExceptionAsync(() => processor.ProcessAsync(id, token));
        }

        var results = await Task.WhenAll(
            ProcessConcurrently(first.Id), ProcessConcurrently(second.Id));
        Assert.Single(results, result => result is null);
        var error = Assert.Single(results, result => result is not null);
        Assert.IsType<DbUpdateConcurrencyException>(error);
        var retryId = results[0] is null ? second.Id : first.Id;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            var pending = await db.TransactionOperations.SingleAsync(x => x.Id == retryId, token);
            Assert.Equal(TransactionOperationStatus.Pending, pending.Status);
            Assert.Null(pending.TransactionId);
            Assert.Equal(1, await db.Transactions.CountAsync(x => x.PortfolioId == portfolio.Id &&
                x.Type == (sell ? TransactionType.Sell : TransactionType.Withdrawal), token));
        }

        // Model redelivery with a new scope, just as the Service Bus worker does.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>()
                .ProcessAsync(retryId, token);
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
            var operations = await db.TransactionOperations.Where(x => x.PortfolioId == portfolio.Id).ToListAsync(token);
            Assert.Single(operations, operation => operation.Status == TransactionOperationStatus.Completed);
            var failed = Assert.Single(operations, operation => operation.Status == TransactionOperationStatus.Failed);
            Assert.Equal(retryId, failed.Id);
            Assert.Null(failed.TransactionId);
            Assert.Equal(sell ? "Insufficient security quantity." : "Insufficient cash balance.", failed.FailureReason);
            Assert.Equal(1, await db.Transactions.CountAsync(x => x.PortfolioId == portfolio.Id &&
                x.Type == (sell ? TransactionType.Sell : TransactionType.Withdrawal), token));
        }
        var details = await _client.GetFromJsonAsync<PortfolioSummary>(
            $"/api/portfolios/{portfolio.Id}/details", JsonOptions, token);
        Assert.NotNull(details);
        Assert.Equal(sell ? 90m : 20m, details.CashBalance);
        Assert.Equal(sell ? 1m : 0m, details.Securities.Sum(x => x.Quantity));
    }

    private sealed class TwoSavesBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
            {
                _ready.TrySetResult(true);
            }
            await _ready.Task.WaitAsync(cancellationToken);
            return result;
        }
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
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionOperationResponse>(JsonOptions);
        Assert.NotNull(result);
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TransactionOperationProcessor>()
            .ProcessAsync(result.OperationId, CancellationToken.None);
        var db = scope.ServiceProvider.GetRequiredService<InvestManagementDbContext>();
        var savedOperation = await db.TransactionOperations.SingleAsync(x => x.Id == result.OperationId);
        Assert.Equal(TransactionOperationStatus.Completed, savedOperation.Status);
        var transaction = await db.Transactions.SingleAsync(x => x.Id == savedOperation.TransactionId);
        return new TransactionResponse(transaction.Id, transaction.PortfolioId, transaction.SecurityId,
            transaction.Type, transaction.Amount, transaction.Quantity, transaction.Price, transaction.CreatedAt);
    }
}

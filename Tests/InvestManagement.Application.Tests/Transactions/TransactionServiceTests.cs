using InvestManagement.Application.Transactions.Models.Requests;
using InvestManagement.Application.Transactions.Services;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvestManagement.Application.Tests.Transactions;

public sealed class TransactionServiceTests
{
    [Fact]
    public async Task DepositAsync_WithValidRequest_CreatesDepositTransaction()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new DepositTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 1_000m
        };

        var response = await service.DepositAsync(
            request,
            CancellationToken.None);

        Assert.Equal(portfolio.Id, response.PortfolioId);
        Assert.Equal(TransactionType.Deposit, response.Type);
        Assert.Equal(1_000m, response.Amount);
        Assert.Null(response.SecurityId);

        var transactions = await dbContext.Transactions.ToListAsync();
        var transaction = Assert.Single(transactions);
        Assert.Equal(response.Id, transaction.Id);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(1_000m, transaction.Amount);
    }

    [Fact]
    public async Task WithdrawalAsync_WithSufficientCash_CreatesWithdrawalTransaction()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            1_000m,
            DateTime.UtcNow.AddMinutes(-1));
        dbContext.Portfolios.Add(portfolio);
        dbContext.Transactions.Add(deposit);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new WithdrawalTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 250m
        };

        var response = await service.WithdrawalAsync(
            request,
            CancellationToken.None);

        Assert.Equal(TransactionType.Withdrawal, response.Type);
        Assert.Equal(250m, response.Amount);
        Assert.Equal(2, await dbContext.Transactions.CountAsync());
        Assert.Contains(
            dbContext.Transactions,
            transaction => transaction.Id == response.Id
                && transaction.Type == TransactionType.Withdrawal);
    }

    [Fact]
    public async Task BuyAsync_WithSufficientCash_CreatesBuyAtCurrentPrice()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(125m);
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            2_000m,
            DateTime.UtcNow.AddMinutes(-1));
        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.Add(deposit);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new BuyTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            Quantity = 4m
        };

        var response = await service.BuyAsync(
            request,
            CancellationToken.None);

        Assert.Equal(TransactionType.Buy, response.Type);
        Assert.Equal(security.Id, response.SecurityId);
        Assert.Equal(4m, response.Quantity);
        Assert.Equal(125m, response.Price);
        Assert.Equal(500m, response.Amount);
        Assert.Equal(2, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task SellAsync_WithSufficientQuantity_CreatesSellAtCurrentPrice()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(100m);
        var buy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            5m,
            100m,
            DateTime.UtcNow.AddMinutes(-1));
        security.UpdatePrice(130m);
        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.Add(buy);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new SellTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            Quantity = 2m
        };

        var response = await service.SellAsync(
            request,
            CancellationToken.None);

        Assert.Equal(TransactionType.Sell, response.Type);
        Assert.Equal(security.Id, response.SecurityId);
        Assert.Equal(2m, response.Quantity);
        Assert.Equal(130m, response.Price);
        Assert.Equal(260m, response.Amount);
        Assert.Equal(2, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task DepositAsync_WhenPortfolioDoesNotExist_ThrowsAndDoesNotCreateTransaction()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = new DepositTransactionRequest
        {
            PortfolioId = Guid.NewGuid(),
            Amount = 1_000m
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DepositAsync(request, CancellationToken.None));

        Assert.Equal("Portfolio not found.", exception.Message);
        Assert.Empty(await dbContext.Transactions.ToListAsync());
    }

    [Fact]
    public async Task BuyAsync_WhenSecurityDoesNotExist_ThrowsAndDoesNotCreateTransaction()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new BuyTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = Guid.NewGuid(),
            Quantity = 1m
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BuyAsync(request, CancellationToken.None));

        Assert.Equal("Security not found.", exception.Message);
        Assert.Empty(await dbContext.Transactions.ToListAsync());
    }

    [Fact]
    public async Task BuyAsync_WhenSecurityPriceIsUnavailable_ThrowsAndDoesNotCreateTransaction()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new BuyTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            Quantity = 1m
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BuyAsync(request, CancellationToken.None));

        Assert.Equal("Security price is not available.", exception.Message);
        Assert.Empty(await dbContext.Transactions.ToListAsync());
    }

    [Fact]
    public async Task WithdrawalAsync_WhenAmountExceedsCashBalance_ThrowsAndDoesNotCreateTransaction()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var deposit = Transaction.CreateDeposit(
            portfolio.Id,
            1_000m,
            DateTime.UtcNow);

        dbContext.Portfolios.Add(portfolio);
        dbContext.Transactions.Add(deposit);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new WithdrawalTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 1_001m
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.WithdrawalAsync(request, CancellationToken.None));

        Assert.Equal("Insufficient cash balance.", exception.Message);
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task SellAsync_WhenQuantityExceedsPosition_ThrowsAndDoesNotCreateTransaction()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
        security.UpdatePrice(100m);
        var buy = Transaction.CreateBuy(
            portfolio.Id,
            security.Id,
            5m,
            100m,
            DateTime.UtcNow);

        dbContext.Portfolios.Add(portfolio);
        dbContext.Securities.Add(security);
        dbContext.Transactions.Add(buy);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new SellTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            Quantity = 6m
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SellAsync(request, CancellationToken.None));

        Assert.Equal("Insufficient security quantity.", exception.Message);
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    private static TransactionService CreateService(
        InvestManagementDbContext dbContext)
    {
        return new TransactionService(
            new PortfolioRepository(dbContext),
            new TransactionRepository(dbContext),
            new SecurityRepository(dbContext),
            new UnitOfWork(dbContext));
    }

    private static InvestManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InvestManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InvestManagementDbContext(options);
    }
}

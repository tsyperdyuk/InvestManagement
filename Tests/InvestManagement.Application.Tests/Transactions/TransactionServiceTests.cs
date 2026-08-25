using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Requests;
using InvestManagement.Application.Transactions.Services;
using InvestManagement.Domain.Model;
using Moq;
using Xunit;

namespace InvestManagement.Application.Tests.Transactions;

public sealed class TransactionServiceTests
{
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task DepositAsync_WithValidRequest_CreatesDepositTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var portfolioRepository = PortfolioRepositoryReturning(portfolio);
        var transactionRepository = new Mock<ITransactionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = CreateService(portfolioRepository, transactionRepository, unitOfWork: unitOfWork);

        var response = await service.DepositAsync(new DepositTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 1_000m
        }, Token);

        Assert.Equal(portfolio.Id, response.PortfolioId);
        Assert.Equal(TransactionType.Deposit, response.Type);
        Assert.Equal(1_000m, response.Amount);
        Assert.Null(response.SecurityId);
        transactionRepository.Verify(x => x.AddAsync(
            It.Is<Transaction>(t => t.Id == response.Id), Token), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(Token), Times.Once);
    }

    [Fact]
    public async Task WithdrawalAsync_WithSufficientCash_CreatesWithdrawalTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var transactions = new[] { Transaction.CreateDeposit(portfolio.Id, 1_000m, DateTime.UtcNow) };
        var portfolioRepository = PortfolioRepositoryReturning(portfolio);
        var transactionRepository = TransactionRepositoryReturning(transactions);
        var service = CreateService(portfolioRepository, transactionRepository);

        var response = await service.WithdrawalAsync(new WithdrawalTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 250m
        }, Token);

        Assert.Equal(TransactionType.Withdrawal, response.Type);
        Assert.Equal(250m, response.Amount);
        transactionRepository.Verify(x => x.AddAsync(
            It.Is<Transaction>(t => t.Id == response.Id), Token), Times.Once);
    }

    [Fact]
    public async Task BuyAsync_WithSufficientCash_CreatesBuyAtCurrentPrice()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = CreateSecurity(125m);
        var portfolioRepository = PortfolioRepositoryReturning(portfolio);
        var transactionRepository = TransactionRepositoryReturning(
            [Transaction.CreateDeposit(portfolio.Id, 2_000m, DateTime.UtcNow)]);
        var securityRepository = SecurityRepositoryReturning(security);
        var service = CreateService(portfolioRepository, transactionRepository, securityRepository);

        var response = await service.BuyAsync(new BuyTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            Quantity = 4m
        }, Token);

        Assert.Equal(TransactionType.Buy, response.Type);
        Assert.Equal(security.Id, response.SecurityId);
        Assert.Equal(4m, response.Quantity);
        Assert.Equal(125m, response.Price);
        Assert.Equal(500m, response.Amount);
        transactionRepository.Verify(x => x.AddAsync(
            It.Is<Transaction>(t => t.Id == response.Id), Token), Times.Once);
    }

    [Fact]
    public async Task SellAsync_WithSufficientQuantity_CreatesSellAtCurrentPrice()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = CreateSecurity(130m);
        var portfolioRepository = PortfolioRepositoryReturning(portfolio);
        var transactionRepository = TransactionRepositoryReturning([
            Transaction.CreateBuy(portfolio.Id, security.Id, 5m, 100m, DateTime.UtcNow)]);
        var service = CreateService(
            portfolioRepository,
            transactionRepository,
            SecurityRepositoryReturning(security));

        var response = await service.SellAsync(new SellTransactionRequest
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            Quantity = 2m
        }, Token);

        Assert.Equal(TransactionType.Sell, response.Type);
        Assert.Equal(2m, response.Quantity);
        Assert.Equal(130m, response.Price);
        Assert.Equal(260m, response.Amount);
        transactionRepository.Verify(x => x.AddAsync(
            It.Is<Transaction>(t => t.Id == response.Id), Token), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_WhenPortfolioDoesNotExist_ThrowsAndDoesNotCreateTransaction()
    {
        var id = Guid.NewGuid();
        var portfolioRepository = PortfolioRepositoryReturning(null, id);
        var transactionRepository = new Mock<ITransactionRepository>();
        var service = CreateService(portfolioRepository, transactionRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DepositAsync(new DepositTransactionRequest
            {
                PortfolioId = id,
                Amount = 1_000m
            }, Token));

        Assert.Equal("Portfolio not found.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
    }

    [Fact]
    public async Task BuyAsync_WhenSecurityDoesNotExist_ThrowsAndDoesNotCreateTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var transactionRepository = new Mock<ITransactionRepository>();
        var securityRepository = SecurityRepositoryReturning(null, Guid.NewGuid());
        var service = CreateService(
            PortfolioRepositoryReturning(portfolio), transactionRepository, securityRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuyAsync(new BuyTransactionRequest
            {
                PortfolioId = portfolio.Id,
                SecurityId = Guid.NewGuid(),
                Quantity = 1m
            }, Token));

        Assert.Equal("Security not found.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
    }

    [Fact]
    public async Task BuyAsync_WhenSecurityPriceIsUnavailable_ThrowsAndDoesNotCreateTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = CreateSecurity();
        var transactionRepository = new Mock<ITransactionRepository>();
        var service = CreateService(
            PortfolioRepositoryReturning(portfolio),
            transactionRepository,
            SecurityRepositoryReturning(security));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuyAsync(new BuyTransactionRequest
            {
                PortfolioId = portfolio.Id,
                SecurityId = security.Id,
                Quantity = 1m
            }, Token));

        Assert.Equal("Security price is not available.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
    }

    [Fact]
    public async Task WithdrawalAsync_WhenAmountExceedsCashBalance_ThrowsAndDoesNotCreateTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var transactionRepository = TransactionRepositoryReturning(
            [Transaction.CreateDeposit(portfolio.Id, 1_000m, DateTime.UtcNow)]);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactionRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.WithdrawalAsync(new WithdrawalTransactionRequest
            {
                PortfolioId = portfolio.Id,
                Amount = 1_001m
            }, Token));

        Assert.Equal("Insufficient cash balance.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
    }

    [Fact]
    public async Task SellAsync_WhenQuantityExceedsPosition_ThrowsAndDoesNotCreateTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP);
        var security = CreateSecurity(100m);
        var transactionRepository = TransactionRepositoryReturning([
            Transaction.CreateBuy(portfolio.Id, security.Id, 5m, 100m, DateTime.UtcNow)]);
        var service = CreateService(
            PortfolioRepositoryReturning(portfolio),
            transactionRepository,
            SecurityRepositoryReturning(security));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SellAsync(new SellTransactionRequest
            {
                PortfolioId = portfolio.Id,
                SecurityId = security.Id,
                Quantity = 6m
            }, Token));

        Assert.Equal("Insufficient security quantity.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
    }

    private static TransactionService CreateService(
        Mock<IPortfolioRepository> portfolioRepository,
        Mock<ITransactionRepository> transactionRepository,
        Mock<ISecurityRepository>? securityRepository = null,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        return new TransactionService(
            portfolioRepository.Object,
            transactionRepository.Object,
            (securityRepository ?? new Mock<ISecurityRepository>()).Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object);
    }

    private static Mock<IPortfolioRepository> PortfolioRepositoryReturning(
        Portfolio? portfolio,
        Guid? id = null)
    {
        var repository = new Mock<IPortfolioRepository>();
        repository.Setup(x => x.GetByIdAsync(id ?? portfolio!.Id, Token)).ReturnsAsync(portfolio);
        return repository;
    }

    private static Mock<ITransactionRepository> TransactionRepositoryReturning(
        IReadOnlyCollection<Transaction> transactions)
    {
        var repository = new Mock<ITransactionRepository>();
        repository.Setup(x => x.GetByPortfolioIdAsync(It.IsAny<Guid>(), Token))
            .ReturnsAsync(transactions);
        return repository;
    }

    private static Mock<ISecurityRepository> SecurityRepositoryReturning(
        Security? security,
        Guid? id = null)
    {
        var repository = new Mock<ISecurityRepository>();
        repository.Setup(x => x.GetByIdAsync(id ?? security!.Id, Token)).ReturnsAsync(security);
        return repository;
    }

    private static Security CreateSecurity(decimal? price = null)
    {
        var security = new Security(
            "ACME", "Acme Corporation", SecurityType.Stock, PortfolioCurrency.GBP);
        if (price.HasValue)
        {
            security.UpdatePrice(price.Value);
        }
        return security;
    }

    private static void VerifyNoTransactionAdded(Mock<ITransactionRepository> repository)
    {
        repository.Verify(
            x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

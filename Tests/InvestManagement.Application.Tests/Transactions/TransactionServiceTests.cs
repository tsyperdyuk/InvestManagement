using InvestManagement.Application.Common.Exceptions;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Contracts;
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
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task DepositAsync_WithValidRequest_QueuesOperationBeforeSaving()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var portfolioRepository = PortfolioRepositoryReturning(portfolio);
        var transactionRepository = new Mock<ITransactionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var outboxWriter = new Mock<IOutboxWriter>();
        var operationRepository = new Mock<ITransactionOperationRepository>();
        TransactionOperation? operation = null;
        ProcessTransaction? command = null;
        operationRepository.Setup(x => x.Add(It.IsAny<TransactionOperation>()))
            .Callback<TransactionOperation>(value => operation = value);
        outboxWriter.Setup(x => x.Add(It.IsAny<ProcessTransaction>()))
            .Callback<ProcessTransaction>(value => command = value);
        unitOfWork.Setup(x => x.SaveChangesAsync(Token))
            .Callback(() =>
            {
                Assert.NotNull(operation);
                Assert.NotNull(command);
                Assert.Equal(operation.Id, command.OperationId);
            })
            .ReturnsAsync(2);
        var service = CreateService(portfolioRepository, transactionRepository,
            unitOfWork: unitOfWork, outboxWriter: outboxWriter,
            operationRepository: operationRepository);

        var response = await service.DepositAsync(new DepositTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 1_000m
        }, Token);

        Assert.NotNull(operation);
        Assert.NotEqual(Guid.Empty, response.OperationId);
        Assert.Equal(operation.Id, response.OperationId);
        Assert.Equal(TransactionOperationStatus.Pending, response.Status);
        Assert.Equal(TransactionOperationStatus.Pending, operation.Status);
        Assert.Equal(portfolio.Id, operation.PortfolioId);
        Assert.Equal(UserId, operation.UserId);
        Assert.Equal(TransactionType.Deposit, operation.Type);
        Assert.Equal(1_000m, operation.Amount);
        Assert.Null(operation.TransactionId);
        operationRepository.Verify(x => x.Add(It.IsAny<TransactionOperation>()), Times.Once);
        outboxWriter.Verify(x => x.Add(It.IsAny<ProcessTransaction>()), Times.Once);
        outboxWriter.VerifyNoOtherCalls();
        transactionRepository.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task DepositAsync_WithNonPositiveAmount_DoesNotQueueOrSave(decimal amount)
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var transactions = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxWriter>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactions,
            unitOfWork: unitOfWork, outboxWriter: outbox, operationRepository: operations);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DepositAsync(
            new DepositTransactionRequest { PortfolioId = portfolio.Id, Amount = amount }, Token));

        transactions.VerifyNoOtherCalls();
        operations.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WithdrawalAsync_QueuesOperationAndCommandBeforeSaving()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var portfolioRepository = PortfolioRepositoryReturning(portfolio);
        var transactionRepository = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>();
        var outbox = new Mock<IOutboxWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TransactionOperation? operation = null;
        ProcessTransaction? command = null;
        operations.Setup(x => x.Add(It.IsAny<TransactionOperation>()))
            .Callback<TransactionOperation>(value => operation = value);
        outbox.Setup(x => x.Add(It.IsAny<ProcessTransaction>()))
            .Callback<ProcessTransaction>(value => command = value);
        unitOfWork.Setup(x => x.SaveChangesAsync(Token)).Callback(() =>
        {
            Assert.NotNull(operation);
            Assert.NotNull(command);
            Assert.Equal(operation.Id, command.OperationId);
        }).ReturnsAsync(2);
        var service = CreateService(portfolioRepository, transactionRepository,
            unitOfWork: unitOfWork, outboxWriter: outbox, operationRepository: operations);

        var response = await service.WithdrawalAsync(new WithdrawalTransactionRequest
        {
            PortfolioId = portfolio.Id,
            Amount = 250m
        }, Token);

        Assert.NotNull(operation);
        Assert.Equal(operation.Id, response.OperationId);
        Assert.Equal(TransactionOperationStatus.Pending, response.Status);
        Assert.Equal(TransactionType.Withdrawal, operation.Type);
        Assert.Equal(250m, operation.Amount);
        Assert.Equal(UserId, operation.UserId);
        Assert.Equal(portfolio.Id, operation.PortfolioId);
        transactionRepository.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(Token), Times.Once);
    }

    [Fact]
    public async Task BuyAsync_QueuesOperationAndCommandBeforeSaving()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var security = CreateSecurity();
        var transactions = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>();
        var outbox = new Mock<IOutboxWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TransactionOperation? operation = null;
        ProcessTransaction? command = null;
        operations.Setup(x => x.Add(It.IsAny<TransactionOperation>()))
            .Callback<TransactionOperation>(value => operation = value);
        outbox.Setup(x => x.Add(It.IsAny<ProcessTransaction>()))
            .Callback<ProcessTransaction>(value => command = value);
        unitOfWork.Setup(x => x.SaveChangesAsync(Token)).Callback(() =>
        {
            Assert.NotNull(operation);
            Assert.NotNull(command);
            Assert.Equal(operation.Id, command.OperationId);
        }).ReturnsAsync(2);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactions,
            SecurityRepositoryReturning(security), unitOfWork, outbox, operations);

        var response = await service.BuyAsync(new BuyTransactionRequest
        {
            PortfolioId = portfolio.Id, SecurityId = security.Id, Quantity = 4m
        }, Token);

        Assert.NotNull(operation);
        Assert.Equal(operation.Id, response.OperationId);
        Assert.Equal(TransactionOperationStatus.Pending, response.Status);
        Assert.Equal(TransactionType.Buy, operation.Type);
        Assert.Equal(security.Id, operation.SecurityId);
        Assert.Equal(4m, operation.Quantity);
        Assert.Equal(UserId, operation.UserId);
        Assert.Equal(portfolio.Id, operation.PortfolioId);
        Assert.Null(operation.TransactionId);
        transactions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(Token), Times.Once);
    }
    [Fact]
    public async Task SellAsync_QueuesOperationAndCommandBeforeSaving()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var security = CreateSecurity();
        var transactions = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>();
        var outbox = new Mock<IOutboxWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TransactionOperation? operation = null;
        ProcessTransaction? command = null;
        operations.Setup(x => x.Add(It.IsAny<TransactionOperation>()))
            .Callback<TransactionOperation>(value => operation = value);
        outbox.Setup(x => x.Add(It.IsAny<ProcessTransaction>()))
            .Callback<ProcessTransaction>(value => command = value);
        unitOfWork.Setup(x => x.SaveChangesAsync(Token)).Callback(() =>
        {
            Assert.NotNull(operation);
            Assert.NotNull(command);
            Assert.Equal(operation.Id, command.OperationId);
        }).ReturnsAsync(2);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactions,
            SecurityRepositoryReturning(security), unitOfWork, outbox, operations);

        var response = await service.SellAsync(new SellTransactionRequest
        {
            PortfolioId = portfolio.Id, SecurityId = security.Id, Quantity = 4m
        }, Token);

        Assert.NotNull(operation);
        Assert.Equal(operation.Id, response.OperationId);
        Assert.Equal(TransactionOperationStatus.Pending, response.Status);
        Assert.Equal(TransactionType.Sell, operation.Type);
        Assert.Equal(security.Id, operation.SecurityId);
        Assert.Equal(4m, operation.Quantity);
        Assert.Equal(UserId, operation.UserId);
        Assert.Equal(portfolio.Id, operation.PortfolioId);
        Assert.Null(operation.TransactionId);
        transactions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(Token), Times.Once);
    }
    [Fact]
    public async Task DepositAsync_WhenPortfolioDoesNotExist_ThrowsAndDoesNotCreateTransaction()
    {
        var id = Guid.NewGuid();
        var portfolioRepository = PortfolioRepositoryReturning(null, id);
        var transactionRepository = new Mock<ITransactionRepository>();
        var outboxWriter = new Mock<IOutboxWriter>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = CreateService(portfolioRepository, transactionRepository,
            unitOfWork: unitOfWork, outboxWriter: outboxWriter);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DepositAsync(new DepositTransactionRequest
            {
                PortfolioId = id,
                Amount = 1_000m
            }, Token));

        Assert.Equal("Portfolio not found.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
        outboxWriter.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuyAsync_WhenSecurityDoesNotExist_ThrowsAndDoesNotCreateTransaction()
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var transactionRepository = new Mock<ITransactionRepository>();
        var securityRepository = SecurityRepositoryReturning(null, Guid.NewGuid());
        var service = CreateService(
            PortfolioRepositoryReturning(portfolio), transactionRepository, securityRepository);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.BuyAsync(new BuyTransactionRequest
            {
                PortfolioId = portfolio.Id,
                SecurityId = Guid.NewGuid(),
                Quantity = 1m
            }, Token));

        Assert.Equal("Security not found.", exception.Message);
        VerifyNoTransactionAdded(transactionRepository);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task BuyAsync_WithInvalidQuantity_DoesNotQueueOrSave(decimal quantity)
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var transactions = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxWriter>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactions,
            unitOfWork: unitOfWork, outboxWriter: outbox, operationRepository: operations);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.BuyAsync(
            new BuyTransactionRequest
            {
                PortfolioId = portfolio.Id, SecurityId = Guid.NewGuid(), Quantity = quantity
            }, Token));

        operations.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
        transactions.VerifyNoOtherCalls();
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task WithdrawalAsync_WithInvalidAmount_DoesNotQueueOrSave(decimal amount)
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var transactionRepository = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxWriter>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactionRepository,
            unitOfWork: unitOfWork, outboxWriter: outbox, operationRepository: operations);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.WithdrawalAsync(new WithdrawalTransactionRequest
            {
                PortfolioId = portfolio.Id,
                Amount = amount
            }, Token));

        transactionRepository.VerifyNoOtherCalls();
        operations.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SellAsync_WithInvalidQuantity_DoesNotQueueOrSave(decimal quantity)
    {
        var portfolio = new Portfolio("Main portfolio", PortfolioCurrency.GBP, UserId);
        var transactions = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var operations = new Mock<ITransactionOperationRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxWriter>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = CreateService(PortfolioRepositoryReturning(portfolio), transactions,
            unitOfWork: unitOfWork, outboxWriter: outbox, operationRepository: operations);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SellAsync(
            new SellTransactionRequest
            {
                PortfolioId = portfolio.Id, SecurityId = Guid.NewGuid(), Quantity = quantity
            }, Token));

        operations.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
        transactions.VerifyNoOtherCalls();
    }
    private static TransactionService CreateService(
        Mock<IPortfolioRepository> portfolioRepository,
        Mock<ITransactionRepository> transactionRepository,
        Mock<ISecurityRepository>? securityRepository = null,
        Mock<IUnitOfWork>? unitOfWork = null,
        Mock<IOutboxWriter>? outboxWriter = null,
        Mock<ITransactionOperationRepository>? operationRepository = null)
    {
        return new TransactionService(
            portfolioRepository.Object,
            transactionRepository.Object,
            (securityRepository ?? new Mock<ISecurityRepository>()).Object,
            (operationRepository ?? new Mock<ITransactionOperationRepository>()).Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
            Mock.Of<ICurrentUser>(x => x.UserId == UserId),
            (outboxWriter ?? new Mock<IOutboxWriter>()).Object);
    }

    private static Mock<IPortfolioRepository> PortfolioRepositoryReturning(
        Portfolio? portfolio,
        Guid? id = null)
    {
        var repository = new Mock<IPortfolioRepository>();
        repository.Setup(x => x.GetByIdAsync(id ?? portfolio!.Id, UserId, Token)).ReturnsAsync(portfolio);
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

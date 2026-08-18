using FluentAssertions;
using InvestManagement.Api.Controllers;
using InvestManagement.Api.Models.Transactions;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Requests;
using InvestManagement.Application.Transactions.Models.Responses;
using InvestManagement.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvestManagement.Api.Tests.Controllers;

public sealed class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _serviceMock = new();
    private readonly TransactionsController _controller;

    public TransactionsControllerTests() =>
        _controller = new TransactionsController(_serviceMock.Object);

    [Fact]
    public async Task Deposit_ShouldReturnCreated_WithMappedTransaction()
    {
        // Arrange
        var portfolioId = Guid.NewGuid();
        var request = new DepositRequest(500m);
        var transaction = CreateTransaction(portfolioId, TransactionType.Deposit, 500m);
        _serviceMock.Setup(x => x.DepositAsync(
                It.Is<DepositTransactionRequest>(r =>
                    r.PortfolioId == portfolioId && r.Amount == request.Amount),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Deposit(portfolioId, request, CancellationToken.None);

        // Assert
        AssertCreated(result, transaction);
    }

    [Fact]
    public async Task Withdraw_ShouldReturnCreated_WithMappedTransaction()
    {
        // Arrange
        var portfolioId = Guid.NewGuid();
        var request = new WithdrawalRequest(200m);
        var transaction = CreateTransaction(portfolioId, TransactionType.Withdrawal, 200m);
        _serviceMock.Setup(x => x.WithdrawalAsync(
                It.Is<WithdrawalTransactionRequest>(r =>
                    r.PortfolioId == portfolioId && r.Amount == request.Amount),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Withdraw(portfolioId, request, CancellationToken.None);

        // Assert
        AssertCreated(result, transaction);
    }

    [Fact]
    public async Task Buy_ShouldReturnCreated_WithMappedTransaction()
    {
        // Arrange
        var portfolioId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var request = new BuyRequest(securityId, 3m);
        var transaction = CreateTransaction(portfolioId, TransactionType.Buy, 300m, securityId);
        _serviceMock.Setup(x => x.BuyAsync(
                It.Is<BuyTransactionRequest>(r => r.PortfolioId == portfolioId &&
                    r.SecurityId == securityId && r.Quantity == request.Quantity),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Buy(portfolioId, request, CancellationToken.None);

        // Assert
        AssertCreated(result, transaction);
    }

    [Fact]
    public async Task Sell_ShouldReturnCreated_WithMappedTransaction()
    {
        // Arrange
        var portfolioId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var request = new SellRequest(securityId, 2m);
        var transaction = CreateTransaction(portfolioId, TransactionType.Sell, 200m, securityId);
        _serviceMock.Setup(x => x.SellAsync(
                It.Is<SellTransactionRequest>(r => r.PortfolioId == portfolioId &&
                    r.SecurityId == securityId && r.Quantity == request.Quantity),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Sell(portfolioId, request, CancellationToken.None);

        // Assert
        AssertCreated(result, transaction);
    }

    private static void AssertCreated(IActionResult result, TransactionResponse transaction)
    {
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeSameAs(transaction);
    }

    private static TransactionResponse CreateTransaction(
        Guid portfolioId, TransactionType type, decimal amount, Guid? securityId = null) =>
        new(Guid.NewGuid(), portfolioId, securityId, type, amount, null, null, DateTime.UtcNow);
}

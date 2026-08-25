using InvestManagement.Domain.Model;
using Xunit;

namespace InvestManagement.Application.Tests.Domain;

public sealed class TransactionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDeposit_WithNonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Transaction.CreateDeposit(
                Guid.NewGuid(),
                amount,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateWithdrawal_WithNonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Transaction.CreateWithdrawal(
                Guid.NewGuid(),
                amount,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateBuy_WithNonPositiveQuantity_Throws(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Transaction.CreateBuy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                quantity,
                100m,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateSell_WithNonPositivePrice_Throws(decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Transaction.CreateSell(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1m,
                price,
                DateTime.UtcNow));
    }

    [Fact]
    public void CreateDeposit_WithEmptyPortfolioId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Transaction.CreateDeposit(
                Guid.Empty,
                100m,
                DateTime.UtcNow));
    }

    [Fact]
    public void CreateBuy_WithEmptySecurityId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Transaction.CreateBuy(
                Guid.NewGuid(),
                Guid.Empty,
                1m,
                100m,
                DateTime.UtcNow));
    }
}

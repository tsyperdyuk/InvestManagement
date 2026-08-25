using InvestManagement.Domain.Model;
using Xunit;

namespace InvestManagement.Application.Tests.Domain;

public sealed class SecurityTests
{
    [Fact]
    public void UpdatePrice_ForFirstPrice_SetsCurrentPriceWithoutPreviousPrice()
    {
        var security = CreateSecurity();

        security.UpdatePrice(125m);

        Assert.Equal(125m, security.CurrentPrice);
        Assert.Null(security.PreviousPrice);
    }

    [Fact]
    public void UpdatePrice_ForSubsequentPrice_MovesCurrentPriceToPreviousPrice()
    {
        var security = CreateSecurity();
        security.UpdatePrice(125m);

        security.UpdatePrice(130m);

        Assert.Equal(130m, security.CurrentPrice);
        Assert.Equal(125m, security.PreviousPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdatePrice_WithNonPositivePrice_ThrowsAndKeepsExistingPrice(
        decimal invalidPrice)
    {
        var security = CreateSecurity();
        security.UpdatePrice(125m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => security.UpdatePrice(invalidPrice));

        Assert.Equal(125m, security.CurrentPrice);
        Assert.Null(security.PreviousPrice);
    }

    private static Security CreateSecurity()
    {
        return new Security(
            "ACME",
            "Acme Corporation",
            SecurityType.Stock,
            PortfolioCurrency.GBP);
    }
}

using FluentAssertions;
using InvestManagement.Api.Controllers;
using InvestManagement.Application.MarketData.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvestManagement.Api.Tests.Controllers;

public sealed class MarketDataControllerTests
{
    [Fact]
    public async Task UpdatePrices_ShouldReturnNoContent_WhenPricesAreUpdated()
    {
        // Arrange
        var serviceMock = new Mock<IMarketDataService>();
        var controller = new MarketDataController();

        // Act
        var result = await controller.UpdatePrices(
            serviceMock.Object, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        serviceMock.Verify(
            x => x.UpdateSecurityPricesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

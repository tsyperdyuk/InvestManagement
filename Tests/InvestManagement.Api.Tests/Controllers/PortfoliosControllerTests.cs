using FluentAssertions;
using InvestManagement.Api.Controllers;
using InvestManagement.Application.Common;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Portfolios.Requests;
using InvestManagement.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvestManagement.Api.Tests.Controllers;

public sealed class PortfoliosControllerTests
{
    private readonly Mock<IPortfolioService> _portfolioServiceMock = new();
    private readonly Mock<IPortfolioSummaryService> _summaryServiceMock = new();
    private readonly PortfoliosController _controller;

    public PortfoliosControllerTests()
    {
        _controller = new PortfoliosController(
            _portfolioServiceMock.Object,
            _summaryServiceMock.Object);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenPortfolioIsCreated()
    {
        // Arrange
        var request = new CreatePortfolioRequest
        {
            Name = "Test Portfolio",
            Currency = PortfolioCurrency.USD
        };
        var portfolio = CreatePortfolio();
        _portfolioServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should()
            .BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PortfoliosController.GetById));
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(portfolio.Id);
        createdResult.Value.Should().BeSameAs(portfolio);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithPagedPortfolios()
    {
        // Arrange
        var pagination = new PaginationRequest { PageNumber = 2, PageSize = 10 };
        var pagedResult = new PagedResult<PortfolioModel>
        {
            Items = [CreatePortfolio()], PageNumber = 2, PageSize = 10, TotalCount = 11
        };
        _portfolioServiceMock
            .Setup(x => x.GetAllAsync(pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(pagination, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(pagedResult);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenPortfolioExists()
    {
        // Arrange
        var portfolio = CreatePortfolio();
        _portfolioServiceMock
            .Setup(x => x.GetByIdAsync(portfolio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        // Act
        var result = await _controller.GetById(portfolio.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(portfolio);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenPortfolioDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _portfolioServiceMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioModel?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Rename_ShouldReturnExpectedResult(bool renamed)
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new RenamePortfolioRequest { Name = "Renamed" };
        _portfolioServiceMock
            .Setup(x => x.RenameAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renamed);

        // Act
        var result = await _controller.Rename(id, request, CancellationToken.None);

        // Assert
        if (renamed)
            result.Should().BeOfType<NoContentResult>();
        else
            result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldReturnExpectedResult(bool deleted)
    {
        // Arrange
        var id = Guid.NewGuid();
        _portfolioServiceMock
            .Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        if (deleted)
            result.Should().BeOfType<NoContentResult>();
        else
            result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDetails_ShouldReturnOk_WhenSummaryExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var summary = new PortfolioSummary
        {
            PortfolioId = id,
            PortfolioName = "Test Portfolio",
            Currency = PortfolioCurrency.USD
        };
        _summaryServiceMock
            .Setup(x => x.GetDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetDetails(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task GetDetails_ShouldReturnNotFound_WhenSummaryDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _summaryServiceMock
            .Setup(x => x.GetDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioSummary?)null);

        // Act
        var result = await _controller.GetDetails(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    private static PortfolioModel CreatePortfolio() => new()
    {
        Id = Guid.NewGuid(), Name = "Test Portfolio", Currency = PortfolioCurrency.USD
    };
}

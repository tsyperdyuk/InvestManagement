using InvestManagement.Application.Common;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Requests;
using InvestManagement.Application.Portfolios.Services;
using InvestManagement.Domain.Model;
using Moq;
using Xunit;

namespace InvestManagement.Application.Tests.Portfolios;

public sealed class PortfolioServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly CancellationToken Token = CancellationToken.None;

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPortfolio()
    {
        var repository = new Mock<IPortfolioRepository>();
        Portfolio? added = null;
        repository.Setup(x => x.AddAsync(It.IsAny<Portfolio>(), Token))
            .Callback<Portfolio, CancellationToken>((portfolio, _) => added = portfolio)
            .ReturnsAsync((Portfolio portfolio, CancellationToken _) => portfolio);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var result = await service.CreateAsync(new CreatePortfolioRequest
        {
            Name = "Retirement",
            Currency = PortfolioCurrency.GBP
        }, Token);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Retirement", result.Name);
        Assert.Equal(PortfolioCurrency.GBP, result.Currency);
        Assert.Equal(result.Id, added?.Id);
        Assert.Equal(UserId, added?.UserId);
        repository.Verify(x => x.AddAsync(It.IsAny<Portfolio>(), Token), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPortfolioExists_ReturnsPortfolio()
    {
        var portfolio = new Portfolio("Retirement", PortfolioCurrency.GBP, UserId);
        var repository = PortfolioRepositoryReturning(portfolio);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var result = await service.GetByIdAsync(portfolio.Id, Token);

        Assert.NotNull(result);
        Assert.Equal(portfolio.Id, result.Id);
        Assert.Equal("Retirement", result.Name);
        Assert.Equal(PortfolioCurrency.GBP, result.Currency);
    }

    [Fact]
    public async Task RenameAsync_WhenPortfolioExists_UpdatesName()
    {
        var portfolio = new Portfolio("Old name", PortfolioCurrency.GBP, UserId);
        var repository = PortfolioRepositoryReturning(portfolio);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var renamed = await service.RenameAsync(
            portfolio.Id,
            new RenamePortfolioRequest { Name = "New name" },
            Token);

        Assert.True(renamed);
        Assert.Equal("New name", portfolio.Name);
        repository.Verify(x => x.UpdateAsync(portfolio, Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenPortfolioExists_RemovesPortfolio()
    {
        var portfolio = new Portfolio("Retirement", PortfolioCurrency.GBP, UserId);
        var repository = PortfolioRepositoryReturning(portfolio);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var deleted = await service.DeleteAsync(portfolio.Id, Token);

        Assert.True(deleted);
        repository.Verify(x => x.DeleteAsync(portfolio, Token), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsRequestedPageAndMetadata()
    {
        var portfolios = Enumerable.Range(1, 2)
            .Select(index => new Portfolio($"Portfolio {index}", PortfolioCurrency.GBP, UserId))
            .ToList();
        var repository = new Mock<IPortfolioRepository>();
        repository.Setup(x => x.GetPagedAsync(2, 2, UserId, Token)).ReturnsAsync(portfolios);
        repository.Setup(x => x.CountAsync(UserId, Token)).ReturnsAsync(5);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var result = await service.GetAllAsync(
            new PaginationRequest { PageNumber = 2, PageSize = 2 }, Token);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPortfolioDoesNotExist_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repository = PortfolioRepositoryReturning(null, id);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var result = await service.GetByIdAsync(id, Token);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenameAsync_WhenPortfolioDoesNotExist_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var repository = PortfolioRepositoryReturning(null, id);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var result = await service.RenameAsync(
            id,
            new RenamePortfolioRequest { Name = "New name" }, Token);

        Assert.False(result);
        repository.Verify(
            x => x.UpdateAsync(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenPortfolioDoesNotExist_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var repository = PortfolioRepositoryReturning(null, id);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>(x => x.UserId == UserId));

        var result = await service.DeleteAsync(id, Token);

        Assert.False(result);
        repository.Verify(
            x => x.DeleteAsync(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void PaginationRequest_WhenPageSizeExceedsMaximum_UsesMaximumPageSize()
    {
        var pagination = new PaginationRequest { PageSize = 500 };

        Assert.Equal(100, pagination.PageSize);
    }

    [Fact]
    public async Task CreateAsync_WithoutCurrentUser_DoesNotPersistPortfolio()
    {
        var repository = new Mock<IPortfolioRepository>(MockBehavior.Strict);
        var service = new PortfolioService(repository.Object, Mock.Of<ICurrentUser>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreatePortfolioRequest { Name = "Retirement", Currency = PortfolioCurrency.GBP }, Token));

        repository.VerifyNoOtherCalls();
    }

    private static Mock<IPortfolioRepository> PortfolioRepositoryReturning(
        Portfolio? portfolio,
        Guid? id = null)
    {
        var repository = new Mock<IPortfolioRepository>();
        repository.Setup(x => x.GetByIdAsync(id ?? portfolio!.Id, UserId, Token))
            .ReturnsAsync(portfolio);
        return repository;
    }
}

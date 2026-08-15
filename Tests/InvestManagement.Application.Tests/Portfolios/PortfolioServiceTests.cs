using InvestManagement.Application.Common;
using InvestManagement.Application.Portfolios.Requests;
using InvestManagement.Application.Portfolios.Services;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvestManagement.Application.Tests.Portfolios;

public sealed class PortfolioServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPortfolio()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = new CreatePortfolioRequest
        {
            Name = "Retirement",
            Currency = PortfolioCurrency.GBP
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Retirement", result.Name);
        Assert.Equal(PortfolioCurrency.GBP, result.Currency);

        var portfolio = await dbContext.Portfolios.SingleAsync();
        Assert.Equal(result.Id, portfolio.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPortfolioExists_ReturnsPortfolio()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Retirement", PortfolioCurrency.GBP);
        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(portfolio.Id, result.Id);
        Assert.Equal("Retirement", result.Name);
        Assert.Equal(PortfolioCurrency.GBP, result.Currency);
    }

    [Fact]
    public async Task RenameAsync_WhenPortfolioExists_UpdatesName()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Old name", PortfolioCurrency.GBP);
        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var renamed = await service.RenameAsync(
            portfolio.Id,
            new RenamePortfolioRequest { Name = "New name" },
            CancellationToken.None);

        Assert.True(renamed);
        Assert.Equal(
            "New name",
            (await dbContext.Portfolios.SingleAsync()).Name);
    }

    [Fact]
    public async Task DeleteAsync_WhenPortfolioExists_RemovesPortfolio()
    {
        await using var dbContext = CreateDbContext();
        var portfolio = new Portfolio("Retirement", PortfolioCurrency.GBP);
        dbContext.Portfolios.Add(portfolio);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var deleted = await service.DeleteAsync(
            portfolio.Id,
            CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(await dbContext.Portfolios.ToListAsync());
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsRequestedPageAndMetadata()
    {
        await using var dbContext = CreateDbContext();
        for (var index = 1; index <= 5; index++)
        {
            dbContext.Portfolios.Add(
                new Portfolio($"Portfolio {index}", PortfolioCurrency.GBP));
        }

        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var pagination = new PaginationRequest
        {
            PageNumber = 2,
            PageSize = 2
        };

        var result = await service.GetAllAsync(
            pagination,
            CancellationToken.None);

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
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenameAsync_WhenPortfolioDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.RenameAsync(
            Guid.NewGuid(),
            new RenamePortfolioRequest { Name = "New name" },
            CancellationToken.None);

        Assert.False(result);
        Assert.Empty(await dbContext.Portfolios.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_WhenPortfolioDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result);
        Assert.Empty(await dbContext.Portfolios.ToListAsync());
    }

    [Fact]
    public void PaginationRequest_WhenPageSizeExceedsMaximum_UsesMaximumPageSize()
    {
        var pagination = new PaginationRequest
        {
            PageSize = 500
        };

        Assert.Equal(100, pagination.PageSize);
    }

    private static PortfolioService CreateService(
        InvestManagementDbContext dbContext)
    {
        return new PortfolioService(new PortfolioRepository(dbContext));
    }

    private static InvestManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InvestManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InvestManagementDbContext(options);
    }
}

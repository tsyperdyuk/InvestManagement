using InvestManagement.Application.Common;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Mappings;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Portfolios.Requests;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ICurrentUser _currentUser;

    public PortfolioService(IPortfolioRepository portfolioRepository,ICurrentUser currentUser)
    {
        _portfolioRepository = portfolioRepository;
        _currentUser = currentUser;
    }

    public async Task<PortfolioModel> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var portfolio = new Portfolio(request.Name, request.Currency, userId);

        var createdPortfolio = await _portfolioRepository.AddAsync(
            portfolio,
            cancellationToken);

        return createdPortfolio.ToResponse();
    }

    public async Task<PagedResult<PortfolioModel>> GetAllAsync(
    PaginationRequest pagination,
    CancellationToken cancellationToken)
    {
        var portfolios = await _portfolioRepository.GetPagedAsync(
            pagination.PageNumber,
            pagination.PageSize,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        var totalCount = await _portfolioRepository.CountAsync(
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        var items = portfolios
            .Select(portfolio => portfolio.ToResponse())
            .ToList();

        return new PagedResult<PortfolioModel>
        {
            Items = items,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PortfolioModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id, _currentUser.GetRequiredUserId(), cancellationToken);

        return portfolio?.ToResponse();
    }

    public async Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id, _currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            return false;
        }

        portfolio.Name = request.Name;

        await _portfolioRepository.UpdateAsync(portfolio, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id, _currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            return false;
        }

        await _portfolioRepository.DeleteAsync(portfolio, cancellationToken);

        return true;
    }
}

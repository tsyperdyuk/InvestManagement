using InvestManagement.Domain.Model;
using InvestManagement.Application.Interfaces;
using InvestManagement.Application.Models;
using InvestManagement.Application.Mappings;
using InvestManagement.Application.Responses;

namespace InvestManagement.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;

    public PortfolioService(IPortfolioRepository portfolioRepository)
    {
        _portfolioRepository = portfolioRepository;
    }

    public async Task<PortfolioResponse> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Currency = request.Currency
        };

        var createdPortfolio = await _portfolioRepository.AddAsync(
            portfolio,
            cancellationToken);

        return createdPortfolio.ToResponse();
    }

    public async Task<IReadOnlyList<PortfolioResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var portfolios = await _portfolioRepository.GetAllAsync(cancellationToken);

        return portfolios
            .Select(portfolio => portfolio.ToResponse())
            .ToList();
    }

    public async Task<PortfolioResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id, cancellationToken);

        return portfolio?.ToResponse();
    }

    public async Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id, cancellationToken);

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
        var portfolio = await _portfolioRepository.GetByIdAsync(id, cancellationToken);

        if (portfolio is null)
        {
            return false;
        }

        await _portfolioRepository.DeleteAsync(portfolio, cancellationToken);

        return true;
    }
}
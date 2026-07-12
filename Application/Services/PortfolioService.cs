using InvestManagement.Domain.Model;
using InvestManagement.Application.Interfaces;
using InvestManagement.Application.Models;

namespace InvestManagement.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;

    public PortfolioService(IPortfolioRepository portfolioRepository)
    {
        _portfolioRepository = portfolioRepository;
    }

    public async Task<Portfolio> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Currency = request.Currency
        };

        return await _portfolioRepository.AddAsync(portfolio, cancellationToken);
    }

    public async Task<IReadOnlyList<Portfolio>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _portfolioRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _portfolioRepository.GetByIdAsync(id, cancellationToken);
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
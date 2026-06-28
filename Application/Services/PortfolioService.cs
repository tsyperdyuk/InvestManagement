using InvestManagement.Domain.Model;
using InvestManagement.Application.Interfaces;
using InvestManagement.Application.Models;

namespace InvestManagement.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly List<Portfolio> _portfolios = [];

    public Task<Portfolio> CreateAsync(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        Portfolio portfolio = new()
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            Currency = request.Currency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _portfolios.Add(portfolio);

        return Task.FromResult(portfolio);
    }

    public Task<IReadOnlyList<Portfolio>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Portfolio> portfolios = _portfolios
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToList();

        return Task.FromResult(portfolios);
    }

    public Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = _portfolios
            .FirstOrDefault(p => p.Id == id);

        return Task.FromResult(portfolio);
    }

    public Task<bool> RenameAsync(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = _portfolios
            .FirstOrDefault(p => p.Id == id);

        if (portfolio is null)
        {
            return Task.FromResult(false);
        }

        portfolio.Name = request.Name;
        portfolio.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = _portfolios
            .FirstOrDefault(p => p.Id == id);

        if (portfolio is null)
        {
            return Task.FromResult(false);
        }

        _portfolios.Remove(portfolio);

        return Task.FromResult(true);
    }
}
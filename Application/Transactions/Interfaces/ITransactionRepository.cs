using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Transactions.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyCollection<Transaction>> GetByPortfolioIdAsync(Guid portfolioId, CancellationToken cancellationToken);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
}

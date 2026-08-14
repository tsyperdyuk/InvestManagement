using InvestManagement.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace InvestManagement.Application.Transactions.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyCollection<Transaction>> GetByPortfolioIdAsync(Guid portfolioId, CancellationToken cancellationToken);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
}

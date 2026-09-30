using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Transactions.Interfaces;

public interface ITransactionOperationRepository
{
    void Add(TransactionOperation operation);

    Task<TransactionOperation?> GetByIdAsync(
        Guid operationId,
        Guid userId,
        CancellationToken cancellationToken);
}

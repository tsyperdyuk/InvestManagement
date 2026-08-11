namespace InvestManagement.Application.Transactions.Interfaces;

public interface ITransactionService
{
    Task DepositAsync(Guid portfolioId, decimal amount, CancellationToken cancellationToken);

    Task WithdrawalAsync(Guid portfolioId, decimal amount, CancellationToken cancellationToken);

    Task BuyAsync(Guid portfolioId, Guid securityId, decimal quantity, CancellationToken cancellationToken);

    Task SellAsync(Guid portfolioId, Guid securityId, decimal quantity, CancellationToken cancellationToken);
}

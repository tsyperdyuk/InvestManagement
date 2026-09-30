using InvestManagement.Application.Transactions.Models.Requests;
using InvestManagement.Application.Transactions.Models.Responses;

namespace InvestManagement.Application.Transactions.Interfaces;

public interface ITransactionService
{
    Task<TransactionOperationResponse?> GetOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken);

    Task<TransactionOperationResponse> DepositAsync(DepositTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionOperationResponse> WithdrawalAsync(WithdrawalTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionOperationResponse> BuyAsync(BuyTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionOperationResponse> SellAsync(SellTransactionRequest request, CancellationToken cancellationToken);
}

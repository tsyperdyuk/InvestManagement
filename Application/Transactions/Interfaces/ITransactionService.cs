using InvestManagement.Application.Transactions.Models.Requests;
using InvestManagement.Application.Transactions.Models.Responses;

namespace InvestManagement.Application.Transactions.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponse> DepositAsync(DepositTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionResponse> WithdrawalAsync(WithdrawalTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionResponse> BuyAsync(BuyTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionResponse> SellAsync(SellTransactionRequest request, CancellationToken cancellationToken);
}
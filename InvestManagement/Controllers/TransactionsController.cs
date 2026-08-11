using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(Guid portfolioId, DepositRequest request, CancellationToken cancellationToken)
    {
        await _transactionService.DepositAsync(portfolioId, request.Amount, cancellationToken);

        return NoContent();
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(Guid portfolioId, WithdrawalRequest request, CancellationToken cancellationToken)
    {
        await _transactionService.WithdrawalAsync(portfolioId, request.Amount, cancellationToken);

        return NoContent();
    }

    [HttpPost("buy")]
    public async Task<IActionResult> Buy(Guid portfolioId, BuyRequest request, CancellationToken cancellationToken)
    {
        await _transactionService.BuyAsync(portfolioId, request.SecurityId, request.Quantity, cancellationToken);

        return NoContent();
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell(Guid portfolioId, SellRequest request, CancellationToken cancellationToken)
    {
        await _transactionService.SellAsync(portfolioId, request.SecurityId, request.Quantity, cancellationToken);

        return NoContent();
    }
}

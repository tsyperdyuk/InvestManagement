using Microsoft.AspNetCore.Authorization;
using InvestManagement.Api.Models.Transactions;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/portfolios/{portfolioId:guid}/transactions")]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(Guid portfolioId, [FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
        var serviceRequest = new DepositTransactionRequest
        {
            PortfolioId = portfolioId,
            Amount = request.Amount
        };

        var operation = await _transactionService.DepositAsync(serviceRequest, cancellationToken);

        return Accepted(operation);
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(Guid portfolioId, [FromBody] WithdrawalRequest request, CancellationToken cancellationToken)
    {
        var serviceRequest = new WithdrawalTransactionRequest
        {
            PortfolioId = portfolioId,
            Amount = request.Amount
        };

        var operation = await _transactionService.WithdrawalAsync(serviceRequest, cancellationToken);

        return Accepted(operation);
    }

    [HttpPost("buy")]
    public async Task<IActionResult> Buy(Guid portfolioId, [FromBody] BuyRequest request, CancellationToken cancellationToken)
    {
        var serviceRequest = new BuyTransactionRequest
        {
            PortfolioId = portfolioId,
            SecurityId = request.SecurityId,
            Quantity = request.Quantity
        };

        var operation = await _transactionService.BuyAsync(serviceRequest, cancellationToken);

        return Accepted(operation);
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell(Guid portfolioId, [FromBody] SellRequest request, CancellationToken cancellationToken)
    {
        var serviceRequest = new SellTransactionRequest
        {
            PortfolioId = portfolioId,
            SecurityId = request.SecurityId,
            Quantity = request.Quantity
        };

        var operation = await _transactionService.SellAsync(serviceRequest, cancellationToken);

        return Accepted(operation);
    }
}

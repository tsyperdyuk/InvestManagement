using InvestManagement.Api.Models.Transactions;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

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

        var transaction = await _transactionService.DepositAsync(serviceRequest, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, transaction);
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(Guid portfolioId, [FromBody] WithdrawalRequest request, CancellationToken cancellationToken)
    {
        var serviceRequest = new WithdrawalTransactionRequest
        {
            PortfolioId = portfolioId,
            Amount = request.Amount
        };

        var transaction = await _transactionService.WithdrawalAsync(serviceRequest, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, transaction);
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

        var transaction = await _transactionService.BuyAsync(serviceRequest, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, transaction);
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

        var transaction = await _transactionService.SellAsync(serviceRequest, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, transaction);
    }
}

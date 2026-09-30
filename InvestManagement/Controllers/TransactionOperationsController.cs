using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/transaction-operations")]
public sealed class TransactionOperationsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionOperationsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("{operationId:guid}")]
    public async Task<ActionResult<TransactionOperationResponse>> GetById(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var operation = await _transactionService.GetOperationAsync(
            operationId, cancellationToken);

        if (operation is null)
        {
            return NotFound();
        }

        return Ok(operation);
    }
}

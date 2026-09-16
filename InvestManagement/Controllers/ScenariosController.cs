using Microsoft.AspNetCore.Authorization;
using InvestManagement.Application.Scenarios.Interfaces;
using InvestManagement.Application.Scenarios.Models;
using InvestManagement.Application.Scenarios.Requests;
using InvestManagement.Application.Transactions.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public sealed class ScenariosController : ControllerBase
{
    private readonly IScenarioService _scenarioService;

    public ScenariosController(IScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    [HttpPost("portfolios/{portfolioId:guid}/scenarios")]
    public async Task<ActionResult<ScenarioModel>> Create(
        Guid portfolioId,
        CreateScenarioRequest request,
        CancellationToken cancellationToken)
    {
        var scenario = await _scenarioService.CreateAsync(
            portfolioId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { scenarioId = scenario.Id },
            scenario);
    }

    [HttpGet("portfolios/{portfolioId:guid}/scenarios")]
    public async Task<ActionResult<IReadOnlyList<ScenarioModel>>>
        GetByPortfolioId(
            Guid portfolioId,
            CancellationToken cancellationToken)
    {
        var scenarios = await _scenarioService.GetByPortfolioIdAsync(
            portfolioId,
            cancellationToken);

        return Ok(scenarios);
    }

    [HttpGet("scenarios/{scenarioId:guid}")]
    public async Task<ActionResult<ScenarioModel>> GetById(
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        var scenario = await _scenarioService.GetByIdAsync(
            scenarioId,
            cancellationToken);

        if (scenario is null)
        {
            return NotFound();
        }

        return Ok(scenario);
    }

    [HttpDelete("scenarios/{scenarioId:guid}")]
    public async Task<IActionResult> Delete(
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        var deleted = await _scenarioService.DeleteAsync(
            scenarioId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("scenarios/{scenarioId:guid}/operations")]
    public async Task<ActionResult<ScenarioOperationModel>> AddOperation(
        Guid scenarioId,
        AddScenarioOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = await _scenarioService.AddOperationAsync(
            scenarioId,
            request,
            cancellationToken);

        if (operation is null)
        {
            return NotFound();
        }

        return StatusCode(StatusCodes.Status201Created, operation);
    }

    [HttpDelete(
        "scenarios/{scenarioId:guid}/operations/{operationId:guid}")]
    public async Task<IActionResult> RemoveOperation(
        Guid scenarioId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var removed = await _scenarioService.RemoveOperationAsync(
            scenarioId,
            operationId,
            cancellationToken);

        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("scenarios/{scenarioId:guid}/preview")]
    public async Task<ActionResult<ScenarioPreviewModel>> Preview(
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        var preview = await _scenarioService.PreviewAsync(
            scenarioId,
            cancellationToken);

        if (preview is null)
        {
            return NotFound();
        }

        return Ok(preview);
    }

    [HttpPost("scenarios/{scenarioId:guid}/apply")]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> Apply(
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        var transactions = await _scenarioService.ApplyAsync(
            scenarioId,
            cancellationToken);

        if (transactions is null)
        {
            return NotFound();
        }

        return Ok(transactions);
    }
}

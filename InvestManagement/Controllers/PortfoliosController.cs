using InvestManagement.Application.Interfaces;
using InvestManagement.Application.Models;
using InvestManagement.Application.Responses;
using InvestManagement.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

[ApiController]
[Route("api/portfolios")]
public class PortfoliosController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfoliosController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioResponse>> Create(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = portfolio.Id },
            portfolio);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PortfolioResponse>>> GetAll([FromQuery] CancellationToken cancellationToken)
    {
        var portfolios = await _portfolioService.GetAllAsync(cancellationToken);

        return Ok(portfolios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PortfolioResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.GetByIdAsync(id, cancellationToken);

        if (portfolio is null)
        {
            return NotFound();
        }

        return Ok(portfolio);
    }

    [HttpPut("{id:guid}/rename")]
    public async Task<IActionResult> Rename(Guid id, RenamePortfolioRequest request, CancellationToken cancellationToken)
    {
        bool renamed = await _portfolioService.RenameAsync(id, request, cancellationToken);

        if (!renamed)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _portfolioService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

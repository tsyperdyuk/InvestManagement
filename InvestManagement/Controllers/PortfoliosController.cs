using InvestManagement.Domain.Model;
using InvestManagement.Application.Interfaces;
using InvestManagement.Application.Models;
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
    public async Task<ActionResult<Portfolio>> Create(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = portfolio.Id },
            portfolio);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Portfolio>>> GetAll([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Portfolio> portfolios = await _portfolioService.GetAllAsync(userId, cancellationToken);

        return Ok(portfolios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Portfolio>> GetById(Guid id, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = await _portfolioService.GetByIdAsync(id, cancellationToken);

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

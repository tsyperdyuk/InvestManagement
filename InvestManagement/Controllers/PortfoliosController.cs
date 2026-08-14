using InvestManagement.Application.Common;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Portfolios.Requests;
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
    public async Task<ActionResult<PortfolioModel>> Create(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = portfolio.Id },
            portfolio);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PortfolioModel>>> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _portfolioService.GetAllAsync(pagination, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PortfolioModel>> GetById(Guid id, CancellationToken cancellationToken)
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

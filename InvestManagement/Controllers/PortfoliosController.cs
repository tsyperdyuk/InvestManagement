using InvestManagement.Interfaces;
using InvestManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Controllers;

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
    public async Task<ActionResult<Portfolio>> Create(CreatePortfolioRequest request)
    {
        Portfolio portfolio = await _portfolioService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = portfolio.Id },
            portfolio);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Portfolio>>> GetAll([FromQuery] Guid userId)
    {
        IReadOnlyList<Portfolio> portfolios = await _portfolioService.GetAllAsync(userId);

        return Ok(portfolios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Portfolio>> GetById(Guid id)
    {
        Portfolio? portfolio = await _portfolioService.GetByIdAsync(id);

        if (portfolio is null)
        {
            return NotFound();
        }

        return Ok(portfolio);
    }

    [HttpPut("{id:guid}/rename")]
    public async Task<IActionResult> Rename(Guid id, RenamePortfolioRequest request)
    {
        bool renamed = await _portfolioService.RenameAsync(id, request);

        if (!renamed)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted = await _portfolioService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

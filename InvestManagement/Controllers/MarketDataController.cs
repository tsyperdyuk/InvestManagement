using InvestManagement.Application.MarketData.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InvestManagement.Api.Controllers;

[ApiController]
[Route("api/market-data")]
public sealed class MarketDataController : ControllerBase
{
    [HttpPost("update-prices")]
    public async Task<IActionResult> UpdatePrices(
        [FromServices] IMarketDataService marketDataService,
        CancellationToken cancellationToken)
    {
        await marketDataService.UpdateSecurityPricesAsync(cancellationToken);

        return NoContent();
    }
}
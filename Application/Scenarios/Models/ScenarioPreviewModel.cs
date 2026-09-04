using InvestManagement.Application.Portfolios.Models;

namespace InvestManagement.Application.Scenarios.Models;

public sealed record ScenarioPreviewModel
{
    public Guid ScenarioId { get; init; }

    public DateTime CalculatedAt { get; init; }

    public required PortfolioSummary CurrentPortfolio { get; init; }

    public required PortfolioSummary SimulatedPortfolio { get; init; }
}

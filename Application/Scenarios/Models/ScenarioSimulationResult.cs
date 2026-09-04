using InvestManagement.Application.Portfolios.Models;

namespace InvestManagement.Application.Scenarios.Models;

public sealed record ScenarioSimulationResult
{
    public required PortfolioSummary Portfolio { get; init; }

    public IReadOnlyList<ScenarioTransactionDraft> Transactions { get; init; } = [];
}

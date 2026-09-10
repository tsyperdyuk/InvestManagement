using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Models;

public sealed record ScenarioModel
{
    public Guid Id { get; init; }

    public Guid PortfolioId { get; init; }

    public required string Name { get; init; }

    public ScenarioStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? AppliedAt { get; init; }

    public IReadOnlyList<ScenarioOperationModel> Operations { get; init; } = [];
}

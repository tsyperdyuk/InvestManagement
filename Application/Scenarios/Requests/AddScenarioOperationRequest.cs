using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Requests;

public sealed record AddScenarioOperationRequest
{
    public ScenarioOperationType Type { get; init; }

    public Guid? SecurityId { get; init; }

    public decimal? Amount { get; init; }

    public decimal? Quantity { get; init; }
}

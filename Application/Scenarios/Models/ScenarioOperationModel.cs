using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Models;

public sealed record ScenarioOperationModel
{
    public Guid Id { get; init; }

    public ScenarioOperationType Type { get; init; }

    public Guid? SecurityId { get; init; }

    public decimal? Amount { get; init; }

    public decimal? Quantity { get; init; }

    public int Order { get; init; }
}

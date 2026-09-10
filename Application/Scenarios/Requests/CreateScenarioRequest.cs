namespace InvestManagement.Application.Scenarios.Requests;

public sealed record CreateScenarioRequest
{
    public required string Name { get; init; }
}

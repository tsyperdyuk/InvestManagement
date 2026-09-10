using InvestManagement.Application.Scenarios.Models;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Mappings;

public static class ScenarioMappings
{
    public static ScenarioModel ToModel(this Scenario scenario)
    {
        return new ScenarioModel
        {
            Id = scenario.Id,
            PortfolioId = scenario.PortfolioId,
            Name = scenario.Name,
            Status = scenario.Status,
            CreatedAt = scenario.CreatedAt,
            AppliedAt = scenario.AppliedAt,
            Operations = scenario.Operations
                .OrderBy(operation => operation.Order)
                .Select(operation => operation.ToModel())
                .ToList()
        };
    }

    public static ScenarioOperationModel ToModel(
        this ScenarioOperation operation)
    {
        return new ScenarioOperationModel
        {
            Id = operation.Id,
            Type = operation.Type,
            SecurityId = operation.SecurityId,
            Amount = operation.Amount,
            Quantity = operation.Quantity,
            Order = operation.Order
        };
    }
}

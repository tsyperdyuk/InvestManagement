using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Models;

public sealed record ScenarioTransactionDraft
{
    public TransactionType Type { get; init; }

    public Guid? SecurityId { get; init; }

    public decimal Amount { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? Price { get; init; }
}

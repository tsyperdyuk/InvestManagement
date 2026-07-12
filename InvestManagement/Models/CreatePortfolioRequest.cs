namespace InvestManagement.Models;

public record CreatePortfolioRequest
{
    public required Guid UserId { get; init; }

    public required string Name { get; init; }

    public string Currency { get; init; } = "USD";
}

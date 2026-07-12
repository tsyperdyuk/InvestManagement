namespace InvestManagement.Models;

public record RenamePortfolioRequest
{
    public required string Name { get; init; }
}

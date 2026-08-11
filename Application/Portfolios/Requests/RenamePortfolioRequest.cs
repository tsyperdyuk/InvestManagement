namespace InvestManagement.Application.Portfolios.Requests;

public record RenamePortfolioRequest
{
    public required string Name { get; init; }
}

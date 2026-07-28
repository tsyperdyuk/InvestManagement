namespace InvestManagement.Domain.Model;

public sealed class Position
{
    public Position(Guid portfolioId, Guid securityId, decimal quantity)
    {
        Id = Guid.NewGuid();
        PortfolioId = portfolioId;
        SecurityId = securityId;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid SecurityId { get; private set; }

    public decimal Quantity { get; private set; }

    public Portfolio Portfolio { get; private set; } = null!;

    public Security Security { get; private set; } = null!;
}
namespace InvestManagement.Domain.Model;

public class Portfolio
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public PortfolioCurrency Currency { get; set; } = PortfolioCurrency.USD;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Position> Positions { get; private set; }
    = new List<Position>();
}

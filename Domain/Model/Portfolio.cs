namespace InvestManagement.Domain.Model;

public class Portfolio
{
    private readonly List<Transaction> _transactions = [];

    private Portfolio()
    {
    }

    public Portfolio(
        string name,
        PortfolioCurrency currency)
    {
        Id = Guid.NewGuid();
        Name = name;
        Currency = currency;
    }

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public PortfolioCurrency Currency { get; set; } = PortfolioCurrency.USD;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();
}

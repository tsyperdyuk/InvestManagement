namespace InvestManagement.Domain.Model;

public class Portfolio
{
    private readonly List<Transaction> _transactions = [];

    private Portfolio()
    {
    }

    public Portfolio(string name, PortfolioCurrency currency, Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.", nameof(userId));
        }

        Id = Guid.NewGuid();
        Name = name;
        Currency = currency;
        UserId = userId;
    }

    public Guid Id { get; set; }

    public Guid UserId { get; private set; }

    public string Name { get; set; } = string.Empty;

    public PortfolioCurrency Currency { get; set; } = PortfolioCurrency.USD;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    public uint Version { get; private set; }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

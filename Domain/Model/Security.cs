using System.ComponentModel.DataAnnotations;

namespace InvestManagement.Domain.Model;

public sealed class Security
{
    public Security(string ticker, string name, SecurityType type, PortfolioCurrency currency)
    {
        Id = Guid.NewGuid();
        Ticker = ticker;
        Name = name;
        Type = type;
        Currency = currency;
    }

    public Guid Id { get; private set; }

    [Required]
    [MaxLength(20)]
    public string Ticker { get; private set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = null!;

    public SecurityType Type { get; private set; }

    public PortfolioCurrency Currency { get; private set; }

    public decimal CurrentPrice { get; private set; }

    public decimal? PreviousPrice { get; private set; }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrice));
        }

        PreviousPrice = CurrentPrice > 0 ? CurrentPrice : null;
        CurrentPrice = newPrice;
    }
}
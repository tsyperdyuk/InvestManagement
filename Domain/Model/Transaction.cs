namespace InvestManagement.Domain.Model;

public sealed class Transaction
{
    private Transaction()
    {
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid? SecurityId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public decimal? Quantity { get; private set; }

    public decimal? Price { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Portfolio Portfolio { get; private set; } = null!;

    public Security? Security { get; private set; }

    public static Transaction CreateDeposit(
        Guid portfolioId,
        decimal amount,
        DateTime createdAt)
    {
        ValidatePortfolioId(portfolioId);
        ValidateAmount(amount);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            Type = TransactionType.Deposit,
            Amount = amount,
            CreatedAt = createdAt
        };
    }

    public static Transaction CreateWithdrawal(
        Guid portfolioId,
        decimal amount,
        DateTime createdAt)
    {
        ValidatePortfolioId(portfolioId);
        ValidateAmount(amount);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            CreatedAt = createdAt
        };
    }

    public static Transaction CreateBuy(
        Guid portfolioId,
        Guid securityId,
        decimal quantity,
        decimal price,
        DateTime createdAt)
    {
        return CreateTrade(
            portfolioId,
            securityId,
            TransactionType.Buy,
            quantity,
            price,
            createdAt);
    }

    public static Transaction CreateSell(
        Guid portfolioId,
        Guid securityId,
        decimal quantity,
        decimal price,
        DateTime createdAt)
    {
        return CreateTrade(
            portfolioId,
            securityId,
            TransactionType.Sell,
            quantity,
            price,
            createdAt);
    }

    private static Transaction CreateTrade(
        Guid portfolioId,
        Guid securityId,
        TransactionType type,
        decimal quantity,
        decimal price,
        DateTime createdAt)
    {
        ValidatePortfolioId(portfolioId);

        if (securityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Security ID cannot be empty.",
                nameof(securityId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price must be greater than zero.");
        }

        return new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            SecurityId = securityId,
            Type = type,
            Quantity = quantity,
            Price = price,
            Amount = quantity * price,
            CreatedAt = createdAt
        };
    }

    private static void ValidatePortfolioId(Guid portfolioId)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException(
                "Portfolio ID cannot be empty.",
                nameof(portfolioId));
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");
        }
    }
}
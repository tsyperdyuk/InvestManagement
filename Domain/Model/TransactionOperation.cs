namespace InvestManagement.Domain.Model;

public sealed class TransactionOperation
{
    private TransactionOperation()
    {
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid UserId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public Guid? SecurityId { get; private set; }

    public decimal? Quantity { get; private set; }

    public TransactionOperationStatus Status { get; private set; }

    public Guid? TransactionId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public static TransactionOperation CreateDeposit(Guid portfolioId, Guid userId, decimal amount)
    {
        return Create(portfolioId, userId, amount, TransactionType.Deposit);
    }

    public static TransactionOperation CreateWithdrawal(Guid portfolioId, Guid userId, decimal amount)
    {
        return Create(portfolioId, userId, amount, TransactionType.Withdrawal);
    }

    public static TransactionOperation CreateBuy(
        Guid portfolioId,
        Guid userId,
        Guid securityId,
        decimal quantity)
    {
        return CreateTrade(portfolioId, userId, securityId, quantity, TransactionType.Buy);
    }

    public static TransactionOperation CreateSell(
        Guid portfolioId,
        Guid userId,
        Guid securityId,
        decimal quantity)
    {
        return CreateTrade(portfolioId, userId, securityId, quantity, TransactionType.Sell);
    }

    private static TransactionOperation CreateTrade(
        Guid portfolioId,
        Guid userId,
        Guid securityId,
        decimal quantity,
        TransactionType type)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException(
                "Portfolio ID cannot be empty.", nameof(portfolioId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.", nameof(userId));
        }

        if (securityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Security ID cannot be empty.", nameof(securityId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity), "Quantity must be greater than zero.");
        }

        return new TransactionOperation
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            UserId = userId,
            Type = type,
            SecurityId = securityId,
            Quantity = quantity,
            Amount = 0m,
            Status = TransactionOperationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkCompleted(Guid transactionId)
    {
        EnsurePending();

        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Transaction ID cannot be empty.",
                nameof(transactionId));
        }

        TransactionId = transactionId;
        Status = TransactionOperationStatus.Completed;
        FinishedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Failure reason is required.",
                nameof(reason));
        }

        FailureReason = reason;
        Status = TransactionOperationStatus.Failed;
        FinishedAtUtc = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != TransactionOperationStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending operations can be processed.");
        }
    }

    private static TransactionOperation Create(Guid portfolioId, Guid userId, decimal amount, TransactionType type)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException(
                "Portfolio ID cannot be empty.",
                nameof(portfolioId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");
        }

        return new TransactionOperation
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            UserId = userId,
            Type = type,
            Amount = amount,
            Status = TransactionOperationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

namespace InvestManagement.Domain.Model;

public sealed class ScenarioOperation
{
    private ScenarioOperation()
    {
    }

    private ScenarioOperation(
        Guid scenarioId,
        ScenarioOperationType type,
        Guid? securityId,
        decimal? amount,
        decimal? quantity,
        int order)
    {
        if (scenarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "Scenario ID cannot be empty.",
                nameof(scenarioId));
        }

        if (order < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                "Operation order must be greater than zero.");
        }

        Id = Guid.NewGuid();
        ScenarioId = scenarioId;
        Type = type;
        SecurityId = securityId;
        Amount = amount;
        Quantity = quantity;
        Order = order;
    }

    public Guid Id { get; private set; }

    public Guid ScenarioId { get; private set; }

    public Scenario Scenario { get; private set; } = null!;

    public ScenarioOperationType Type { get; private set; }

    public Guid? SecurityId { get; private set; }

    public Security? Security { get; private set; }

    public decimal? Amount { get; private set; }

    public decimal? Quantity { get; private set; }

    public int Order { get; private set; }

    public static ScenarioOperation CreateCashOperation(
        Guid scenarioId,
        ScenarioOperationType type,
        decimal amount,
        int order)
    {
        if (type is not ScenarioOperationType.Deposit
            and not ScenarioOperationType.Withdrawal)
        {
            throw new ArgumentException(
                "Cash operation must be Deposit or Withdrawal.",
                nameof(type));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");
        }

        return new ScenarioOperation(
            scenarioId,
            type,
            securityId: null,
            amount,
            quantity: null,
            order);
    }

    public static ScenarioOperation CreateTradeOperation(
        Guid scenarioId,
        ScenarioOperationType type,
        Guid securityId,
        decimal quantity,
        int order)
    {
        if (type is not ScenarioOperationType.Buy
            and not ScenarioOperationType.Sell)
        {
            throw new ArgumentException(
                "Trade operation must be Buy or Sell.",
                nameof(type));
        }

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

        return new ScenarioOperation(
            scenarioId,
            type,
            securityId,
            amount: null,
            quantity,
            order);
    }
}

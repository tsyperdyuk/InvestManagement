namespace InvestManagement.Domain.Model;

public sealed class Scenario
{
    private readonly List<ScenarioOperation> _operations = [];

    private Scenario()
    {
    }

    public Scenario(
        Guid portfolioId,
        string name,
        DateTime createdAt)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException(
                "Portfolio ID cannot be empty.",
                nameof(portfolioId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Scenario name cannot be empty.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        PortfolioId = portfolioId;
        Name = name.Trim();
        Status = ScenarioStatus.Draft;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Portfolio Portfolio { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public ScenarioStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? AppliedAt { get; private set; }

    public IReadOnlyCollection<ScenarioOperation> Operations =>
        _operations.AsReadOnly();

    public uint Version { get; private set; }

    public ScenarioOperation AddCashOperation(
        ScenarioOperationType type,
        decimal amount)
    {
        EnsureDraft();

        var operation = ScenarioOperation.CreateCashOperation(
            Id,
            type,
            amount,
            GetNextOperationOrder());

        _operations.Add(operation);

        return operation;
    }

    public ScenarioOperation AddTradeOperation(
        ScenarioOperationType type,
        Guid securityId,
        decimal quantity)
    {
        EnsureDraft();

        var operation = ScenarioOperation.CreateTradeOperation(
            Id,
            type,
            securityId,
            quantity,
            GetNextOperationOrder());

        _operations.Add(operation);

        return operation;
    }

    public bool RemoveOperation(Guid operationId)
    {
        EnsureDraft();

        var operation = _operations.SingleOrDefault(
            x => x.Id == operationId);

        if (operation is null)
        {
            return false;
        }

        _operations.Remove(operation);

        return true;
    }

    public void Rename(string name)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Scenario name cannot be empty.",
                nameof(name));
        }

        Name = name.Trim();
    }

    public void MarkAsApplied(DateTime appliedAt)
    {
        EnsureDraft();

        Status = ScenarioStatus.Applied;
        AppliedAt = appliedAt;
    }

    private void EnsureDraft()
    {
        if (Status != ScenarioStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only draft scenarios can be modified.");
        }
    }

    private int GetNextOperationOrder()
    {
        return _operations.Count == 0
            ? 1
            : _operations.Max(x => x.Order) + 1;
    }
}

using InvestManagement.Application.Common.Exceptions;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Scenarios.Models;
using InvestManagement.Application.Securities.Models;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Services;

public sealed class ScenarioSimulationEngine
{
    public ScenarioSimulationResult Simulate(
        PortfolioSummary currentPortfolio,
        IEnumerable<ScenarioOperation> operations,
        IReadOnlyDictionary<Guid, Security> securities)
    {
        var cashBalance = currentPortfolio.CashBalance;
        var positions = currentPortfolio.Securities.ToDictionary(
            summary => summary.SecurityId,
            PositionState.FromSummary);
        var transactionDrafts = new List<ScenarioTransactionDraft>();

        foreach (var operation in operations.OrderBy(x => x.Order))
        {
            switch (operation.Type)
            {
                case ScenarioOperationType.Deposit:
                    ApplyDeposit(operation, ref cashBalance, transactionDrafts);
                    break;

                case ScenarioOperationType.Withdrawal:
                    ApplyWithdrawal(operation, ref cashBalance, transactionDrafts);
                    break;

                case ScenarioOperationType.Buy:
                    ApplyBuy(
                        operation,
                        securities,
                        positions,
                        ref cashBalance,
                        transactionDrafts);
                    break;

                case ScenarioOperationType.Sell:
                    ApplySell(
                        operation,
                        securities,
                        positions,
                        ref cashBalance,
                        transactionDrafts);
                    break;

                default:
                    throw new BusinessRuleException(
                        $"Unsupported scenario operation type {operation.Type}.");
            }
        }

        var securitySummaries = positions.Values
            .Where(position => position.Quantity > 0)
            .OrderBy(position => position.Ticker)
            .Select(position => position.ToSummary())
            .ToList();
        var totalMarketValue = securitySummaries.Sum(x => x.MarketValue);
        var totalCostBasis = securitySummaries.Sum(x => x.CostBasis);

        return new ScenarioSimulationResult
        {
            Portfolio = new PortfolioSummary
            {
                PortfolioId = currentPortfolio.PortfolioId,
                PortfolioName = currentPortfolio.PortfolioName,
                Currency = currentPortfolio.Currency,
                CashBalance = cashBalance,
                TotalMarketValue = totalMarketValue,
                TotalPortfolioValue = cashBalance + totalMarketValue,
                TotalCostBasis = totalCostBasis,
                Securities = securitySummaries
            },
            Transactions = transactionDrafts
        };
    }

    private static void ApplyDeposit(
        ScenarioOperation operation,
        ref decimal cashBalance,
        ICollection<ScenarioTransactionDraft> transactionDrafts)
    {
        var amount = GetRequiredAmount(operation);
        cashBalance += amount;

        transactionDrafts.Add(CreateCashDraft(TransactionType.Deposit, amount));
    }

    private static void ApplyWithdrawal(
        ScenarioOperation operation,
        ref decimal cashBalance,
        ICollection<ScenarioTransactionDraft> transactionDrafts)
    {
        var amount = GetRequiredAmount(operation);

        if (cashBalance < amount)
        {
            throw new BusinessRuleException(
                $"Insufficient cash balance for scenario operation {operation.Id}.");
        }

        cashBalance -= amount;

        transactionDrafts.Add(CreateCashDraft(TransactionType.Withdrawal, amount));
    }

    private static void ApplyBuy(
        ScenarioOperation operation,
        IReadOnlyDictionary<Guid, Security> securities,
        IDictionary<Guid, PositionState> positions,
        ref decimal cashBalance,
        ICollection<ScenarioTransactionDraft> transactionDrafts)
    {
        var security = GetSecurity(operation, securities);
        var quantity = GetRequiredQuantity(operation);
        var amount = quantity * security.CurrentPrice;

        if (cashBalance < amount)
        {
            throw new BusinessRuleException(
                $"Insufficient cash balance for scenario operation {operation.Id}.");
        }

        cashBalance -= amount;

        if (!positions.TryGetValue(security.Id, out var position))
        {
            position = PositionState.FromSecurity(security);
            positions.Add(security.Id, position);
        }

        position.UpdateSecurity(security);
        position.Quantity += quantity;
        position.CostBasis += amount;

        transactionDrafts.Add(CreateTradeDraft(
            TransactionType.Buy,
            security,
            quantity,
            amount));
    }

    private static void ApplySell(
        ScenarioOperation operation,
        IReadOnlyDictionary<Guid, Security> securities,
        IDictionary<Guid, PositionState> positions,
        ref decimal cashBalance,
        ICollection<ScenarioTransactionDraft> transactionDrafts)
    {
        var security = GetSecurity(operation, securities);
        var quantity = GetRequiredQuantity(operation);

        if (!positions.TryGetValue(security.Id, out var position)
            || position.Quantity < quantity)
        {
            throw new BusinessRuleException(
                $"Insufficient security quantity for scenario operation {operation.Id}.");
        }

        var averagePrice = position.Quantity == 0
            ? 0
            : position.CostBasis / position.Quantity;
        var amount = quantity * security.CurrentPrice;

        position.UpdateSecurity(security);
        position.Quantity -= quantity;
        position.CostBasis -= quantity * averagePrice;

        if (position.Quantity == 0)
        {
            position.CostBasis = 0;
        }

        cashBalance += amount;

        transactionDrafts.Add(CreateTradeDraft(
            TransactionType.Sell,
            security,
            quantity,
            amount));
    }

    private static Security GetSecurity(
        ScenarioOperation operation,
        IReadOnlyDictionary<Guid, Security> securities)
    {
        var securityId = operation.SecurityId
            ?? throw new BusinessRuleException(
                $"Security ID is missing for scenario operation {operation.Id}.");

        if (!securities.TryGetValue(securityId, out var security))
        {
            throw new BusinessRuleException(
                $"Security {securityId} was not loaded.");
        }

        if (security.CurrentPrice <= 0)
        {
            throw new BusinessRuleException(
                $"Current price is not available for security {securityId}.");
        }

        return security;
    }

    private static decimal GetRequiredAmount(ScenarioOperation operation)
    {
        var amount = operation.Amount
            ?? throw new BusinessRuleException(
                $"Amount is missing for scenario operation {operation.Id}.");

        if (amount <= 0)
        {
            throw new BusinessRuleException(
                $"Amount must be positive for scenario operation {operation.Id}.");
        }

        return amount;
    }

    private static decimal GetRequiredQuantity(ScenarioOperation operation)
    {
        var quantity = operation.Quantity
            ?? throw new BusinessRuleException(
                $"Quantity is missing for scenario operation {operation.Id}.");

        if (quantity <= 0)
        {
            throw new BusinessRuleException(
                $"Quantity must be positive for scenario operation {operation.Id}.");
        }

        return quantity;
    }

    private static ScenarioTransactionDraft CreateCashDraft(
        TransactionType type,
        decimal amount)
    {
        return new ScenarioTransactionDraft
        {
            Type = type,
            Amount = amount
        };
    }

    private static ScenarioTransactionDraft CreateTradeDraft(
        TransactionType type,
        Security security,
        decimal quantity,
        decimal amount)
    {
        return new ScenarioTransactionDraft
        {
            Type = type,
            SecurityId = security.Id,
            Amount = amount,
            Quantity = quantity,
            Price = security.CurrentPrice
        };
    }

    private sealed class PositionState
    {
        public Guid SecurityId { get; private set; }

        public string Ticker { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal CostBasis { get; set; }

        public decimal CurrentPrice { get; private set; }

        public static PositionState FromSummary(SecuritySummary summary)
        {
            return new PositionState
            {
                SecurityId = summary.SecurityId,
                Ticker = summary.Ticker,
                Name = summary.Name,
                Quantity = summary.Quantity,
                CostBasis = summary.CostBasis,
                CurrentPrice = summary.CurrentPrice
            };
        }

        public static PositionState FromSecurity(Security security)
        {
            var position = new PositionState();
            position.UpdateSecurity(security);
            return position;
        }

        public void UpdateSecurity(Security security)
        {
            SecurityId = security.Id;
            Ticker = security.Ticker;
            Name = security.Name;
            CurrentPrice = security.CurrentPrice;
        }

        public SecuritySummary ToSummary()
        {
            var averagePrice = Quantity == 0
                ? 0
                : CostBasis / Quantity;

            return new SecuritySummary
            {
                SecurityId = SecurityId,
                Ticker = Ticker,
                Name = Name,
                Quantity = Quantity,
                CostBasis = CostBasis,
                AveragePrice = averagePrice,
                CurrentPrice = CurrentPrice,
                MarketValue = Quantity * CurrentPrice
            };
        }
    }
}

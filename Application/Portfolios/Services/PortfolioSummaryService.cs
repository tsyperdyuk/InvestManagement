using InvestManagement.Application.Common;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Models;
using InvestManagement.Application.Portfolios.Responses;
using InvestManagement.Application.Securities.Models;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Portfolios.Services;

public sealed class PortfolioSummaryService(IPortfolioRepository portfolioRepository, ICurrentUser currentUser) : IPortfolioSummaryService
{
    public async Task<PortfolioSummary?> GetSummaryAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        var result = await CalculateAsync(
            portfolioId,
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new PortfolioSummary
        {
            PortfolioId = result.PortfolioId,
            PortfolioName = result.PortfolioName,
            Currency = result.Currency,
            CashBalance = result.CashBalance,
            TotalMarketValue = result.TotalMarketValue,
            TotalPortfolioValue = result.TotalPortfolioValue,
            TotalCostBasis = result.TotalCostBasis
        };
    }

    public async Task<PortfolioSummary?> GetDetailsAsync(
        Guid portfolioId,
        CancellationToken cancellationToken)
    {
        var result = await CalculateAsync(
            portfolioId,
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new PortfolioSummary
        {
            PortfolioId = result.PortfolioId,
            PortfolioName = result.PortfolioName,
            Currency = result.Currency,
            CashBalance = result.CashBalance,
            TotalMarketValue = result.TotalMarketValue,
            TotalPortfolioValue = result.TotalPortfolioValue,
            TotalCostBasis = result.TotalCostBasis,
            Securities = result.Securities
        };
    }

    private async Task<PortfolioCalculationResult?> CalculateAsync(
        Guid portfolioId,
        CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetSummaryDataAsync(portfolioId, currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            return null;
        }

        var transactions = portfolio.Transactions
            .OrderBy(transaction => transaction.CreatedAt)
            .ToList();

        var cashBalance = CalculateCashBalance(transactions);

        var securities = transactions
            .Where(transaction =>
                transaction.Type is TransactionType.Buy
                    or TransactionType.Sell)
            .GroupBy(transaction => transaction.SecurityId!.Value)
            .Select(CalculateSecuritySummary)
            .Where(summary => summary.Quantity > 0)
            .ToList();

        var totalMarketValue = securities.Sum(x => x.MarketValue);
        var totalCostBasis = securities.Sum(x => x.CostBasis);

        return new PortfolioCalculationResult
        {
            PortfolioId = portfolio.Id,
            PortfolioName = portfolio.Name,
            Currency = portfolio.Currency,
            CashBalance = cashBalance,
            TotalMarketValue = totalMarketValue,
            TotalPortfolioValue = cashBalance + totalMarketValue,
            TotalCostBasis = totalCostBasis,
            Securities = securities
        };
    }

    private static decimal CalculateCashBalance(
        IEnumerable<Transaction> transactions)
    {
        return transactions.Sum(transaction =>
            transaction.Type switch
            {
                TransactionType.Deposit => transaction.Amount,
                TransactionType.Sell => transaction.Amount,

                TransactionType.Withdrawal => -transaction.Amount,
                TransactionType.Buy => -transaction.Amount,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(transaction.Type),
                    transaction.Type,
                    "Unsupported transaction type.")
            });
    }

    private static SecuritySummary CalculateSecuritySummary(
        IGrouping<Guid, Transaction> transactionGroup)
    {
        var transactions = transactionGroup
            .OrderBy(transaction => transaction.CreatedAt)
            .ToList();

        decimal quantity = 0;
        decimal costBasis = 0;

        foreach (var transaction in transactions)
        {
            var transactionQuantity = transaction.Quantity
                ?? throw new InvalidOperationException(
                    $"Quantity is missing for transaction {transaction.Id}.");

            var transactionPrice = transaction.Price
                ?? throw new InvalidOperationException(
                    $"Price is missing for transaction {transaction.Id}.");

            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    quantity += transactionQuantity;
                    costBasis += transactionQuantity * transactionPrice;
                    break;

                case TransactionType.Sell:
                    if (transactionQuantity > quantity)
                    {
                        throw new InvalidOperationException(
                            $"Cannot sell {transactionQuantity} units of " +
                            $"security {transaction.SecurityId}. " +
                            $"Available quantity: {quantity}.");
                    }

                    var averagePriceBeforeSale =
                        quantity == 0
                            ? 0
                            : costBasis / quantity;

                    quantity -= transactionQuantity;
                    costBasis -=
                        transactionQuantity * averagePriceBeforeSale;

                    break;

                default:
                    throw new InvalidOperationException(
                        $"Transaction type {transaction.Type} is not a trade.");
            }
        }

        var lastTransaction = transactions[^1];

        var security = lastTransaction.Security
            ?? throw new InvalidOperationException(
                $"Security {transactionGroup.Key} was not loaded.");

        var currentPrice = security.CurrentPrice;

        if (currentPrice <= 0)
        {
            throw new InvalidOperationException(
                $"Current price is not available for security {security.Id}.");
        }

        var averagePrice =
            quantity == 0
                ? 0
                : costBasis / quantity;

        return new SecuritySummary
        {
            SecurityId = security.Id,
            Ticker = security.Ticker,
            Name = security.Name,
            Quantity = quantity,
            CostBasis = costBasis,
            AveragePrice = averagePrice,
            CurrentPrice = currentPrice,
            MarketValue = quantity * currentPrice
        };
    }
}
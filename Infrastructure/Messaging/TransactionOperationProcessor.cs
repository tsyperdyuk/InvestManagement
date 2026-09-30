using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestManagement.Infrastructure.Messaging;

public sealed class TransactionOperationProcessor
{
    private readonly InvestManagementDbContext _dbContext;
    private readonly ILogger<TransactionOperationProcessor> _logger;

    public TransactionOperationProcessor(InvestManagementDbContext dbContext, ILogger<TransactionOperationProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var operation = await _dbContext.TransactionOperations
            .SingleOrDefaultAsync(
                operation => operation.Id == operationId,
                cancellationToken);

        if (operation is null)
        {
            throw new InvalidOperationException(
                $"Transaction operation {operationId} was not found.");
        }

        if (operation.Status != TransactionOperationStatus.Pending)
        {
            _logger.LogInformation(
                "Operation {OperationId} is already finished with status {Status}",
                operation.Id,
                operation.Status);

            return;
        }

        if (operation.Type != TransactionType.Deposit &&
            operation.Type != TransactionType.Withdrawal &&
            operation.Type != TransactionType.Buy &&
            operation.Type != TransactionType.Sell)
        {
            operation.MarkFailed("Unsupported transaction type.");

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var portfolio = await _dbContext.Portfolios
            .SingleOrDefaultAsync(
                portfolio =>
                    portfolio.Id == operation.PortfolioId &&
                    portfolio.UserId == operation.UserId,
                cancellationToken);

        if (portfolio is null)
        {
            operation.MarkFailed(
                "Portfolio was not found or its owner has changed.");

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        Transaction transaction;

        if (operation.Type == TransactionType.Withdrawal)
        {
            var cashBalance = await GetCashBalanceAsync(portfolio.Id, cancellationToken);

            if (cashBalance < operation.Amount)
            {
                operation.MarkFailed("Insufficient cash balance.");

                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            transaction = Transaction.CreateWithdrawal(
                portfolio.Id,
                operation.Amount,
                DateTime.UtcNow);
        }
        else if (operation.Type == TransactionType.Buy || operation.Type == TransactionType.Sell)
        {
            if (operation.SecurityId is not Guid securityId ||
                securityId == Guid.Empty ||
                operation.Quantity is not decimal quantity ||
                quantity <= 0)
            {
                operation.MarkFailed("Invalid trade operation.");
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var security = await _dbContext.Securities
                .AsNoTracking()
                .SingleOrDefaultAsync(security => security.Id == securityId, cancellationToken);

            if (security is null)
            {
                operation.MarkFailed("Security not found.");
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            if (security.Currency != portfolio.Currency)
            {
                operation.MarkFailed("Security currency must match portfolio currency.");
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            if (security.CurrentPrice <= 0)
            {
                operation.MarkFailed("Security price is not available.");
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var price = security.CurrentPrice;
            if (operation.Type == TransactionType.Buy)
            {
                var cashBalance = await GetCashBalanceAsync(portfolio.Id, cancellationToken);
                if (cashBalance < quantity * price)
                {
                    operation.MarkFailed("Insufficient cash balance.");
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return;
                }

                transaction = Transaction.CreateBuy(
                    portfolio.Id, securityId, quantity, price, DateTime.UtcNow);
            }
            else
            {
                var availableQuantity = await _dbContext.Transactions
                    .Where(item => item.PortfolioId == portfolio.Id && item.SecurityId == securityId)
                    .SumAsync(item => item.Type == TransactionType.Buy
                        ? item.Quantity ?? 0m
                        : item.Type == TransactionType.Sell ? -(item.Quantity ?? 0m) : 0m,
                        cancellationToken);

                if (availableQuantity < quantity)
                {
                    operation.MarkFailed("Insufficient security quantity.");
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return;
                }

                transaction = Transaction.CreateSell(
                    portfolio.Id, securityId, quantity, price, DateTime.UtcNow);
            }
        }
        else
        {
            transaction = Transaction.CreateDeposit(
                portfolio.Id,
                operation.Amount,
                DateTime.UtcNow);
        }

        _dbContext.Transactions.Add(transaction);

        portfolio.Touch();
        operation.MarkCompleted(transaction.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Operation {OperationId} completed with transaction {TransactionId}",
            operation.Id,
            transaction.Id);
    }

    private Task<decimal> GetCashBalanceAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        return _dbContext.Transactions
            .Where(transaction => transaction.PortfolioId == portfolioId)
            .SumAsync(
                transaction =>
                    transaction.Type == TransactionType.Deposit ||
                    transaction.Type == TransactionType.Sell
                        ? transaction.Amount
                        : transaction.Type == TransactionType.Withdrawal ||
                          transaction.Type == TransactionType.Buy
                            ? -transaction.Amount
                            : 0m,
                cancellationToken);
    }
}

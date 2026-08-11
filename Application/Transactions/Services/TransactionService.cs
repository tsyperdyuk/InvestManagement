using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Domain.Model;
using InvestManagement.Infrastructure.Data;

namespace InvestManagement.Application.Transactions.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISecurityRepository _securityRepository;
    private readonly InvestManagementDbContext _dbContext;

    public TransactionService(IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository, ISecurityRepository securityRepository, InvestManagementDbContext dbContext)
    {
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _securityRepository = securityRepository;
        _dbContext = dbContext;
    }

    public async Task DepositAsync(Guid portfolioId, decimal amount, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId, cancellationToken);

        if (portfolio is null)
        {
            throw new InvalidOperationException("Portfolio not found.");
        }

        var transaction = Transaction.CreateDeposit(portfolioId, amount, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task WithdrawalAsync(Guid portfolioId, decimal amount, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId, cancellationToken);

        if (portfolio is null)
        {
            throw new InvalidOperationException("Portfolio not found.");
        }

        var transactions = await _transactionRepository.GetByPortfolioIdAsync(portfolioId, cancellationToken);

        var cashBalance = CalculateCashBalance(transactions);

        if (cashBalance < amount)
        {
            throw new InvalidOperationException("Insufficient cash balance.");
        }

        var transaction = Transaction.CreateWithdrawal(portfolioId, amount, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BuyAsync(Guid portfolioId, Guid securityId, decimal quantity, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId, cancellationToken);

        if (portfolio is null)
        {
            throw new InvalidOperationException("Portfolio not found.");
        }

        var security = await _securityRepository.GetByIdAsync(securityId, cancellationToken);

        if (security is null)
        {
            throw new InvalidOperationException("Security not found.");
        }

        if (security.CurrentPrice <= 0)
        {
            throw new InvalidOperationException("Security price is not available.");
        }

        var transactions = await _transactionRepository.GetByPortfolioIdAsync(portfolioId, cancellationToken);

        var cashBalance = CalculateCashBalance(transactions);

        var amount = quantity * security.CurrentPrice;

        if (cashBalance < amount)
        {
            throw new InvalidOperationException("Insufficient cash balance.");
        }

        var transaction = Transaction.CreateBuy(portfolioId, securityId, quantity, security.CurrentPrice, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SellAsync(Guid portfolioId, Guid securityId, decimal quantity, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId, cancellationToken);

        if (portfolio is null)
        {
            throw new InvalidOperationException("Portfolio not found.");
        }

        var security = await _securityRepository.GetByIdAsync(securityId, cancellationToken);

        if (security is null)
        {
            throw new InvalidOperationException("Security not found.");
        }

        if (security.CurrentPrice <= 0)
        {
            throw new InvalidOperationException("Security price is not available.");
        }

        var transactions = await _transactionRepository.GetByPortfolioIdAsync(portfolioId, cancellationToken);

        var securityQuantity = CalculateSecurityQuantity(transactions, securityId);

        if (securityQuantity < quantity)
        {
            throw new InvalidOperationException("Insufficient security quantity.");
        }

        var transaction = Transaction.CreateSell(portfolioId, securityId, quantity, security.CurrentPrice, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static decimal CalculateCashBalance(IEnumerable<Transaction> transactions)
    {
        return transactions.Sum(transaction => transaction.Type switch
        {
            TransactionType.Deposit => transaction.Amount,
            TransactionType.Sell => transaction.Amount,
            TransactionType.Withdrawal => -transaction.Amount,
            TransactionType.Buy => -transaction.Amount,
            _ => 0
        });
    }

    private static decimal CalculateSecurityQuantity(IEnumerable<Transaction> transactions, Guid securityId)
    {
        return transactions
            .Where(x => x.SecurityId == securityId)
            .Sum(x => x.Type switch
            {
                TransactionType.Buy => x.Quantity ?? 0,
                TransactionType.Sell => -(x.Quantity ?? 0),
                _ => 0
            });
    }
}
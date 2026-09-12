using InvestManagement.Application.Common;
using InvestManagement.Application.Common.Exceptions;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Requests;
using InvestManagement.Application.Transactions.Models.Responses;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Transactions.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISecurityRepository _securityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public TransactionService(IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository, ISecurityRepository securityRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _securityRepository = securityRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TransactionResponse> DepositAsync(DepositTransactionRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, _currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        portfolio.Touch();

        var transaction = Transaction.CreateDeposit(request.PortfolioId, request.Amount, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id,
            transaction.PortfolioId,
            transaction.SecurityId,
            transaction.Type,
            transaction.Amount,
            transaction.Quantity,
            transaction.Price,
            transaction.CreatedAt);
    }

    public async Task<TransactionResponse> WithdrawalAsync(WithdrawalTransactionRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, _currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        var transactions = await _transactionRepository.GetByPortfolioIdAsync(request.PortfolioId, cancellationToken);

        var cashBalance = CalculateCashBalance(transactions);

        if (cashBalance < request.Amount)
        {
            throw new InvalidOperationException("Insufficient cash balance.");
        }

        portfolio.Touch();

        var transaction = Transaction.CreateWithdrawal(request.PortfolioId, request.Amount, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id,
            transaction.PortfolioId,
            transaction.SecurityId,
            transaction.Type,
            transaction.Amount,
            transaction.Quantity,
            transaction.Price,
            transaction.CreatedAt);
    }

    public async Task<TransactionResponse> BuyAsync(BuyTransactionRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, _currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        var security = await _securityRepository.GetByIdAsync(request.SecurityId, cancellationToken);

        if (security is null)
        {
            throw new InvalidOperationException("Security not found.");
        }

        if (security.CurrentPrice <= 0)
        {
            throw new InvalidOperationException("Security price is not available.");
        }

        var transactions = await _transactionRepository.GetByPortfolioIdAsync(request.PortfolioId, cancellationToken);

        var cashBalance = CalculateCashBalance(transactions);
        var amount = request.Quantity * security.CurrentPrice;

        if (cashBalance < amount)
        {
            throw new InvalidOperationException("Insufficient cash balance.");
        }

        portfolio.Touch();

        var transaction = Transaction.CreateBuy(request.PortfolioId, request.SecurityId, request.Quantity, security.CurrentPrice, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id,
            transaction.PortfolioId,
            transaction.SecurityId,
            transaction.Type,
            transaction.Amount,
            transaction.Quantity,
            transaction.Price,
            transaction.CreatedAt);
    }

    public async Task<TransactionResponse> SellAsync(SellTransactionRequest request, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, _currentUser.GetRequiredUserId(), cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        var security = await _securityRepository.GetByIdAsync(request.SecurityId, cancellationToken);

        if (security is null)
        {
            throw new InvalidOperationException("Security not found.");
        }

        if (security.CurrentPrice <= 0)
        {
            throw new InvalidOperationException("Security price is not available.");
        }

        portfolio.Touch();

        var transactions = await _transactionRepository.GetByPortfolioIdAsync(request.PortfolioId, cancellationToken);

        var securityQuantity = CalculateSecurityQuantity(transactions, request.SecurityId);

        if (securityQuantity < request.Quantity)
        {
            throw new InvalidOperationException("Insufficient security quantity.");
        }

        var transaction = Transaction.CreateSell(request.PortfolioId, request.SecurityId, request.Quantity, security.CurrentPrice, DateTime.UtcNow);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id,
            transaction.PortfolioId,
            transaction.SecurityId,
            transaction.Type,
            transaction.Amount,
            transaction.Quantity,
            transaction.Price,
            transaction.CreatedAt);
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
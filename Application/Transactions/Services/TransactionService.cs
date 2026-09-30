using InvestManagement.Application.Common;
using InvestManagement.Application.Common.Exceptions;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Contracts;
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
    private readonly ITransactionOperationRepository _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IOutboxWriter _outboxWriter;

    public TransactionService(IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository, ISecurityRepository securityRepository, ITransactionOperationRepository operationRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser, IOutboxWriter outboxWriter)
    {
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _securityRepository = securityRepository;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _outboxWriter = outboxWriter;
    }

    public async Task<TransactionOperationResponse?> GetOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var operation = await _operationRepository.GetByIdAsync(
            operationId,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        return operation is null
            ? null
            : new TransactionOperationResponse(
                operation.Id,
                operation.Status,
                operation.TransactionId,
                operation.FailureReason);
    }

    public async Task<TransactionOperationResponse> DepositAsync(DepositTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var portfolio = await _portfolioRepository.GetByIdAsync(
            request.PortfolioId,
            userId,
            cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        if (request.Amount <= 0)
        {
            throw new BusinessRuleException(
                "Deposit amount must be greater than zero.");
        }

        var operation = TransactionOperation.CreateDeposit(
            portfolio.Id,
            userId,
            request.Amount);

        _operationRepository.Add(operation);

        _outboxWriter.Add(new ProcessTransaction(operation.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionOperationResponse(operation.Id, operation.Status);
    }

    public async Task<TransactionOperationResponse> WithdrawalAsync(WithdrawalTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, userId, cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Withdrawal amount must be greater than zero.");
        }

        var operation = TransactionOperation.CreateWithdrawal(portfolio.Id, userId, request.Amount);
        _operationRepository.Add(operation);
        _outboxWriter.Add(new ProcessTransaction(operation.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionOperationResponse(operation.Id, operation.Status);
    }

    public async Task<TransactionOperationResponse> BuyAsync(BuyTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, userId, cancellationToken);
        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        if (request.SecurityId == Guid.Empty || request.Quantity <= 0)
        {
            throw new BusinessRuleException("A security ID and a positive quantity are required.");
        }

        var security = await _securityRepository.GetByIdAsync(request.SecurityId, cancellationToken);
        if (security is null)
        {
            throw new NotFoundException("Security not found.");
        }

        var operation = TransactionOperation.CreateBuy(portfolio.Id, userId, security.Id, request.Quantity);
        _operationRepository.Add(operation);
        _outboxWriter.Add(new ProcessTransaction(operation.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new TransactionOperationResponse(operation.Id, operation.Status);
    }
    public async Task<TransactionOperationResponse> SellAsync(SellTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, userId, cancellationToken);
        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        if (request.SecurityId == Guid.Empty || request.Quantity <= 0)
        {
            throw new BusinessRuleException("A security ID and a positive quantity are required.");
        }

        var security = await _securityRepository.GetByIdAsync(request.SecurityId, cancellationToken);
        if (security is null)
        {
            throw new NotFoundException("Security not found.");
        }

        var operation = TransactionOperation.CreateSell(portfolio.Id, userId, security.Id, request.Quantity);
        _operationRepository.Add(operation);
        _outboxWriter.Add(new ProcessTransaction(operation.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new TransactionOperationResponse(operation.Id, operation.Status);
    }
}

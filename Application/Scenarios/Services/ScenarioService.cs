using InvestManagement.Application.Common;
using System;
using InvestManagement.Application.Common.Exceptions;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Scenarios.Interfaces;
using InvestManagement.Application.Scenarios.Mappings;
using InvestManagement.Application.Scenarios.Models;
using InvestManagement.Application.Scenarios.Requests;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Models.Responses;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Services;

public sealed class ScenarioService : IScenarioService
{
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioSummaryService _portfolioSummaryService;
    private readonly ISecurityRepository _securityRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ScenarioSimulationEngine _simulationEngine;

    public ScenarioService(
        IScenarioRepository scenarioRepository,
        IPortfolioRepository portfolioRepository,
        IPortfolioSummaryService portfolioSummaryService,
        ISecurityRepository securityRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        ScenarioSimulationEngine simulationEngine, ICurrentUser currentUser)
    {
        _scenarioRepository = scenarioRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioSummaryService = portfolioSummaryService;
        _securityRepository = securityRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _simulationEngine = simulationEngine;
    }

    public async Task<ScenarioModel> CreateAsync(
        Guid portfolioId,
        CreateScenarioRequest request,
        CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(
            portfolioId,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        var scenario = new Scenario(
            portfolioId,
            request.Name,
            DateTime.UtcNow);

        await _scenarioRepository.AddAsync(scenario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return scenario.ToModel();
    }

    public async Task<ScenarioModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(
            id,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        return scenario?.ToModel();
    }

    public async Task<IReadOnlyList<ScenarioModel>> GetByPortfolioIdAsync(
        Guid portfolioId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId, userId, cancellationToken);
        if (portfolio is null)
        {
            throw new NotFoundException("Portfolio not found.");
        }

        var scenarios = await _scenarioRepository.GetByPortfolioIdAsync(
            portfolioId,
            userId,
            cancellationToken);

        return scenarios
            .Select(scenario => scenario.ToModel())
            .ToList();
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(
            id,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (scenario is null)
        {
            return false;
        }

        if (scenario.Status != ScenarioStatus.Draft)
        {
            throw new ConflictException(
                "Only draft scenarios can be deleted.");
        }

        _scenarioRepository.Delete(scenario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ScenarioOperationModel?> AddOperationAsync(
        Guid scenarioId,
        AddScenarioOperationRequest request,
        CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(
            scenarioId,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (scenario is null)
        {
            return null;
        }

        EnsureDraft(scenario);

        ScenarioOperation operation;

        if (request.Type is ScenarioOperationType.Deposit
            or ScenarioOperationType.Withdrawal)
        {
            operation = scenario.AddCashOperation(
                request.Type,
                request.Amount!.Value);
        }
        else
        {
            var securityId = request.SecurityId!.Value;
            var security = await _securityRepository.GetByIdAsync(
                securityId,
                cancellationToken);

            if (security is null)
            {
                throw new NotFoundException("Security not found.");
            }

            operation = scenario.AddTradeOperation(
                request.Type,
                securityId,
                request.Quantity!.Value);
        }

        await _scenarioRepository.AddOperationAsync(
            operation,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return operation.ToModel();
    }

    public async Task<bool> RemoveOperationAsync(
        Guid scenarioId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(
            scenarioId,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (scenario is null)
        {
            return false;
        }

        EnsureDraft(scenario);

        if (!scenario.RemoveOperation(operationId))
        {
            return false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ScenarioPreviewModel?> PreviewAsync(Guid scenarioId, CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(
            scenarioId,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (scenario is null)
        {
            return null;
        }

        if (scenario.Status != ScenarioStatus.Draft)
        {
            throw new ConflictException(
                "Only draft scenarios can be previewed.");
        }

        var currentPortfolio = await _portfolioSummaryService.GetDetailsAsync(scenario.PortfolioId, cancellationToken)
            ?? throw new NotFoundException("Portfolio not found.");
        var securities = await _securityRepository.GetAllAsync(cancellationToken);
        var securitiesById = securities.ToDictionary(security => security.Id);
        var simulation = _simulationEngine.Simulate(
            currentPortfolio,
            scenario.Operations,
            securitiesById);

        return new ScenarioPreviewModel
        {
            ScenarioId = scenario.Id,
            CalculatedAt = DateTime.UtcNow,
            CurrentPortfolio = currentPortfolio,
            SimulatedPortfolio = simulation.Portfolio
        };
    }

    public async Task<IReadOnlyList<TransactionResponse>?> ApplyAsync(Guid scenarioId, CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(
            scenarioId,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (scenario is null)
        {
            return null;
        }

        if (scenario.Status != ScenarioStatus.Draft)
        {
            throw new ConflictException(
                "Scenario has already been applied.");
        }

        if (scenario.Operations.Count == 0)
        {
            throw new BusinessRuleException(
                "Cannot apply a scenario without operations.");
        }

        var portfolio = await _portfolioRepository.GetByIdAsync(
            scenario.PortfolioId,
            _currentUser.GetRequiredUserId(),
            cancellationToken)
            ?? throw new NotFoundException("Portfolio not found.");
        var currentPortfolio = await _portfolioSummaryService.GetDetailsAsync(
            scenario.PortfolioId,
            cancellationToken)
            ?? throw new NotFoundException("Portfolio not found.");
        var securities = await _securityRepository.GetAllAsync(cancellationToken);
        var simulation = _simulationEngine.Simulate(
            currentPortfolio,
            scenario.Operations,
            securities.ToDictionary(security => security.Id));
        var appliedAt = DateTime.UtcNow;
        var transactions = simulation.Transactions
            .Select((draft, index) => CreateTransaction(
                scenario.PortfolioId,
                draft,
                appliedAt.AddTicks(index)))
            .ToList();

        await _transactionRepository.AddRangeAsync(
            transactions,
            cancellationToken);

        portfolio.Touch();
        scenario.MarkAsApplied(appliedAt);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transactions
            .Select(ToResponse)
            .ToList();
    }

    private static Transaction CreateTransaction(
        Guid portfolioId,
        ScenarioTransactionDraft draft,
        DateTime createdAt)
    {
        return draft.Type switch
        {
            TransactionType.Deposit => Transaction.CreateDeposit(
                portfolioId,
                draft.Amount,
                createdAt),

            TransactionType.Withdrawal => Transaction.CreateWithdrawal(
                portfolioId,
                draft.Amount,
                createdAt),

            TransactionType.Buy => Transaction.CreateBuy(
                portfolioId,
                GetRequiredSecurityId(draft),
                GetRequiredQuantity(draft),
                GetRequiredPrice(draft),
                createdAt),

            TransactionType.Sell => Transaction.CreateSell(
                portfolioId,
                GetRequiredSecurityId(draft),
                GetRequiredQuantity(draft),
                GetRequiredPrice(draft),
                createdAt),

            _ => throw new InvalidOperationException(
                $"Unsupported transaction type {draft.Type}.")
        };
    }

    private static void EnsureDraft(Scenario scenario)
    {
        if (scenario.Status != ScenarioStatus.Draft)
        {
            throw new ConflictException(
                "Only draft scenarios can be modified.");
        }
    }

    private static Guid GetRequiredSecurityId(ScenarioTransactionDraft draft)
    {
        return draft.SecurityId
            ?? throw new InvalidOperationException(
                "Security ID is missing from scenario transaction draft.");
    }

    private static decimal GetRequiredQuantity(ScenarioTransactionDraft draft)
    {
        return draft.Quantity
            ?? throw new InvalidOperationException(
                "Quantity is missing from scenario transaction draft.");
    }

    private static decimal GetRequiredPrice(ScenarioTransactionDraft draft)
    {
        return draft.Price
            ?? throw new InvalidOperationException(
                "Price is missing from scenario transaction draft.");
    }

    private static TransactionResponse ToResponse(Transaction transaction)
    {
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
}

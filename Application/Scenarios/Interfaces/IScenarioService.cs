using InvestManagement.Application.Scenarios.Models;
using InvestManagement.Application.Scenarios.Requests;
using InvestManagement.Application.Transactions.Models.Responses;

namespace InvestManagement.Application.Scenarios.Interfaces;

public interface IScenarioService
{
    Task<ScenarioModel> CreateAsync(
        Guid portfolioId,
        CreateScenarioRequest request,
        CancellationToken cancellationToken);

    Task<ScenarioModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ScenarioModel>> GetByPortfolioIdAsync(
        Guid portfolioId,
        CancellationToken cancellationToken);

    Task<ScenarioOperationModel?> AddOperationAsync(
        Guid scenarioId,
        AddScenarioOperationRequest request,
        CancellationToken cancellationToken);

    Task<bool> RemoveOperationAsync(
        Guid scenarioId,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ScenarioPreviewModel?> PreviewAsync(
        Guid scenarioId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionResponse>?> ApplyAsync(
        Guid scenarioId,
        CancellationToken cancellationToken);
}

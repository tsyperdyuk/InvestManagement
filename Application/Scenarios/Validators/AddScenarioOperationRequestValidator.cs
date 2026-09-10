using FluentValidation;
using InvestManagement.Application.Scenarios.Requests;
using InvestManagement.Domain.Model;

namespace InvestManagement.Application.Scenarios.Validators;

public sealed class AddScenarioOperationRequestValidator
    : AbstractValidator<AddScenarioOperationRequest>
{
    public AddScenarioOperationRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid scenario operation type.");

        When(IsCashOperation, () =>
        {
            RuleFor(x => x.Amount)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.SecurityId)
                .Null()
                .WithMessage("Security ID is not allowed for cash operations.");

            RuleFor(x => x.Quantity)
                .Null()
                .WithMessage("Quantity is not allowed for cash operations.");
        });

        When(IsTradeOperation, () =>
        {
            RuleFor(x => x.SecurityId)
                .NotEmpty()
                .WithMessage("Security ID is required for trade operations.");

            RuleFor(x => x.Quantity)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");

            RuleFor(x => x.Amount)
                .Null()
                .WithMessage("Amount is not allowed for trade operations.");
        });
    }

    private static bool IsCashOperation(AddScenarioOperationRequest request)
    {
        return request.Type is ScenarioOperationType.Deposit
            or ScenarioOperationType.Withdrawal;
    }

    private static bool IsTradeOperation(AddScenarioOperationRequest request)
    {
        return request.Type is ScenarioOperationType.Buy
            or ScenarioOperationType.Sell;
    }
}

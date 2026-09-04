using FluentValidation;
using InvestManagement.Application.Scenarios.Requests;

namespace InvestManagement.Application.Scenarios.Validators;

public sealed class CreateScenarioRequestValidator
    : AbstractValidator<CreateScenarioRequest>
{
    public CreateScenarioRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Scenario name is required.")
            .MaximumLength(200)
            .WithMessage(
                "Scenario name cannot exceed 200 characters.");
    }
}

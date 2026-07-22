using FluentValidation;
using InvestManagement.Application.Models;

namespace InvestManagement.Application.Validators;

public class CreatePortfolioRequestValidator : AbstractValidator<CreatePortfolioRequest>
{
    public CreatePortfolioRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Portfolio name is required.")
            .MaximumLength(100)
            .WithMessage("Portfolio name cannot exceed 100 characters.");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency.");
    }
}

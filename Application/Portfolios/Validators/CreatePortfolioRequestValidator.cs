using FluentValidation;
using InvestManagement.Application.Portfolios.Requests;

namespace InvestManagement.Application.Portfolios.Validators;

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

using FluentValidation;
using MyProject.Application.DTOs.Account;

namespace MyProject.Application.Validators
{
    public class AccountValidator : AbstractValidator<CreateAccountDto>
    {
        public AccountValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Account name is required.")
                .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters.");

            RuleFor(x => x.AccountType)
                .NotEmpty().WithMessage("Account type is required.");
        }
    }
}
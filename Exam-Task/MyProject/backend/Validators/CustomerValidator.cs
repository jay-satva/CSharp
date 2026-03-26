using FluentValidation;
using MyProject.Application.DTOs.Customer;

namespace MyProject.Application.Validators
{
    public class CustomerValidator : AbstractValidator<CreateCustomerDto>
    {
        public CustomerValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required.")
                .MaximumLength(100).WithMessage("Display name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email address.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}
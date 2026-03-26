using FluentValidation;
using MyProject.Application.DTOs.Item;

namespace MyProject.Application.Validators
{
    public class ItemValidator : AbstractValidator<CreateItemDto>
    {
        public ItemValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Item name is required.")
                .MaximumLength(100).WithMessage("Item name cannot exceed 100 characters.");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price must be 0 or greater.");

            RuleFor(x => x.IncomeAccountRef)
                .NotEmpty().WithMessage("Income account is required.");
        }
    }
}
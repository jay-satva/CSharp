using FluentValidation;
using MyProject.Application.DTOs.Invoice;

namespace MyProject.Application.Validators
{
    public class InvoiceValidator : AbstractValidator<CreateInvoiceDto>
    {
        public InvoiceValidator()
        {
            RuleFor(x => x.CustomerRef)
                .NotEmpty().WithMessage("Customer is required.");

            RuleFor(x => x.InvoiceDate)
                .NotEmpty().WithMessage("Invoice date is required.");

            RuleFor(x => x.DueDate)
                .NotEmpty().WithMessage("Due date is required.")
                .GreaterThanOrEqualTo(x => x.InvoiceDate).WithMessage("Due date must be on or after invoice date.");

            RuleFor(x => x.LineItems)
                .NotEmpty().WithMessage("At least one line item is required.");

            RuleForEach(x => x.LineItems).SetValidator(new LineItemValidator());
        }
    }

    public class LineItemValidator : AbstractValidator<CreateInvoiceLineItemDto>
    {
        public LineItemValidator()
        {
            RuleFor(x => x.ItemRef)
                .NotEmpty().WithMessage("Item is required.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price must be 0 or greater.");
        }
    }
}
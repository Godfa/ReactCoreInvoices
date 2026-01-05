using Domain;
using FluentValidation;

namespace Application.ExpenseLineItems
{
    public class ExpenseLineItemValidator : AbstractValidator<ExpenseLineItem>
    {
        public ExpenseLineItemValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit Price must be greater than or equal to 0");

            RuleFor(x => x.ExpenseItemId)
                .NotEmpty().WithMessage("ExpenseItemId is required");
        }
    }
}

using Domain;
using FluentValidation;

namespace Application.ExpenseItems
{
    public class ExpenseItemValidator : AbstractValidator<ExpenseItem>
    {
        public ExpenseItemValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.ExpenseCreditor)
                .InclusiveBetween(1, 10).WithMessage("Expense Creditor must be between 1 and 10");

            RuleFor(x => x.ExpenseType)
                .IsInEnum().WithMessage("Invalid Expense Type");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0");
        }
    }
}

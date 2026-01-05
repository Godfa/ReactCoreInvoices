using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseLineItems
{
    public class Create
    {
        public class Command : IRequest
        {
            public ExpenseLineItem ExpenseLineItem { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.ExpenseLineItem).SetValidator(new ExpenseLineItemValidator());
            }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly DataContext _context;
            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                // Verify that the parent ExpenseItem exists
                var expenseItem = await _context.ExpenseItems
                    .FirstOrDefaultAsync(x => x.Id == request.ExpenseLineItem.ExpenseItemId, cancellationToken);

                if (expenseItem == null)
                {
                    throw new Exception("ExpenseItem not found");
                }

                _context.ExpenseLineItems.Add(request.ExpenseLineItem);

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

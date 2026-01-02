using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseItems
{
    public class AddPayer
    {
        public class Command : IRequest
        {
            public Guid ExpenseItemId { get; set; }
            public int CreditorId { get; set; }
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
                var expenseItem = await _context.ExpenseItems
                    .Include(ei => ei.Payers)
                    .FirstOrDefaultAsync(ei => ei.Id == request.ExpenseItemId, cancellationToken);

                if (expenseItem == null)
                    throw new Exception("Expense item not found");

                var creditor = await _context.Creditors
                    .FirstOrDefaultAsync(c => c.Id == request.CreditorId, cancellationToken);

                if (creditor == null)
                    throw new Exception("Creditor not found");

                var existingPayer = await _context.ExpenseItemPayers
                    .FirstOrDefaultAsync(eip => eip.ExpenseItemId == request.ExpenseItemId
                        && eip.CreditorId == request.CreditorId, cancellationToken);

                if (existingPayer != null)
                    return Unit.Value; // Already a payer

                var payer = new ExpenseItemPayer
                {
                    ExpenseItemId = request.ExpenseItemId,
                    CreditorId = request.CreditorId
                };

                _context.ExpenseItemPayers.Add(payer);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

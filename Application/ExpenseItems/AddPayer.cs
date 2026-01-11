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
            public string AppUserId { get; set; }
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

                // Get the InvoiceId from the shadow property
                var invoiceId = _context.Entry(expenseItem).Property<Guid?>("InvoiceId").CurrentValue;

                if (invoiceId.HasValue)
                {
                    var invoice = await _context.Invoices.FindAsync(invoiceId.Value);

                    if (invoice != null && (invoice.Status == InvoiceStatus.Maksussa || invoice.Status == InvoiceStatus.Arkistoitu))
                    {
                        throw new Exception("Maksajia ei voi lisätä, kun lasku on maksussa tai arkistoitu.");
                    }
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(c => c.Id == request.AppUserId, cancellationToken);

                if (user == null)
                    throw new Exception("User not found");

                var existingPayer = await _context.ExpenseItemPayers
                    .FirstOrDefaultAsync(eip => eip.ExpenseItemId == request.ExpenseItemId
                        && eip.AppUserId == request.AppUserId, cancellationToken);

                if (existingPayer != null)
                    return Unit.Value; // Already a payer

                var payer = new ExpenseItemPayer
                {
                    ExpenseItemId = request.ExpenseItemId,
                    AppUserId = request.AppUserId
                };

                _context.ExpenseItemPayers.Add(payer);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

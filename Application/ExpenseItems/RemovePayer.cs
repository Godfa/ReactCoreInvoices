using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseItems
{
    public class RemovePayer
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
                var payer = await _context.ExpenseItemPayers
                    .FirstOrDefaultAsync(eip => eip.ExpenseItemId == request.ExpenseItemId
                        && eip.AppUserId == request.AppUserId, cancellationToken);

                if (payer == null)
                    return Unit.Value; // Payer not found, nothing to remove

                // Get the expense item to check invoice status
                var expenseItem = await _context.ExpenseItems.FindAsync(request.ExpenseItemId);

                if (expenseItem != null)
                {
                    // Get the InvoiceId from the shadow property
                    var invoiceId = _context.Entry(expenseItem).Property<Guid?>("InvoiceId").CurrentValue;

                    if (invoiceId.HasValue)
                    {
                        var invoice = await _context.Invoices.FindAsync(invoiceId.Value);

                        if (invoice != null && (invoice.Status == InvoiceStatus.Maksussa || invoice.Status == InvoiceStatus.Arkistoitu))
                        {
                            throw new Exception("Maksajia ei voi poistaa, kun lasku on maksussa tai arkistoitu.");
                        }
                    }
                }

                _context.ExpenseItemPayers.Remove(payer);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

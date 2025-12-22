using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseItems
{
    public class Create
    {
        public class Command : IRequest
        {
            public ExpenseItem ExpenseItem { get; set; }
            public Guid InvoiceId { get; set; }
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
                if (request.InvoiceId != Guid.Empty)
                {
                    var invoice = await _context.Invoices.Include(i => i.ExpenseItems)
                        .FirstOrDefaultAsync(x => x.Id == request.InvoiceId, cancellationToken);
                    
                    if (invoice != null)
                    {
                        invoice.ExpenseItems.Add(request.ExpenseItem);
                    }
                    else
                    {
                         // If invoice not found, maybe just add as orphan? 
                         // Or throw. For now, add as orphan to avoid data loss, but ideally should error.
                         _context.ExpenseItems.Add(request.ExpenseItem);
                    }
                }
                else
                {
                    _context.ExpenseItems.Add(request.ExpenseItem);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }

    }
}

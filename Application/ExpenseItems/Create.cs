using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using FluentValidation;
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

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.ExpenseItem).SetValidator(new ExpenseItemValidator());
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
                if (request.InvoiceId != Guid.Empty)
                {
                    var invoice = await _context.Invoices
                        .FirstOrDefaultAsync(x => x.Id == request.InvoiceId, cancellationToken);

                    if (invoice != null)
                    {
                        // Prevent adding expense items to invoices that are in payment or archived
                        if (invoice.Status == InvoiceStatus.Maksussa || invoice.Status == InvoiceStatus.Arkistoitu)
                        {
                            throw new Exception("Kuluja ei voi lisätä, kun lasku on maksussa tai arkistoitu.");
                        }

                        _context.ExpenseItems.Add(request.ExpenseItem);
                        // Set the foreign key directly instead of modifying the collection
                        _context.Entry(request.ExpenseItem).Property("InvoiceId").CurrentValue = request.InvoiceId;
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

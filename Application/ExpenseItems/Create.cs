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
                _context.ExpenseItems.Add(request.ExpenseItem);

                if (request.InvoiceId != Guid.Empty)
                {
                    var invoice = await _context.Invoices
                        .Include(i => i.Participants)
                        .FirstOrDefaultAsync(x => x.Id == request.InvoiceId, cancellationToken);

                    if (invoice != null)
                    {
                        // Set the foreign key directly instead of modifying the collection
                        _context.Entry(request.ExpenseItem).Property("InvoiceId").CurrentValue = request.InvoiceId;

                        // Add all invoice participants as default payers
                        request.ExpenseItem.Payers = invoice.Participants
                            .Select(p => new ExpenseItemPayer
                            {
                                ExpenseItemId = request.ExpenseItem.Id,
                                CreditorId = p.CreditorId
                            })
                            .ToList();
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }

    }
}

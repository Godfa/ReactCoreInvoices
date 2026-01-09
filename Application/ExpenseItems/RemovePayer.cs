using System;
using System.Threading;
using System.Threading.Tasks;
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

                _context.ExpenseItemPayers.Remove(payer);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Persistence;

namespace Application.ExpenseLineItems
{
    public class Delete
    {
        public class Command : IRequest
        {
            public Guid Id { get; set; }
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
                var expenseLineItem = await _context.ExpenseLineItems
                    .FindAsync(new object[] { request.Id }, cancellationToken);

                if (expenseLineItem == null)
                {
                    throw new Exception("ExpenseLineItem not found");
                }

                _context.ExpenseLineItems.Remove(expenseLineItem);

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Persistence;

namespace Application.Invoices
{
    public class ChangeStatus
    {
        public class Command : IRequest<Invoice>
        {
            public Guid InvoiceId { get; set; }
            public InvoiceStatus NewStatus { get; set; }
        }

        public class Handler : IRequestHandler<Command, Invoice>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Invoice> Handle(Command request, CancellationToken cancellationToken)
            {
                var invoice = await _context.Invoices.FindAsync(new object[] { request.InvoiceId }, cancellationToken);

                if (invoice == null)
                {
                    throw new Exception("Laskua ei löytynyt");
                }

                invoice.Status = request.NewStatus;

                await _context.SaveChangesAsync(cancellationToken);

                return invoice;
            }
        }
    }
}

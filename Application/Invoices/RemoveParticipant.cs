using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Invoices
{
    public class RemoveParticipant
    {
        public class Command : IRequest
        {
            public Guid InvoiceId { get; set; }
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
                var participant = await _context.InvoiceParticipants
                    .FirstOrDefaultAsync(ip => ip.InvoiceId == request.InvoiceId && ip.AppUserId == request.AppUserId, cancellationToken);

                if (participant == null)
                    throw new Exception("Participant not found");

                // Check invoice status before removing participant
                var invoice = await _context.Invoices.FindAsync(request.InvoiceId);

                if (invoice != null && (invoice.Status == InvoiceStatus.Maksussa || invoice.Status == InvoiceStatus.Arkistoitu))
                {
                    throw new Exception("Osallistujia ei voi poistaa, kun lasku on maksussa tai arkistoitu.");
                }

                _context.InvoiceParticipants.Remove(participant);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

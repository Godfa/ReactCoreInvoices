using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Invoices
{
    public class PayViaToken
    {
        public class Command : IRequest<Unit>
        {
            public Guid Token { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                // Find the participant by payment token
                var participant = await _context.InvoiceParticipants
                    .Include(p => p.Invoice)
                    .FirstOrDefaultAsync(p => p.PaymentToken == request.Token, cancellationToken);

                if (participant == null)
                    throw new Exception("Virheellinen tai vanhentunut maksulinkki.");

                // Check if already paid
                if (participant.HasPaid)
                    throw new Exception("Tämä maksu on jo merkitty maksetuksi.");

                // Validate that invoice is in 'Maksussa' state
                if (participant.Invoice.Status != Domain.InvoiceStatus.Maksussa)
                    throw new Exception("Lasku ei ole enää maksussa-tilassa.");

                // Mark as paid
                participant.HasPaid = true;
                participant.PaidAt = DateTime.UtcNow;

                // Invalidate the token (one-time use only)
                participant.PaymentToken = null;

                var result = await _context.SaveChangesAsync(cancellationToken) > 0;

                if (!result)
                    throw new Exception("Maksun merkitseminen epäonnistui. Yritä uudelleen.");

                return Unit.Value;
            }
        }
    }
}

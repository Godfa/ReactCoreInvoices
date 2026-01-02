using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Invoices
{
    public class AddParticipant
    {
        public class Command : IRequest
        {
            public Guid InvoiceId { get; set; }
            public int CreditorId { get; set; }
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
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                if (invoice == null)
                    throw new Exception($"Invoice with id {request.InvoiceId} not found");

                var creditor = await _context.Creditors.FindAsync(new object[] { request.CreditorId }, cancellationToken);

                if (creditor == null)
                    throw new Exception($"Creditor with id {request.CreditorId} not found");

                var existingParticipant = await _context.InvoiceParticipants
                    .FirstOrDefaultAsync(ip => ip.InvoiceId == request.InvoiceId && ip.CreditorId == request.CreditorId, cancellationToken);

                if (existingParticipant != null)
                    throw new Exception("Creditor is already a participant");

                var participant = new InvoiceParticipant
                {
                    InvoiceId = request.InvoiceId,
                    CreditorId = request.CreditorId
                };

                _context.InvoiceParticipants.Add(participant);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

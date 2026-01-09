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
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                if (invoice == null)
                    throw new Exception($"Invoice with id {request.InvoiceId} not found");

                var user = await _context.Users.FindAsync(new object[] { request.AppUserId }, cancellationToken);

                if (user == null)
                    throw new Exception($"User with id {request.AppUserId} not found");

                var existingParticipant = await _context.InvoiceParticipants
                    .FirstOrDefaultAsync(ip => ip.InvoiceId == request.InvoiceId && ip.AppUserId == request.AppUserId, cancellationToken);

                if (existingParticipant != null)
                    throw new Exception("User is already a participant");

                var participant = new InvoiceParticipant
                {
                    InvoiceId = request.InvoiceId,
                    AppUserId = request.AppUserId
                };

                _context.InvoiceParticipants.Add(participant);
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

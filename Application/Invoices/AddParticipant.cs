using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            private readonly IEmailService _emailService;
            private readonly IConfiguration _config;

            public Handler(DataContext context, IEmailService emailService, IConfiguration config)
            {
                _context = context;
                _emailService = emailService;
                _config = config;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                if (invoice == null)
                    throw new Exception($"Invoice with id {request.InvoiceId} not found");

                // Prevent adding participants to invoices that are in payment or archived
                if (invoice.Status == InvoiceStatus.Maksussa || invoice.Status == InvoiceStatus.Arkistoitu)
                {
                    throw new Exception("Osallistujia ei voi lisätä, kun lasku on maksussa tai arkistoitu.");
                }

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

                if (invoice.Status == InvoiceStatus.Aktiivinen && !string.IsNullOrEmpty(user.Email))
                {
                    var usualSuspects = new[] { "Epi", "JHattu", "Leivo", "Timo", "Jaapu", "Urpi", "Zeip" };
                    bool isUsualSuspect = usualSuspects.Any(us => string.Equals(us, user.DisplayName, StringComparison.OrdinalIgnoreCase));
                    if (!isUsualSuspect)
                    {
                        try
                        {
                            var appUrl = _config["Email:AppUrl"];
                            var invoiceUrl = $"{appUrl}/invoices/{invoice.Id}";
                            
                            await _emailService.SendInvoiceReviewNotificationAsync(
                                user.Email,
                                user.DisplayName,
                                invoice.Title,
                                invoiceUrl
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to send review notification to new participant {user.Email}: {ex.Message}");
                        }
                    }
                }

                return Unit.Value;
            }
        }
    }
}

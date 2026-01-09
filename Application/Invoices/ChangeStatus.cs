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
            private readonly IEmailService _emailService;
            private readonly IConfiguration _config;

            public Handler(DataContext context, IEmailService emailService, IConfiguration config)
            {
                _context = context;
                _emailService = emailService;
                _config = config;
            }

            public async Task<Invoice> Handle(Command request, CancellationToken cancellationToken)
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.Creditor)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                if (invoice == null)
                {
                    throw new Exception("Laskua ei löytynyt");
                }

                var oldStatus = invoice.Status;
                invoice.Status = request.NewStatus;

                await _context.SaveChangesAsync(cancellationToken);

                // Send email notifications when status changes to Katselmoitavana (Under Review)
                if (request.NewStatus == InvoiceStatus.Katselmoitavana && oldStatus != InvoiceStatus.Katselmoitavana)
                {
                    var appUrl = _config["Email:AppUrl"] ?? "https://your-app-url.com";
                    var invoiceUrl = $"{appUrl}/invoices/{invoice.Id}";

                    // Send email to all participants
                    if (invoice.Participants != null && invoice.Participants.Any())
                    {
                        foreach (var participant in invoice.Participants)
                        {
                            if (participant.Creditor != null && !string.IsNullOrEmpty(participant.Creditor.Email))
                            {
                                await _emailService.SendInvoiceReviewNotificationAsync(
                                    participant.Creditor.Email,
                                    participant.Creditor.Name,
                                    invoice.Title,
                                    invoiceUrl
                                );
                            }
                        }
                    }
                }

                return invoice;
            }
        }
    }
}

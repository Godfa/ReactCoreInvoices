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
                        .ThenInclude(p => p.AppUser)
                    .Include(i => i.Approvals)
                        .ThenInclude(a => a.AppUser)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                if (invoice == null)
                {
                    throw new Exception("Laskua ei löytynyt");
                }

                var oldStatus = invoice.Status;
                invoice.Status = request.NewStatus;

                await _context.SaveChangesAsync(cancellationToken);

                // Send email notifications when status changes to Maksussa (In Payment)
                if (request.NewStatus == InvoiceStatus.Maksussa && oldStatus != InvoiceStatus.Maksussa)
                {
                    var appUrl = _config["Email:AppUrl"];
                    var invoiceUrl = $"{appUrl}/invoices/{invoice.Id}";

                    // Send email to all participants
                    if (invoice.Participants != null && invoice.Participants.Any())
                    {
                        foreach (var participant in invoice.Participants)
                        {
                            if (participant.AppUser != null && !string.IsNullOrEmpty(participant.AppUser.Email))
                            {
                                await _emailService.SendInvoiceReviewNotificationAsync(
                                    participant.AppUser.Email,
                                    participant.AppUser.DisplayName,
                                    invoice.Title,
                                    invoiceUrl
                                );
                            }
                        }
                    }
                }

                // Reload invoice with all related data
                invoice = await _context.Invoices
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.AppUser)
                    .Include(i => i.Approvals)
                        .ThenInclude(a => a.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Organizer)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                            .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                return invoice;
            }
        }
    }
}

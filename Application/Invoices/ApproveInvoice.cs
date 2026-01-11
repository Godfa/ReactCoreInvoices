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
    public class ApproveInvoice
    {
        public class Command : IRequest<Invoice>
        {
            public Guid InvoiceId { get; set; }
            public string AppUserId { get; set; }
            public string CurrentUserId { get; set; }
            public bool IsAdmin { get; set; }
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
                    throw new Exception($"Invoice with id {request.InvoiceId} not found");

                // Check if user is a participant
                var isParticipant = invoice.Participants?.Any(p => p.AppUserId == request.AppUserId) ?? false;
                if (!isParticipant)
                    throw new Exception("Only participants can approve the invoice");

                // Check if current user is trying to approve for someone else (only admin can do this)
                if (request.CurrentUserId != request.AppUserId && !request.IsAdmin)
                    throw new Exception("Voit hyväksyä vain omasta puolestasi");

                // Check if invoice is in active status
                if (invoice.Status != InvoiceStatus.Aktiivinen)
                    throw new Exception("Vain aktiivisen laskun voi hyväksyä");

                // Check if already approved
                var existingApproval = await _context.InvoiceApprovals
                    .FirstOrDefaultAsync(ia => ia.InvoiceId == request.InvoiceId && ia.AppUserId == request.AppUserId, cancellationToken);

                if (existingApproval != null)
                    throw new Exception("Olet jo hyväksynyt tämän laskun");

                // Add approval
                var approval = new InvoiceApproval
                {
                    InvoiceId = request.InvoiceId,
                    AppUserId = request.AppUserId,
                    ApprovedAt = DateTime.UtcNow
                };

                _context.InvoiceApprovals.Add(approval);
                await _context.SaveChangesAsync(cancellationToken);

                // Reload invoice with updated approvals and all related data
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

                // Check if all participants have approved
                var participantCount = invoice.Participants?.Count ?? 0;
                var approvalCount = invoice.Approvals?.Count ?? 0;

                if (participantCount > 0 && approvalCount == participantCount)
                {
                    // All participants have approved, change status to Maksussa
                    invoice.Status = InvoiceStatus.Maksussa;
                    await _context.SaveChangesAsync(cancellationToken);

                    // Send email notifications to all participants
                    var appUrl = _config["Email:AppUrl"] ?? "https://your-app-url.com";
                    var invoiceUrl = $"{appUrl}/invoices/{invoice.Id}";

                    // Ensure participants with user data are loaded for email notifications
                    if (invoice.Participants != null && invoice.Participants.Any())
                    {
                        foreach (var participant in invoice.Participants)
                        {
                            // Ensure AppUser is loaded
                            if (participant.AppUser == null && !string.IsNullOrEmpty(participant.AppUserId))
                            {
                                await _context.Entry(participant)
                                    .Reference(p => p.AppUser)
                                    .LoadAsync(cancellationToken);
                            }

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

                return invoice;
            }
        }
    }
}

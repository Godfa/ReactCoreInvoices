using System;
using System.Collections.Generic;
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
    public class SendPaymentNotifications
    {
        public class Command : IRequest
        {
            public Guid InvoiceId { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly DataContext _context;
            private readonly IEmailService _emailService;
            private readonly IPdfService _pdfService;
            private readonly IConfiguration _config;

            public Handler(DataContext context, IEmailService emailService, IPdfService pdfService, IConfiguration config)
            {
                _context = context;
                _emailService = emailService;
                _pdfService = pdfService;
                _config = config;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var invoice = await _context.Invoices
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

                if (invoice == null)
                    return Unit.Value;

                // Check if all participants have approved
                var participantCount = invoice.Participants?.Count ?? 0;
                var approvalCount = invoice.Approvals?.Count ?? 0;

                if (approvalCount < participantCount)
                    return Unit.Value; // Not all participants have approved yet

                // Check if status is already Maksussa or Arkistoitu
                if (invoice.Status != InvoiceStatus.Aktiivinen)
                    return Unit.Value; // Already processed

                // Update invoice status to Maksussa
                invoice.Status = InvoiceStatus.Maksussa;
                await _context.SaveChangesAsync(cancellationToken);

                // Send email notifications with PDFs to all participants
                var baseUrl = _config["Email:AppUrl"];
                var invoiceUrl = $"{baseUrl}/invoices/{invoice.Id}";

                var attachments = new List<EmailAttachment>();

                // Generate full invoice PDF
                try
                {
                    var fullInvoicePdf = await _pdfService.GenerateInvoicePdfAsync(invoice.Id);
                    attachments.Add(new EmailAttachment
                    {
                        FileName = $"Lasku_{invoice.Title.Replace(" ", "_")}_Kokonaiserittely.pdf",
                        Content = fullInvoicePdf,
                        ContentType = "application/pdf"
                    });
                }
                catch (Exception ex)
                {
                    // Log error but continue with participant emails
                    Console.WriteLine($"Error generating full invoice PDF: {ex.Message}");
                }

                // Send notification to each participant
                foreach (var participant in invoice.Participants ?? new List<InvoiceParticipant>())
                {
                    // Ensure AppUser is loaded
                    if (participant.AppUser == null && !string.IsNullOrEmpty(participant.AppUserId))
                    {
                        await _context.Entry(participant)
                            .Reference(p => p.AppUser)
                            .LoadAsync(cancellationToken);
                    }

                    if (participant.AppUser == null || string.IsNullOrEmpty(participant.AppUser.Email))
                        continue;

                    var participantAttachments = new List<EmailAttachment>(attachments);

                    // Generate participant-specific PDF
                    try
                    {
                        var participantPdf = await _pdfService.GenerateParticipantInvoicePdfAsync(invoice.Id, participant.AppUserId);
                        participantAttachments.Add(new EmailAttachment
                        {
                            FileName = $"Lasku_{invoice.Title.Replace(" ", "_")}_Henkilokohtainen.pdf",
                            Content = participantPdf,
                            ContentType = "application/pdf"
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with email (just without participant PDF)
                        Console.WriteLine($"Error generating participant PDF for {participant.AppUserId}: {ex.Message}");
                    }

                    // Send email
                    try
                    {
                        await _emailService.SendInvoicePaymentNotificationAsync(
                            participant.AppUser.Email,
                            participant.AppUser.DisplayName,
                            invoice.Title,
                            invoiceUrl,
                            participantAttachments
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other participants
                        Console.WriteLine($"Error sending email to {participant.AppUser.Email}: {ex.Message}");
                    }
                }

                return Unit.Value;
            }
        }
    }
}

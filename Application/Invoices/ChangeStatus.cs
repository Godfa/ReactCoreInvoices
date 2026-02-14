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
            private readonly IPdfService _pdfService;
            private readonly IConfiguration _config;

            public Handler(DataContext context, IEmailService emailService, IPdfService pdfService, IConfiguration config)
            {
                _context = context;
                _emailService = emailService;
                _pdfService = pdfService;
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

                    // Generate full invoice PDF
                    var attachments = new List<EmailAttachment>();
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
                        Console.WriteLine($"Error generating full invoice PDF: {ex.Message}");
                    }

                    // Send email to all participants
                    if (invoice.Participants != null && invoice.Participants.Any())
                    {
                        foreach (var participant in invoice.Participants)
                        {
                            if (participant.AppUser != null && !string.IsNullOrEmpty(participant.AppUser.Email))
                            {
                                var participantAttachments = new List<EmailAttachment>(attachments);
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
                                    Console.WriteLine($"Error generating participant PDF for {participant.AppUserId}: {ex.Message}");
                                }

                                await _emailService.SendInvoicePaymentNotificationAsync(
                                    participant.AppUser.Email,
                                    participant.AppUser.DisplayName,
                                    invoice.Title,
                                    invoiceUrl,
                                    participantAttachments
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

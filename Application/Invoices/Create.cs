using System;
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
    public class Create
    {
        public class Command : IRequest<Invoice>
        {
            public Invoice Invoice { get; set; }
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
                // Validate that no active invoice exists
                var hasActive = await _context.Invoices
                    .AnyAsync(i => i.Status == InvoiceStatus.Aktiivinen, cancellationToken);

                if (hasActive)
                {
                    throw new Exception("Uutta laskua ei voi luoda, kun olemassa oleva lasku on aktiivinen. Arkistoi ensin nykyinen lasku.");
                }

                // Generate new Id if not provided or empty
                if (request.Invoice.Id == Guid.Empty)
                {
                    request.Invoice.Id = Guid.NewGuid();
                }

                // Auto-generate LanNumber as max + 1
                var maxLanNumber = await _context.Invoices
                    .MaxAsync(i => (int?)i.LanNumber, cancellationToken) ?? 0;

                request.Invoice.LanNumber = maxLanNumber + 1;

                // Auto-generate Title if not provided
                if (string.IsNullOrWhiteSpace(request.Invoice.Title))
                {
                    request.Invoice.Title = $"Mökkilan {request.Invoice.LanNumber}";
                }

                // Clear navigation properties to avoid entity tracking conflicts
                // Participants and ExpenseItems should be added separately through their own endpoints
                request.Invoice.Participants = null;
                request.Invoice.ExpenseItems = null;

                _context.Invoices.Add(request.Invoice);
                await _context.SaveChangesAsync();

                // Send creation notification to all usual suspects
                var appUrl = _config["Email:AppUrl"];
                var invoiceUrl = $"{appUrl}/invoices/{request.Invoice.Id}";
                var usualSuspects = new[] { "Epi", "JHattu", "Leivo", "Timo", "Jaapu", "Urpi", "Zeip" };

                var recipients = await _context.Users
                    .Where(u => usualSuspects.Contains(u.DisplayName) && u.Email != null && u.Email != "")
                    .ToListAsync(cancellationToken);

                foreach (var user in recipients)
                {
                    try
                    {
                        await _emailService.SendInvoiceReviewNotificationAsync(
                            user.Email,
                            user.DisplayName,
                            request.Invoice.Title,
                            invoiceUrl
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send creation notification to {user.Email}: {ex.Message}");
                    }
                }

                return request.Invoice;
            }
        }
    }
}
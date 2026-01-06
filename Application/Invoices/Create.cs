using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Invoice> Handle(Command request, CancellationToken cancellationToken)
            {
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

                return request.Invoice;
            }
        }

    }
}
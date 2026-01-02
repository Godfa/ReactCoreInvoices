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
        public class Command : IRequest
        {
            public Invoice Invoice { get; set; }
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
                // Auto-generate LanNumber as max + 1
                var maxLanNumber = await _context.Invoices
                    .MaxAsync(i => (int?)i.LanNumber, cancellationToken) ?? 0;

                request.Invoice.LanNumber = maxLanNumber + 1;

                _context.Invoices.Add(request.Invoice);

                await _context.SaveChangesAsync();

                return Unit.Value;
            }
        }

    }
}
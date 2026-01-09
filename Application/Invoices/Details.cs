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
    public class Details
    {
        public class Query : IRequest<Invoice>
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Query, Invoice>
        {
        private readonly DataContext _context;
        
            public Handler(DataContext context)
            {
                _context = context;              
            }

            public async Task<Invoice> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.Invoices
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                        .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.LineItems)
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.AppUser)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            }
        }
    }
}
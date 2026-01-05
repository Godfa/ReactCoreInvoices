using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseItems
{
    public class Details
    {
        public class Query : IRequest<ExpenseItem>
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Query, ExpenseItem>
        {
            private readonly DataContext _context;
        
            public Handler(DataContext context)
            {
                _context = context;              
            }

            public async Task<ExpenseItem> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.ExpenseItems
                    .Include(ei => ei.LineItems)
                    .Include(ei => ei.Payers)
                        .ThenInclude(p => p.Creditor)
                    .FirstOrDefaultAsync(ei => ei.Id == request.Id, cancellationToken);
            }
        }
    }
}

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
    public class List
    {
        public class Query : IRequest<List<ExpenseItem>> { }

        public class Handler : IRequestHandler<Query, List<ExpenseItem>>
        {
            private readonly DataContext _context;
            public Handler(DataContext context)
            {
                _context = context;

            }

            public async Task<List<ExpenseItem>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.ExpenseItems
                    .Include(ei => ei.Payers)
                        .ThenInclude(p => p.Creditor)
                    .Include(ei => ei.LineItems)
                    .ToListAsync(cancellationToken);
            }

        }
    }
}

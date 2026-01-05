using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseLineItems
{
    public class List
    {
        public class Query : IRequest<List<ExpenseLineItem>>
        {
            public Guid ExpenseItemId { get; set; }
        }

        public class Handler : IRequestHandler<Query, List<ExpenseLineItem>>
        {
            private readonly DataContext _context;
            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<List<ExpenseLineItem>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.ExpenseLineItems
                    .Where(eli => eli.ExpenseItemId == request.ExpenseItemId)
                    .OrderBy(eli => eli.Name)
                    .ToListAsync(cancellationToken);
            }
        }
    }
}

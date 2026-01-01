using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Creditors
{
    public class List
    {
        public class Query : IRequest<List<Creditor>> { }

        public class Handler : IRequestHandler<Query, List<Creditor>>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<List<Creditor>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.Creditors.ToListAsync(cancellationToken);
            }
        }
    }
}

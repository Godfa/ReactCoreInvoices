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
    public class List
    {
        public class Query : IRequest<List<Invoice>> {}

        public class Handler : IRequestHandler<Query, List<Invoice>>
    {
        private readonly DataContext _context;
        public Handler(DataContext context)
        {
            _context = context;

        }

        public async Task<List<Invoice>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _context.Invoices.ToListAsync(cancellationToken);
        }

    }
    }

    
}
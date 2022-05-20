using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Domain;
using MediatR;
using Persistence;

namespace Application.Invoices
{
    public class Edit
    {
        public class Command : IRequest
        {
            public Invoice Invoice { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
            public Handler(DataContext context, IMapper mapper)
            {
                _mapper = mapper;
                _context = context;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var invoice = await _context.Invoices.FindAsync(request.Invoice.Id);

                _mapper.Map(request.Invoice, invoice);
                await _context.SaveChangesAsync();

                return Unit.Value;
            }
        }
    }
}
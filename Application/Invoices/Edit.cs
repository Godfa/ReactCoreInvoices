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

                if (invoice == null) return Unit.Value;

                // Only map simple properties, not navigation properties
                // to avoid entity tracking conflicts
                invoice.Title = request.Invoice.Title;
                invoice.Description = request.Invoice.Description;
                invoice.Image = request.Invoice.Image;
                invoice.Status = request.Invoice.Status;

                await _context.SaveChangesAsync();

                return Unit.Value;
            }
        }
    }
}
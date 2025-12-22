using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
// using FluentValidation;
using Domain;
using MediatR;
using Persistence;

namespace Application.ExpenseItems
{
    public class Edit
    {
        public class Command : IRequest
        {
            public ExpenseItem ExpenseItem { get; set; }
        }

        // public class CommandValidator : AbstractValidator<Command>
        // {
        //     public CommandValidator()
        //     {
        //         RuleFor(x => x.ExpenseItem).SetValidator(new ExpenseItemValidator());
        //     }
        // }

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
                var expenseItem = await _context.ExpenseItems.FindAsync(new object[]{request.ExpenseItem.Id}, cancellationToken);

                if (expenseItem == null) return Unit.Value; // Or throw NotFound

                _mapper.Map(request.ExpenseItem, expenseItem);
                
                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.ExpenseLineItems
{
    public class Edit
    {
        public class Command : IRequest
        {
            public Guid Id { get; set; }
            public ExpenseLineItem ExpenseLineItem { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.ExpenseLineItem).SetValidator(new ExpenseLineItemValidator());
            }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            public Handler(DataContext context, IMapper mapper)
            {
                _context = context;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var expenseLineItem = await _context.ExpenseLineItems
                    .FindAsync(new object[] { request.Id }, cancellationToken);

                if (expenseLineItem == null)
                {
                    throw new Exception("ExpenseLineItem not found");
                }

                _mapper.Map(request.ExpenseLineItem, expenseLineItem);

                await _context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
        }
    }
}

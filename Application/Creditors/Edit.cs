using System.Threading;
using System.Threading.Tasks;
using Domain;
using FluentValidation;
using MediatR;
using Persistence;

namespace Application.Creditors
{
    public class Edit
    {
        public class Command : IRequest
        {
            public Creditor Creditor { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Creditor.Name)
                    .NotEmpty().WithMessage("Name is required")
                    .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
            }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var creditor = await _context.Creditors.FindAsync(new object[] { request.Creditor.Id }, cancellationToken);

                if (creditor == null)
                    throw new KeyNotFoundException($"Creditor with id {request.Creditor.Id} not found");

                creditor.Name = request.Creditor.Name;

                await _context.SaveChangesAsync(cancellationToken);
                return Unit.Value;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using MediatR;

namespace Application.ExpenseTypes
{
    public class List
    {
        public class Query : IRequest<List<KeyValuePair<int, string>>> { }

        public class Handler : IRequestHandler<Query, List<KeyValuePair<int, string>>>
        {
            public Task<List<KeyValuePair<int, string>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var types = Enum.GetValues(typeof(ExpenseType))
                    .Cast<ExpenseType>()
                    .Select(e => new KeyValuePair<int, string>((int)e, e.ToString()))
                    .ToList();
                
                return Task.FromResult(types);
            }
        }
    }
}

using Application.ExpenseTypes;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ExpenseTypesController : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<List<KeyValuePair<int, string>>>> GetExpenseTypes()
        {
            return await Mediator.Send(new List.Query());
        }
    }
}

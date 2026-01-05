using Application.ExpenseLineItems;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class ExpenseLineItemsController : BaseApiController
    {
        [HttpGet("{expenseItemId}")]
        public async Task<ActionResult<List<ExpenseLineItem>>> GetLineItems(Guid expenseItemId)
        {
            return await Mediator.Send(new List.Query { ExpenseItemId = expenseItemId });
        }

        [HttpPost("{expenseItemId}")]
        public async Task<IActionResult> CreateLineItem(Guid expenseItemId, ExpenseLineItem expenseLineItem)
        {
            expenseLineItem.ExpenseItemId = expenseItemId;
            return Ok(await Mediator.Send(new Create.Command { ExpenseLineItem = expenseLineItem }));
        }

        [HttpPut("{expenseItemId}/items/{id}")]
        public async Task<IActionResult> EditLineItem(Guid expenseItemId, Guid id, ExpenseLineItem expenseLineItem)
        {
            expenseLineItem.Id = id;
            expenseLineItem.ExpenseItemId = expenseItemId;
            return Ok(await Mediator.Send(new Edit.Command { Id = id, ExpenseLineItem = expenseLineItem }));
        }

        [HttpDelete("{expenseItemId}/items/{id}")]
        public async Task<IActionResult> DeleteLineItem(Guid expenseItemId, Guid id)
        {
            return Ok(await Mediator.Send(new Delete.Command { Id = id }));
        }
    }
}

using Application.ExpenseItems;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ExpenseItemsController : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<List<ExpenseItem>>> GetExpenseItems()
        {
            return await Mediator.Send(new List.Query());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseItem>> GetExpenseItem(Guid id)
        {
            return await Mediator.Send(new Details.Query{Id = id});
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpenseItem(ExpenseItem expenseItem)
        {
            // Note: invoiceId must be 0000.. if creating orphan, or provided if creating linked?
            // Since we can't easily pass the invoiceId in the body alongside ExpenseItem entity via default binding easily 
            // without a DTO or separate param, let's assume it's orphan or user uses a wrapper, 
            // BUT Create.Command expects InvoiceId.
            // If we use [FromQuery] for InvoiceId it is cleaner.
            
            return Ok(await Mediator.Send(new Create.Command { ExpenseItem = expenseItem }));
        }
        
        [HttpPost("{invoiceId}")]
        public async Task<IActionResult> CreateExpenseItemForInvoice(Guid invoiceId, ExpenseItem expenseItem)
        {
            return Ok(await Mediator.Send(new Create.Command { InvoiceId = invoiceId, ExpenseItem = expenseItem }));
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> EditExpenseItem(Guid id, ExpenseItem expenseItem)
        {
            expenseItem.Id = id;
            return Ok(await Mediator.Send(new Edit.Command{ExpenseItem = expenseItem}));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpenseItem(Guid id)
        {
            return Ok(await Mediator.Send(new Delete.Command{Id = id}));
        }
    }
}

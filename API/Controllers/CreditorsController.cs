using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Creditors;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class CreditorsController : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<List<Creditor>>> GetCreditors()
        {
            return await Mediator.Send(new List.Query());
        }

        [HttpPost]
        public async Task<IActionResult> CreateCreditor(Creditor creditor)
        {
            await Mediator.Send(new Create.Command { Creditor = creditor });
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditCreditor(int id, Creditor creditor)
        {
            creditor.Id = id;
            await Mediator.Send(new Edit.Command { Creditor = creditor });
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCreditor(int id)
        {
            await Mediator.Send(new Delete.Command { Id = id });
            return Ok();
        }
    }
}

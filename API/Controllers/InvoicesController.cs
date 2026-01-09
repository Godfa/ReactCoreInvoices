using Application.Invoices;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class InvoicesController : BaseApiController
    {
        

        [HttpGet]
        public async Task<ActionResult<List<Invoice>>> GetInvoices()
        {
            return await Mediator.Send(new List.Query());
        }

        
        [HttpGet("{Id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(Guid id)
        {
            return await Mediator.Send(new Details.Query{Id = id});
        }

        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            return Ok(await Mediator.Send(new Create.Command {Invoice = invoice}));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditInvoice(Guid id, Invoice invoice)
        {
            invoice.Id = id;
            return Ok(await Mediator.Send(new Edit.Command{Invoice = invoice}));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(Guid id)
        {
            return Ok(await Mediator.Send(new Delete.Command{Id = id}));
        }

        [HttpPost("{invoiceId}/participants/{creditorId}")]
        public async Task<IActionResult> AddParticipant(Guid invoiceId, int creditorId)
        {
            await Mediator.Send(new AddParticipant.Command { InvoiceId = invoiceId, CreditorId = creditorId });
            return Ok();
        }

        [HttpDelete("{invoiceId}/participants/{creditorId}")]
        public async Task<IActionResult> RemoveParticipant(Guid invoiceId, int creditorId)
        {
            await Mediator.Send(new RemoveParticipant.Command { InvoiceId = invoiceId, CreditorId = creditorId });
            return Ok();
        }

        [HttpPut("{invoiceId}/status/{status}")]
        public async Task<IActionResult> ChangeInvoiceStatus(Guid invoiceId, int status)
        {
            return Ok(await Mediator.Send(new ChangeStatus.Command
            {
                InvoiceId = invoiceId,
                NewStatus = (InvoiceStatus)status
            }));
        }
    }
}
using System.Security.Claims;
using Application.Invoices;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
            return await Mediator.Send(new Details.Query { Id = id });
        }

        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            return Ok(await Mediator.Send(new Create.Command { Invoice = invoice }));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditInvoice(Guid id, Invoice invoice)
        {
            invoice.Id = id;
            return Ok(await Mediator.Send(new Edit.Command { Invoice = invoice }));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvoice(Guid id)
        {
            return Ok(await Mediator.Send(new Delete.Command { Id = id }));
        }

        [HttpPost("{invoiceId}/participants/{userId}")]
        public async Task<IActionResult> AddParticipant(Guid invoiceId, string userId)
        {
            await Mediator.Send(new AddParticipant.Command { InvoiceId = invoiceId, AppUserId = userId });
            return Ok();
        }

        [HttpDelete("{invoiceId}/participants/{userId}")]
        public async Task<IActionResult> RemoveParticipant(Guid invoiceId, string userId)
        {
            await Mediator.Send(new RemoveParticipant.Command { InvoiceId = invoiceId, AppUserId = userId });
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

        [HttpPost("{invoiceId}/approve/{userId}")]
        public async Task<IActionResult> ApproveInvoice(Guid invoiceId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            return Ok(await Mediator.Send(new ApproveInvoice.Command
            {
                InvoiceId = invoiceId,
                AppUserId = userId,
                CurrentUserId = currentUserId,
                IsAdmin = isAdmin
            }));
        }

        [HttpDelete("{invoiceId}/approve/{userId}")]
        public async Task<IActionResult> UnapproveInvoice(Guid invoiceId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            return Ok(await Mediator.Send(new UnapproveInvoice.Command
            {
                InvoiceId = invoiceId,
                AppUserId = userId,
                CurrentUserId = currentUserId,
                IsAdmin = isAdmin
            }));
        }

        [HttpPost("{invoiceId}/send-payment-notifications")]
        public async Task<IActionResult> SendPaymentNotifications(Guid invoiceId)
        {
            await Mediator.Send(new SendPaymentNotifications.Command { InvoiceId = invoiceId });
            return Ok();
        }

        [HttpPost("{invoiceId}/participants/{userId}/toggle-payment")]
        public async Task<IActionResult> TogglePaymentStatus(Guid invoiceId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            await Mediator.Send(new TogglePaymentStatus.Command
            {
                InvoiceId = invoiceId,
                AppUserId = userId,
                CurrentUserId = currentUserId,
                IsAdmin = isAdmin
            });
            return Ok();
        }

        [HttpGet("{invoiceId}/participants/{userId}/virtual-barcode")]
        public async Task<ActionResult> GetVirtualBarcodeForParticipant(Guid invoiceId, string userId)
        {
            var result = await Mediator.Send(new GetVirtualBarcodeForParticipant.Query
            {
                InvoiceId = invoiceId,
                UserId = userId
            });
            return Ok(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting("PaymentTokenPolicy")]
        [HttpGet("pay-via-token/{token}")]
        public async Task<IActionResult> PayViaToken(Guid token)
        {
            try
            {
                await Mediator.Send(new PayViaToken.Command { Token = token });

                var html = @"<!DOCTYPE html>
<html lang='fi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Maksu vahvistettu - Mökkilan Invoices</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 12px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            padding: 40px;
            max-width: 500px;
            text-align: center;
        }
        .icon {
            font-size: 64px;
            margin-bottom: 20px;
        }
        h1 {
            color: #2d3748;
            margin-bottom: 16px;
            font-size: 28px;
        }
        p {
            color: #4a5568;
            line-height: 1.6;
            margin-bottom: 12px;
        }
        .success {
            color: #38a169;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>✅</div>
        <h1 class='success'>Kiitos!</h1>
        <p>Maksusi on merkitty onnistuneesti maksetuksi.</p>
        <p>Voit nyt sulkea tämän sivun.</p>
    </div>
</body>
</html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                var errorHtml = $@"<!DOCTYPE html>
<html lang='fi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Virhe - Mökkilan Invoices</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 12px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            padding: 40px;
            max-width: 500px;
            text-align: center;
        }}
        .icon {{
            font-size: 64px;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #2d3748;
            margin-bottom: 16px;
            font-size: 28px;
        }}
        p {{
            color: #4a5568;
            line-height: 1.6;
            margin-bottom: 12px;
        }}
        .error {{
            color: #e53e3e;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>⚠️</div>
        <h1 class='error'>Virhe</h1>
        <p>{ex.Message}</p>
        <p>Jos ongelma jatkuu, ota yhteyttä laskun luojaan.</p>
    </div>
</body>
</html>";

                return Content(errorHtml, "text/html");
            }
        }
    }
}
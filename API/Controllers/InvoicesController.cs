using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Controllers
{
    public class InvoicesController : BaseApiController
    {
        private readonly DataContext _context;
        public InvoicesController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Invoice>>> GetInvoices()
        {
            return await _context.Invoices.ToListAsync();
        }

        
        [HttpGet("{Id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(Guid id)
        {
            return await _context.Invoices.FindAsync(id);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Domain
{
    public class User: IdentityUser
    {
        public string DisplayName { get; set; }
        public bool MustChangePassword { get; set; }

        public ICollection<InvoiceParticipant> Invoices { get; set; }
        public ICollection<ExpenseItemPayer> ExpenseItemPayers { get; set; }
    }
}
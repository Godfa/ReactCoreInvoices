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

        // Sensitive user profile data - not included in tokens or seed data
        public string BankAccount { get; set; }
        public string PreferredPaymentMethod { get; set; }

        public ICollection<InvoiceParticipant> Invoices { get; set; }
        public ICollection<ExpenseItemPayer> ExpenseItemPayers { get; set; }
    }
}
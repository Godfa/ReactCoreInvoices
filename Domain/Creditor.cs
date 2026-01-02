using System;
using System.Collections.Generic;

namespace Domain
{
    public class Creditor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public virtual List<InvoiceParticipant> Invoices { get; set; }
        public virtual List<ExpenseItemPayer> ExpenseItems { get; set; }
    }
}

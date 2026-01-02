using System;
using System.Collections.Generic;

namespace Domain
{
    public class Creditor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual List<InvoiceParticipant> Invoices { get; set; }
    }
}

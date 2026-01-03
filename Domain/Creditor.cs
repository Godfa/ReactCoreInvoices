using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain
{
    public class Creditor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [JsonIgnore]
        public virtual List<InvoiceParticipant> Invoices { get; set; }

        [JsonIgnore]
        public virtual List<ExpenseItemPayer> ExpenseItems { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Domain
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public int LanNumber { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public string Image { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Aktiivinen;

        public virtual List<ExpenseItem> ExpenseItems { get; set; }
        public virtual List<InvoiceParticipant> Participants { get; set; }
        public virtual List<InvoiceApproval> Approvals { get; set; }

        [NotMapped]
        public decimal Amount => ExpenseItems?.Sum(ei => ei.Amount) ?? 0;

    }
}
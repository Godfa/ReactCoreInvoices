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
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        public string Image { get; set; }

        public virtual List<ExpenseItem> ExpenseItems { get; set; }
        public virtual List<InvoiceParticipant> Participants { get; set; }

    }
}
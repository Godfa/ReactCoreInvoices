using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Domain
{
     public class ExpenseItem
    {
        public Guid Id { get; set; }
        public int ExpenseCreditor { get; set; }
        public ExpenseType ExpenseType { get; set; }
        public string Name { get; set; }

        [NotMapped]
        public decimal Amount => LineItems?.Sum(li => li.Total) ?? 0;

        public virtual List<ExpenseItemPayer> Payers { get; set; }
        public virtual List<ExpenseLineItem> LineItems { get; set; }
    }
}
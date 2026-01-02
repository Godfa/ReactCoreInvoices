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
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
    }
}
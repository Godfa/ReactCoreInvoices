using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain
{
     public class ExpenseItem        
    {
        public Guid Id { get; set; }
        //pyublic List<User> ExpenseDebitors { get; set; }
        public int ExpenseCreditor { get; set; }
        public ExpenseType ExpenseType { get; set; }
        public string Name { get; set; }
    }
}
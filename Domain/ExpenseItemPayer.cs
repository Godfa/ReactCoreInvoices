using System;
using System.Text.Json.Serialization;

namespace Domain
{
    public class ExpenseItemPayer
    {
        public Guid ExpenseItemId { get; set; }

        [JsonIgnore]
        public ExpenseItem ExpenseItem { get; set; }

        public int CreditorId { get; set; }
        public Creditor Creditor { get; set; }
    }
}

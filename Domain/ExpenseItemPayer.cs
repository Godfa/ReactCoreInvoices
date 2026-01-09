using System;
using System.Text.Json.Serialization;

namespace Domain
{
    public class ExpenseItemPayer
    {
        public Guid ExpenseItemId { get; set; }

        [JsonIgnore]
        public ExpenseItem ExpenseItem { get; set; }

        public string AppUserId { get; set; }
        public User AppUser { get; set; }
    }
}

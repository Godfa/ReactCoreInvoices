using System;

namespace Domain
{
    public class InvoiceApproval
    {
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; }
        public string AppUserId { get; set; }
        public User AppUser { get; set; }
        public DateTime ApprovedAt { get; set; }
    }
}

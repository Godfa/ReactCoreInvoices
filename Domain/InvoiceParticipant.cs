using System;

namespace Domain
{
    public class InvoiceParticipant
    {
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; }

        public int CreditorId { get; set; }
        public Creditor Creditor { get; set; }
    }
}

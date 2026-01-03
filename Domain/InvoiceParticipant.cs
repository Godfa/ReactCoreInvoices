using System;
using System.Text.Json.Serialization;

namespace Domain
{
    public class InvoiceParticipant
    {
        public Guid InvoiceId { get; set; }

        [JsonIgnore]
        public Invoice Invoice { get; set; }

        public int CreditorId { get; set; }
        public Creditor Creditor { get; set; }
    }
}

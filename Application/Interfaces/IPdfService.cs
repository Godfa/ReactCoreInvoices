using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPdfService
    {
        /// <summary>
        /// Generates PDF for full invoice with all expense details
        /// </summary>
        Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);

        /// <summary>
        /// Generates PDF for individual participant showing their personal share
        /// </summary>
        Task<byte[]> GenerateParticipantInvoicePdfAsync(Guid invoiceId, string participantId);
    }
}

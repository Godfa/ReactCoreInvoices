using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendNewUserEmailAsync(string email, string displayName, string username, string temporaryPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string displayName, string temporaryPassword);
        Task<bool> SendPasswordResetLinkAsync(string email, string displayName, string resetLink);
        Task<bool> SendInvoiceReviewNotificationAsync(string email, string displayName, string invoiceTitle, string invoiceUrl);
    }
}

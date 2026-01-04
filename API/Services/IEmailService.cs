using System.Threading.Tasks;

namespace API.Services
{
    public interface IEmailService
    {
        Task<bool> SendNewUserEmailAsync(string email, string displayName, string username, string temporaryPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string displayName, string temporaryPassword);
    }
}

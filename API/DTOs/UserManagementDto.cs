namespace API.DTOs
{
    public class UserManagementDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public class CreateUserDto
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }

    public class UpdateUserDto
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }

    public class SendPasswordResetDto
    {
        public string UserId { get; set; }
    }
}

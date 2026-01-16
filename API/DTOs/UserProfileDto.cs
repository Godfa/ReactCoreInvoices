namespace API.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string BankAccount { get; set; }
        public string PreferredPaymentMethod { get; set; }
    }

    public class UpdateProfileDto
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string BankAccount { get; set; }
        public string PreferredPaymentMethod { get; set; }
    }
}

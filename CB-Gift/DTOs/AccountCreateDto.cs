namespace CB_Gift.DTOs
{
    public class AccountCreateDto
    {
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public string Password { get; set; } = default!;
    }
}

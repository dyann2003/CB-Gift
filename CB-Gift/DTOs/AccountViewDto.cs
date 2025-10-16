namespace CB_Gift.DTOs
{
    public class AccountViewDto
    {
        public string Id { get; set; } = default!;
        public string? FullName { get; set; }
        public string Email { get; set; } = default!;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}

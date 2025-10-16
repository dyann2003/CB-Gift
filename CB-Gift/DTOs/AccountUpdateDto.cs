namespace CB_Gift.DTOs
{
    public class AccountUpdateDto
    {
        public string FullName { get; set; } = default!;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}

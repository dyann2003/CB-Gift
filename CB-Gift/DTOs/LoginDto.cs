namespace CB_Gift.DTOs
{
    public class LoginDto
    {
        public string UserNameOrEmail { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}

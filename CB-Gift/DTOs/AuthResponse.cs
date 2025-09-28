namespace CB_Gift.DTOs
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string? Email { get; set; }
        public AuthResponse(string token, string userName, string? email)
        { AccessToken = token; UserName = userName; Email = email; }
    }
}

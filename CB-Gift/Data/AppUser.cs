using Microsoft.AspNetCore.Identity;

namespace CB_Gift.Data
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

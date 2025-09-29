using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Data
{
    public class CBGiftDbContext : IdentityDbContext<AppUser, IdentityRole, string>
    {
        public CBGiftDbContext(DbContextOptions<CBGiftDbContext> options) : base(options) { }
    }


}

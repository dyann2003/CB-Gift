using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CB_Gift.Data;

namespace CBGift.Tests.Testing
{
    public static class IdentityMocks
    {
        public static UserManager<AppUser> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var pwdHasher = new Mock<IPasswordHasher<AppUser>>();
            var userValidators = new[] { new Mock<IUserValidator<AppUser>>().Object };
            var pwdValidators = new[] { new Mock<IPasswordValidator<AppUser>>().Object };
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new IdentityErrorDescriber();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<AppUser>>>();

            return new UserManager<AppUser>(store.Object, options.Object, pwdHasher.Object,
                userValidators, pwdValidators, keyNormalizer.Object, errors, services.Object, logger.Object);
        }

        public static SignInManager<AppUser> MockSignInManager(UserManager<AppUser> userManager)
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<AppUser>>>();

            return new SignInManager<AppUser>(userManager, contextAccessor.Object, claimsFactory.Object, options.Object, logger.Object, null, null);
        }
    }
}
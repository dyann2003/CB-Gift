using CB_Gift.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CB_Gift.Validators
{
    public class AccountValidators
    {
        public static (bool ok, string? error) ValidateCreate(AccountCreateDto x)
        {
            if (string.IsNullOrWhiteSpace(x.FullName)) return (false, "Name is required");
            if (x.FullName.Length > 256) return (false, "Name max length 256");
            if (string.IsNullOrWhiteSpace(x.Email)) return (false, "Email is required");
            if (!new EmailAddressAttribute().IsValid(x.Email)) return (false, "Email is invalid");
            if (!string.IsNullOrWhiteSpace(x.PhoneNumber) &&
                !Regex.IsMatch(x.PhoneNumber, @"^\+?[0-9]{7,15}$"))
                return (false, "Phone is invalid");
            if (string.IsNullOrWhiteSpace(x.Role)) return (false, "Role is required");
            if (string.IsNullOrWhiteSpace(x.Password) || x.Password.Length < 6)
                return (false, "Password min length 6");
            return (true, null);
        }

        public static (bool ok, string? error) ValidateUpdate(AccountUpdateDto x)
        {
            if (string.IsNullOrWhiteSpace(x.FullName)) return (false, "Name is required");
            if (!string.IsNullOrWhiteSpace(x.PhoneNumber) &&
                !Regex.IsMatch(x.PhoneNumber, @"^\+?[0-9]{7,15}$"))
                return (false, "Phone is invalid");
            if (string.IsNullOrWhiteSpace(x.Role)) return (false, "Role is required");
            return (true, null);
        }
    }
}

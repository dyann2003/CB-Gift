using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        public string Email { get; set; }
    }
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Full Name is required")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]{3,50}$",
       ErrorMessage = "Full Name must be 3-50 characters, letters only, no special characters.")]
        public string FullName { get; set; }

        [RegularExpression(@"^0\d{9}$",
            ErrorMessage = "Phone number must be 10 digits and start with 0.")]
        public string? PhoneNumber { get; set; }
    }
}

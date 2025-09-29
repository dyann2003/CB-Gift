using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        public string Email { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TicketSystem.Models
{
   
    // we use seperate model for logging in for better security 
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        // i shoul change the Password  to change the minimal of 6 characters;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

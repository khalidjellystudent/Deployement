using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";
    }

}

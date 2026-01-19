using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
    public class Violation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 1_000_000)]
        public decimal Cost { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

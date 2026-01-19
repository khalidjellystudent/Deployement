using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = "";
        [Required]
        public string Message { get; set; }= "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } // default false
    }

}

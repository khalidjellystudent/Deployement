using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
    public class Ticket
    {
        [Key]
        public int Ticket_Id { get; set; }

        public string Violations { get; set; } = ""; // e.g., "speeding,parking"

        public string Plate_Number { get; set; } = "";
        public string Violation_Place { get; set; } = "";
        public string IssuedBy { get; set; } = "";
        public string Notes { get; set; }  = "";
        public string Status { get; set; } = "Pending";
        public DateTime Ticket_Time { get; set; } = DateTime.Now;
        public DateTime Due_Date { get; set; } = DateTime.Today;
        public bool Appealed { get; set; } = false;
        public decimal FineAmount { get; set; }

        [Column(TypeName = "varbinary(max)")]
        public byte[]? EvidenceImageData { get; set; }

        public string? EvidenceImageContentType { get; set; }

        [NotMapped]
        public string? EvidenceImageTempPath { get; set; }

        public string? VoiceReportPath { get; set; }

        [ForeignKey("Users")]
        public string Email { get; set; } = "";
        public User Users { get; set; }=  null!;
    }
}

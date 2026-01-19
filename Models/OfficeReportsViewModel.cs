namespace TicketSystem.Models;

public class OfficeReportsViewModel
{
    public int TicketsLastMonth { get; set; }
    public int TicketsThisYear { get; set; }
    public int TotalTickets { get; set; }

    public int TotalUsers { get; set; }

    public int PaidTickets { get; set; }
    public int UnpaidTickets { get; set; }

    // Index 0..11 => Jan..Dec
    public int[] MonthlyTicketsThisYear { get; set; } = new int[12];

    // Last 30 days trend (same length arrays)
    public string[] Last30DaysLabels { get; set; } = Array.Empty<string>();
    public int[] Last30DaysTicketCounts { get; set; } = Array.Empty<int>();

    // Top violations (parsed from Ticket.Violations)
    public string[] TopViolationLabels { get; set; } = Array.Empty<string>();
    public int[] TopViolationCounts { get; set; } = Array.Empty<int>();
}

using TicketSystem.Models;

public class OfficerDashboardViewModel
{
    public int TicketsToday { get; set; }
    public int PaidTickets { get; set; }
    public int PendingTickets { get; set; }
    public int MonthlyTickets { get; set; }
    public double PaidPercentage { get; set; }
    public int TotalTickets { get; set; }
    public List<Ticket> RecentTickets { get; set; }  = new List<Ticket>(); //   what did this do and does it break any thing?
}
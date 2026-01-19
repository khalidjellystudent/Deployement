namespace TicketSystem.Models
{
    public class OfficerStatsViewModel
    {
        public User Officer { get; set; } = null!;
        public int TotalTickets { get; set; }
        public decimal TotalFines { get; set; }
        public List<MonthlyStat> TicketsPerMonth { get; set; }  = new List<MonthlyStat>();
        public List<ViolationStat> ViolationBreakdown { get; set; } = new List<ViolationStat>();
    }

    public class MonthlyStat
    {
        public string Month { get; set; } = "";
        public int Count { get; set; }
    }

    public class ViolationStat
    {
        public string ViolationType { get; set; } = "";
        public int Count { get; set; }
    }
}
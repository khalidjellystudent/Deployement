
using Microsoft.EntityFrameworkCore;
using TicketSystem.Models;

namespace TicketSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<Violation> Violations { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }

        


    }

}



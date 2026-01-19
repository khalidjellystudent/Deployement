using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;
using TicketSystem.Models;



namespace TicketSystem.Controllers
{ 

    [Authorize(Roles = "User")]
    public class UserController : Controller
        {
            private readonly AppDbContext _db;

            public UserController(AppDbContext db)
            {
                _db = db;
            }

        public IActionResult Index()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var tickets = _db.Tickets
                .Include(t => t.Users) // Include user data if needed
                .Where(t => t.Email == userEmail)
                .OrderByDescending(t => t.Ticket_Time)
                .ToList();

            return View(tickets);
        }

        public IActionResult DetailedTicket(int id)
            {
                var ticket = _db.Tickets
                    .Include(t => t.Users)
                    .FirstOrDefault(t => t.Ticket_Id == id);

                if (ticket == null || ticket.Email != User.FindFirstValue(ClaimTypes.Email))
                {
                    return NotFound();
                }

                return View(ticket);
            }


            //   ****************Paying methods**********
            // In your TicketController.cs (or similar)
        public IActionResult PayTicket(int id)
        {
             var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == id);
              if (ticket == null)
              {
                  return NotFound();
              }
           return View(ticket); // Pass the ticket to the payment page
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(int ticketId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);

            if (ticket == null || string.IsNullOrWhiteSpace(userEmail) ||
                !string.Equals(ticket.Email?.Trim(), userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            ticket.Status = "Paid";
            _db.SaveChanges();
            return RedirectToAction("Index", new { message = "Payment successful!" });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AskRevoke_Ticket(int ticketId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);

            if (ticket == null || string.IsNullOrWhiteSpace(userEmail) ||
                !string.Equals(ticket.Email?.Trim(), userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            ticket.Status = "UnderProcess";
            ticket.Appealed = true;
            _db.SaveChanges();
            return RedirectToAction("Index", new { message = "tickets under individuals process" });
        }





    }
}
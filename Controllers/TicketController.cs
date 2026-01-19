using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;

namespace TicketSystem.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly AppDbContext _db;

        public TicketController(AppDbContext db)
        {
            _db = db;
        }

        // A single authenticated entry point for QR scans.
        // Redirects to the correct role-specific ticket details page.
        [HttpGet]
        public IActionResult Open(int id)
        {
            var ticket = _db.Tickets
                .AsNoTracking()
                .FirstOrDefault(t => t.Ticket_Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (User.IsInRole("User"))
            {
                var userEmailRaw = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.Identity?.Name;

                var userEmail = (userEmailRaw ?? string.Empty).Trim();
                var ticketEmail = (ticket.Email ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    TempData["AccessDeniedMessage"] = "Your session is missing required identity information. Please log in again.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (string.IsNullOrWhiteSpace(ticketEmail) ||
                    !string.Equals(ticketEmail, userEmail, System.StringComparison.OrdinalIgnoreCase))
                {
                    TempData["AccessDeniedMessage"] = "You can't access another user's ticket.";
                    return RedirectToAction("AccessDenied", "Home");
                }

                return RedirectToAction("DetailedTicket", "User", new { id });
            }

            if (User.IsInRole("Officer"))
            {
                return RedirectToAction("DetailedTicket", "Officer", new { id });
            }

            if (User.IsInRole("Office"))
            {
                return RedirectToAction("DetailedTicket", "Office", new { id });
            }

            if (User.IsInRole("Admin"))
            {
                // No Admin ticket details page currently; send to Home.
                TempData["AccessDeniedMessage"] = "Admin ticket view is not configured.";
                return RedirectToAction("AccessDenied", "Home");
            }

            TempData["AccessDeniedMessage"] = "You do not have permission to view this ticket.";
            return RedirectToAction("AccessDenied", "Home");
        }
    }
}

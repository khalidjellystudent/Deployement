using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
     //[Authorize(Roles = "Office")]
    public class OfficeController : Controller
    {
        private readonly AppDbContext _db;
        public OfficeController(AppDbContext context)
        {
            _db = context;
        }
        
        public IActionResult Index()
        {
            return View();
        }   
        public IActionResult ScanPlate()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ScanTicketQr()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            var startOfThisYear = new DateTime(now.Year, 1, 1);
            var startOfNextYear = startOfThisYear.AddYears(1);

            // NOTE: Status values in this project are free-form strings.
            // We'll treat "Paid" (case-insensitive) as paid, everything else as unpaid.
            var paidQuery = _db.Tickets.Where(t => t.Status != null && t.Status.ToLower() == "paid");

            var model = new OfficeReportsViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalTickets = await _db.Tickets.CountAsync(),

                TicketsLastMonth = await _db.Tickets.CountAsync(t => t.Ticket_Time >= startOfLastMonth && t.Ticket_Time < startOfThisMonth),
                TicketsThisYear = await _db.Tickets.CountAsync(t => t.Ticket_Time >= startOfThisYear && t.Ticket_Time < startOfNextYear),

                PaidTickets = await paidQuery.CountAsync(),
            };

            model.UnpaidTickets = Math.Max(0, model.TotalTickets - model.PaidTickets);

            // Monthly tickets for the current year (Jan..Dec)
            var monthly = await _db.Tickets
                .Where(t => t.Ticket_Time >= startOfThisYear && t.Ticket_Time < startOfNextYear)
                .GroupBy(t => t.Ticket_Time.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var m in monthly)
            {
                if (m.Month >= 1 && m.Month <= 12)
                {
                    model.MonthlyTicketsThisYear[m.Month - 1] = m.Count;
                }
            }

            // Tickets in the last 30 days (trend)
            var today = DateTime.Today;
            var startDay = today.AddDays(-29);
            var endExclusive = today.AddDays(1);

            var daily = await _db.Tickets
                .Where(t => t.Ticket_Time >= startDay && t.Ticket_Time < endExclusive)
                .GroupBy(t => t.Ticket_Time.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var dailyMap = daily.ToDictionary(x => x.Day.Date, x => x.Count);
            var labels = new string[30];
            var counts = new int[30];
            for (var i = 0; i < 30; i++)
            {
                var d = startDay.AddDays(i);
                labels[i] = d.ToString("MM-dd");
                counts[i] = dailyMap.TryGetValue(d.Date, out var c) ? c : 0;
            }
            model.Last30DaysLabels = labels;
            model.Last30DaysTicketCounts = counts;

            // Top violations (all-time) parsed from comma-separated string
            var violationStrings = await _db.Tickets
                .AsNoTracking()
                .Select(t => t.Violations)
                .ToListAsync();

            var violationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in violationStrings)
            {
                if (string.IsNullOrWhiteSpace(v)) continue;
                var parts = v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                {
                    var key = p.Trim();
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    violationCounts[key] = violationCounts.TryGetValue(key, out var existing) ? existing + 1 : 1;
                }
            }

            var top = violationCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(8)
                .ToList();

            model.TopViolationLabels = top.Select(x => x.Key).ToArray();
            model.TopViolationCounts = top.Select(x => x.Value).ToArray();

            return View(model);
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Process()
        {
            // Fetch only tickets whose status is "UnderProcess"
            var tickets = await _db.Tickets
                .Where(t => t.Status == "UnderProcess")
                .OrderByDescending(t => t.Ticket_Time)
                .ToListAsync();

            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTicket(int Ticket_Id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == Ticket_Id);
            if (ticket != null)
            {
                _db.Tickets.Remove(ticket);
                _db.SaveChanges();
            }

            return RedirectToAction("Process"); // Replace with your actual view name
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                if (_db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                try
                {
                    // ✅ Hash the password before saving
                    model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                    _db.Users.Add(model);
                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration successful! Please log in.";
                    return RedirectToAction("Login", "Home"); // Redirect to login instead of Office
                }
                catch (Exception)
                {
                    // In real apps, log the error
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                    return View(model);
                }
            }

            // Return with validation errors
            return View(model);
        }



        [HttpGet]
        public IActionResult SearchTickets(string plateNumber)
        {
            // Search case-insensitive and trim whitespace
            var normalizedPlate = plateNumber?.Trim().ToUpper();

            var tickets = _db.Tickets
                .Include(t => t.Users) // Load related user data
                .Where(t => t.Plate_Number.ToUpper() == normalizedPlate)
                .OrderByDescending(t => t.Ticket_Time)
                .ToList();

            ViewBag.SearchQuery = plateNumber;
            return View("TicketResults", tickets);
        }

        
        


        [HttpGet]
        public IActionResult DetailedTicket(int id)
        {
            var ticket = _db.Tickets
                .Include(t => t.Users)
                .FirstOrDefault(t => t.Ticket_Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket); // Passing single ticket, not a list
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DenieRevoke_Ticket(int ticketId)
        {
            var ticket = _db.Tickets.Find(ticketId);
            if (ticket != null)
            {
                ticket.Status = "Pending";
                _db.SaveChanges();
                return RedirectToAction("Index", new { message = "After reviewing the Ticket The is Appeal Revoked" });
            }
            return View("Error");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(int ticketId)
        {
            var ticket = _db.Tickets.Find(ticketId);
            if (ticket != null)
            {
                ticket.Status = "Paid";
                _db.SaveChanges();
                return RedirectToAction("Index", new { message = "Payment successful!" });
            }
            return View("Error");
        }




    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Models;
using BCrypt.Net;
using System.Data.SqlClient;
using System.IO.Compression;


[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }
    public IActionResult Index()
    {
        // Dashboard Statistics
        var totalUsers = _db.Users.Count();
        var totalOfficers = _db.Users.Count(u => u.Role == "Officer");
        var totalRegularUsers = _db.Users.Count(u => u.Role == "User");
        var totalTickets = _db.Tickets.Count();
        var totalFines = _db.Tickets.Sum(t => (decimal?)t.FineAmount) ?? 0;
        var pendingTickets = _db.Tickets.Count(t => t.Status == "Pending");
        var paidTickets = _db.Tickets.Count(t => t.Status == "Paid");
        var appealedTickets = _db.Tickets.Count(t => t.Appealed);
        var unreadFeedbacks = _db.Feedbacks.Count(f => !f.IsRead);
        var totalFeedbacks = _db.Feedbacks.Count();

        // Recent tickets (last 7 days)
        var sevenDaysAgo = DateTime.Now.AddDays(-7);
        var recentTickets = _db.Tickets.Count(t => t.Ticket_Time >= sevenDaysAgo);

        // Monthly ticket trends (last 6 months)
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var monthlyTickets = _db.Tickets
            .Where(t => t.Ticket_Time >= sixMonthsAgo)
            .GroupBy(t => new { t.Ticket_Time.Year, t.Ticket_Time.Month })
            // switch to in-memory evaluation before formatting the month string
            .AsEnumerable()
            .Select(g => new
            {
                Month = $"{g.Key.Month}/{g.Key.Year}",
                Count = g.Count(),
                TotalFines = g.Sum(t => t.FineAmount)
            })
            .OrderBy(x => x.Month)
            .ToList();

        // Top violations
        var topViolations = _db.Tickets
            // switch to in-memory evaluation so string.Split/Trim are executed client-side
            .AsEnumerable()
            .SelectMany(t => (t.Violations ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(v => v.Trim())
            .Select(g => new { Violation = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        // Top officers by ticket count
        var topOfficers = _db.Tickets
            .GroupBy(t => t.IssuedBy)
            .Select(g => new
            {
                OfficerEmail = g.Key,
                TicketCount = g.Count(),
                TotalFines = g.Sum(t => t.FineAmount)
            })
            .OrderByDescending(x => x.TicketCount)
            .Take(5)
            .ToList();

        // Recent feedbacks
        var recentFeedbacks = _db.Feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .Take(5)
            .ToList();

        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalOfficers = totalOfficers;
        ViewBag.TotalRegularUsers = totalRegularUsers;
        ViewBag.TotalTickets = totalTickets;
        ViewBag.TotalFines = totalFines;
        ViewBag.PendingTickets = pendingTickets;
        ViewBag.PaidTickets = paidTickets;
        ViewBag.AppealedTickets = appealedTickets;
        ViewBag.UnreadFeedbacks = unreadFeedbacks;
        ViewBag.TotalFeedbacks = totalFeedbacks;
        ViewBag.RecentTickets = recentTickets;
        ViewBag.MonthlyTickets = monthlyTickets;
        ViewBag.TopViolations = topViolations;
        ViewBag.TopOfficers = topOfficers;
        ViewBag.RecentFeedbacks = recentFeedbacks;

        return View();
    }
    public IActionResult ResetPassword()
    {
        return View();
    }
    [HttpGet]
    public IActionResult ResetPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest();

        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
            return NotFound();

        var model = new ResetPasswordViewModel
        {
            Email = user.Email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = _db.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user == null)
            return NotFound();

        // ✅ Hash the new password
        user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"Password for {user.Email} has been reset.";
        return RedirectToAction("UserList"); // Or wherever your admin list is
    }


    
    public IActionResult UserList()
    {
        var users = _db.Users.ToList();
        return View(users);
    }


    // feedbacks
 
    public IActionResult FeedbackList()
    {
        var feedbacks = _db.Feedbacks
                           .OrderByDescending(f => f.CreatedAt)
                           .ToList();
        return View(feedbacks);
    }

    // delete feed back massage 

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkAsRead(int id)
    {
        var feedback = _db.Feedbacks.FirstOrDefault(f => f.Id == id);
        if (feedback == null) return NotFound();

        if (!feedback.IsRead)
        {
            feedback.IsRead = true;
            _db.SaveChanges();
        }

        return Ok();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteFeedback(int id)
    {
        var feedback = _db.Feedbacks.FirstOrDefault(f => f.Id == id);
        if (feedback == null)
        {
            TempData["ErrorMessage"] = "لم يتم العثور على الرسالة.";
            return RedirectToAction("FeedbackList");
        }

        _db.Feedbacks.Remove(feedback);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "تم حذف الرسالة بنجاح.";
        return RedirectToAction("FeedbackList");
    }






    
    public IActionResult OfficerList()
    {
        var officers = _db.Users
                          .Where(u => u.Role == "Officer")
                          .ToList();
        return View(officers);
    }

    public IActionResult OfficerDetails(string email)
    {
        var officer = _db.Users.FirstOrDefault(u => u.Email == email && u.Role == "Officer");
        if (officer == null) return NotFound();

        // Tickets issued by this officer
        var tickets = _db.Tickets
                         .Where(t => t.IssuedBy == officer.Email)
                         .ToList();
        
        var stats = new OfficerStatsViewModel
        {
            Officer = officer,
            TotalTickets = tickets.Count,
            TotalFines = tickets.Sum(t => t.FineAmount),
            TicketsPerMonth = tickets
                .GroupBy(t => new { t.Ticket_Time.Year, t.Ticket_Time.Month })
                .AsEnumerable()
                .Select(g => new MonthlyStat
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Count = g.Count()
                }).ToList(),
            ViolationBreakdown = tickets
                .SelectMany(t => t.Violations.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(v => v.Trim())
                .Select(g => new ViolationStat
                {
                    ViolationType = g.Key,
                    Count = g.Count()
                }).ToList()
        };

        return View(stats);
    }


    // Violations management (Admin only)
    [HttpGet]
    public IActionResult ViolationList()
    {
        var violations = _db.Violations
            .OrderByDescending(v => v.IsActive)
            .ThenBy(v => v.Name)
            .ToList();

        return View(violations);
    }

    [HttpGet]
    public IActionResult AddViolation()
    {
        return View(new Violation());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddViolation(Violation model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var name = (model.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(Violation.Name), "Name is required.");
            return View(model);
        }

        var exists = _db.Violations.Any(v => v.Name == name);
        if (exists)
        {
            ModelState.AddModelError(nameof(Violation.Name), "A violation with this name already exists.");
            return View(model);
        }

        model.Name = name;
        _db.Violations.Add(model);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Violation added successfully.";
        return RedirectToAction(nameof(ViolationList));
    }

    [HttpGet]
    public IActionResult EditViolation(int id)
    {
        var violation = _db.Violations.FirstOrDefault(v => v.Id == id);
        if (violation == null) return NotFound();
        return View(violation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditViolation(Violation model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var violation = _db.Violations.FirstOrDefault(v => v.Id == model.Id);
        if (violation == null) return NotFound();

        var name = (model.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(Violation.Name), "Name is required.");
            return View(model);
        }

        var exists = _db.Violations.Any(v => v.Id != model.Id && v.Name == name);
        if (exists)
        {
            ModelState.AddModelError(nameof(Violation.Name), "A violation with this name already exists.");
            return View(model);
        }

        violation.Name = name;
        violation.Cost = model.Cost;
        violation.IsActive = model.IsActive;

        _db.SaveChanges();

        TempData["SuccessMessage"] = "Violation updated successfully.";
        return RedirectToAction(nameof(ViolationList));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteViolation(int id)
    {
        var violation = _db.Violations.FirstOrDefault(v => v.Id == id);
        if (violation == null) return NotFound();

        _db.Violations.Remove(violation);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Violation deleted successfully.";
        return RedirectToAction(nameof(ViolationList));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleViolationActive(int id)
    {
        var violation = _db.Violations.FirstOrDefault(v => v.Id == id);
        if (violation == null) return NotFound();

        violation.IsActive = !violation.IsActive;
        _db.SaveChanges();

        TempData["SuccessMessage"] = violation.IsActive ? "Violation enabled." : "Violation disabled.";
        return RedirectToAction(nameof(ViolationList));
    }
}

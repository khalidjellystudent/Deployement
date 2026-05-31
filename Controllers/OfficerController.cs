using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Generic;
using TicketSystem.Data;
using TicketSystem.Models;


namespace TicketSystem.Controllers
{
    [Authorize(Roles ="Officer")]
    public class OfficerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OfficerController> _logger;
        private readonly IWebHostEnvironment _env;

        public OfficerController(AppDbContext context, ILogger<OfficerController> logger, IWebHostEnvironment env)
        {
            _db = context;
            _logger = logger;
            _env = env;
        }
        public IActionResult Index()
        {
            var currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var today = DateTime.Today;
            var currentMonth = DateTime.Now.Month;

            var totalTickets = _db.Tickets
                .Where(t => t.IssuedBy == currentUserEmail)
                .Count();

            var paidTickets = _db.Tickets
                .Where(t => t.IssuedBy == currentUserEmail && t.Status == "Paid")
                .Count();

            var paidPercentage = totalTickets > 0 ?
                (paidTickets * 100.0 / totalTickets) : 0;

            var dashboardData = new OfficerDashboardViewModel
            {
                TicketsToday = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail && t.Ticket_Time.Date == today)
                    .Count(),

                PaidTickets = paidTickets,

                PendingTickets = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail && t.Status == "Pending")
                    .Count(),

                MonthlyTickets = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail && t.Ticket_Time.Month == currentMonth)
                    .Count(),

                PaidPercentage = paidPercentage,
                TotalTickets = totalTickets,

                RecentTickets = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail)
                    .OrderByDescending(t => t.Ticket_Time)
                    .Take(10)
                    .ToList()
            };

            return View(dashboardData);
        }
        public IActionResult ScanPlate()
        {
            return View();
        }
        public IActionResult IssueTicket()
        {
            ViewBag.Violations = _db.Violations
                .Where(v => v.IsActive)
                .OrderBy(v => v.Name)
                .ToList();

            var ticket = new Ticket
            {
                IssuedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty
            };

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WritingTicket(Ticket u, int[] ViolationIds, IFormFile? voiceReport, IFormFile? evidenceImage)
        {
            // set who issued the ticket
            u.IssuedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            if (evidenceImage == null || evidenceImage.Length == 0)
            {
                ViewData["m"] = "يجب رفع صورة مخالفة واحدة على الأقل قبل إصدار التذكرة.";
                ViewBag.Violations = _db.Violations
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToList();
                return View("IssueTicket", u);
            }

            if (!IsAllowedImageFile(evidenceImage))
            {
                ViewData["m"] = "يُسمح برفع الصور فقط كدليل للمخالفة.";
                ViewBag.Violations = _db.Violations
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToList();
                return View("IssueTicket", u);
            }

            if (ViolationIds == null || ViolationIds.Length == 0)
            {
                ViewData["m"] = "يجب اختيار مخالفة واحدة على الأقل قبل إصدار التذكرة.";
                ViewBag.Violations = _db.Violations
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToList();
                return View("IssueTicket", u);
            }

            var distinctIds = ViolationIds.Distinct().ToArray();
            var selectedViolations = _db.Violations
                .Where(v => v.IsActive && distinctIds.Contains(v.Id))
                .ToList();

            if (selectedViolations.Count != distinctIds.Length)
            {
                ViewData["m"] = "تم اختيار مخالفات غير صالحة. يرجى إعادة المحاولة.";
                ViewBag.Violations = _db.Violations
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToList();
                return View("IssueTicket", u);
            }

            // snapshot names + cost at time of issuing
            u.Violations = string.Join(",", selectedViolations.Select(v => v.Name));
            u.FineAmount = selectedViolations.Sum(v => v.Cost);

            u.EvidenceImageContentType = evidenceImage.ContentType;
            u.EvidenceImageTempPath = SaveEvidenceImageToTempStorage(evidenceImage);

            // handle voice report upload
            if (voiceReport != null && voiceReport.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(voiceReport.ContentType) || !voiceReport.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                {
                    ViewData["m"] = "يُسمح بملفات الصوت فقط للتقرير.";
                    ViewBag.Violations = _db.Violations
                        .Where(v => v.IsActive)
                        .OrderBy(v => v.Name)
                        .ToList();
                    return View("IssueTicket", u);
                }

                var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "audio");
                Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(voiceReport.FileName);
                var safeExt = string.IsNullOrWhiteSpace(ext) ? ".m4a" : ext;
                var fileName = $"ticket_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{safeExt}";
                var fullPath = Path.Combine(uploadsRoot, fileName);

                using (var fs = new FileStream(fullPath, FileMode.Create))
                {
                    voiceReport.CopyTo(fs);
                }

                u.VoiceReportPath = $"/uploads/audio/{fileName}";
            }

            // serialize and pass to confirmation via TempData
            TempData["PendingTicket"] = JsonSerializer.Serialize(u);
            return RedirectToAction("ConfirmTicket");
        }




        // New action to handle the confirmation page
        public IActionResult ConfirmTicket()
        {
            if (TempData["PendingTicket"] == null)
            {
                return RedirectToAction("IssueTicket");
            }

            var ticketJson = TempData["PendingTicket"] as string;
            if (string.IsNullOrEmpty(ticketJson))
            {
                return RedirectToAction("IssueTicket");
            }

            var ticket = JsonSerializer.Deserialize<Ticket?>(ticketJson);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Invalid ticket data.";
                return RedirectToAction("IssueTicket");
            }

            // Store again in TempData for the final submission
            TempData["PendingTicket"] = ticketJson;

            // Check for error message
            if (TempData["ErrorMessage"] != null)
            {
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
            }

            return View(ticket);
        }

        // New action to handle the final submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmTicket(bool confirm)
        {
            if (confirm && TempData["PendingTicket"] != null)
            {
                var ticketJson = TempData["PendingTicket"] as string;
                if (string.IsNullOrEmpty(ticketJson))
                {
                    return RedirectToAction("IssueTicket");
                }

                var ticket = JsonSerializer.Deserialize<Ticket?>(ticketJson);
                if (ticket == null)
                {
                    TempData["PendingTicket"] = ticketJson;
                    TempData["ErrorMessage"] = "Invalid ticket data.";
                    return RedirectToAction("ConfirmTicket");
                }

                if (string.IsNullOrWhiteSpace(ticket.EvidenceImageTempPath) || string.IsNullOrWhiteSpace(ticket.EvidenceImageContentType))
                {
                    TempData["PendingTicket"] = ticketJson;
                    TempData["ErrorMessage"] = "Ticket evidence image is missing.";
                    return RedirectToAction("ConfirmTicket");
                }

                var evidencePhysicalPath = GetPhysicalPathFromWebPath(ticket.EvidenceImageTempPath);
                if (!System.IO.File.Exists(evidencePhysicalPath))
                {
                    TempData["PendingTicket"] = ticketJson;
                    TempData["ErrorMessage"] = "Uploaded evidence image could not be found.";
                    return RedirectToAction("ConfirmTicket");
                }

                ticket.EvidenceImageData = System.IO.File.ReadAllBytes(evidencePhysicalPath);
                System.IO.File.Delete(evidencePhysicalPath);
                ticket.EvidenceImageTempPath = null;

                // ✅ Check for at least one violation
                if (string.IsNullOrWhiteSpace(ticket.Violations))
                {
                    TempData["PendingTicket"] = ticketJson; // keep data
                    TempData["ErrorMessage"] = "يجب اختيار مخالفة واحدة على الأقل قبل إصدار التذكرة.";
                    return RedirectToAction("ConfirmTicket");
                }

                // ✅ Check if email exists
                if (!string.IsNullOrEmpty(ticket.Email))
                {
                    var userExists = _db.Users.Any(u => u.Email == ticket.Email);
                    if (!userExists)
                    {
                        TempData["PendingTicket"] = ticketJson;
                        TempData["ErrorMessage"] = "This account does not exist. Please check the email address.";
                        return RedirectToAction("ConfirmTicket");
                    }
                }

                // ✅ Save to DB (log any error)
                try
                {
                    _db.Tickets.Add(ticket);
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving ticket for {Email}", ticket?.Email);
                    TempData["PendingTicket"] = ticketJson; // keep data
                    TempData["ErrorMessage"] = "An error occurred while saving the ticket. Please try again later.";
                    return RedirectToAction("ConfirmTicket");
                }

                TempData["SuccessMessage"] = "Ticket issued successfully!";
                return RedirectToAction("TicketIssued", new { id = ticket.Ticket_Id });
            }
            else
            {
                return RedirectToAction("IssueTicket");
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTicket(int ticket_Id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticket_Id);
            if (ticket != null)
            {
                try
                {
                    _db.Tickets.Remove(ticket);
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed deleting ticket {TicketId}", ticket_Id);
                    TempData["Message"] = "Unable to delete ticket at this time.";
                }
            }

            return RedirectToAction("Process"); // Replace with your actual view name
        }


        // Success page after ticket is issued
        public IActionResult TicketIssued(int id)
        {
            var ticket = _db.Tickets.Find(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
            return View(ticket);
        }


        [HttpGet]
        public IActionResult Receipt(int id)
        {
            var ticket = _db.Tickets
                .Include(t => t.Users)
                .FirstOrDefault(t => t.Ticket_Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }


        // to her

        [HttpGet]
        public IActionResult SearchTickets(string plateNumber)
        {
            // Guard empty input: return empty results instead of throwing or returning everything
            if (string.IsNullOrWhiteSpace(plateNumber))
            {
                ViewBag.SearchQuery = plateNumber;
                return View("TicketResults", new List<Ticket>());
            }

            // Search case-insensitive and trim whitespace
            var normalizedPlate = plateNumber.Trim().ToUpper();

            var tickets = _db.Tickets
                .Include(t => t.Users) // Load related user data
                .Where(t => t.Plate_Number != null && t.Plate_Number.ToUpper() == normalizedPlate)
                .OrderByDescending(t => t.Ticket_Time)
                .ToList();

            ViewBag.SearchQuery = plateNumber;
            return View("TicketResults", tickets);
        }
        ////////// ************************************under development i am tinking about removing it 90% ********************
        ///
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateNotes(int ticketId, string notes)
        {
            var ticket = _db.Tickets.Find(ticketId);
            if (ticket == null) return NotFound();

            ticket.Notes = notes;
            _db.SaveChanges();

            TempData["Message"] = "Notes updated successfully";
            return RedirectToAction("DetailedTicket", new { id = ticketId });
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


        // editing section
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTicket()
        {
            // Keep TempData alive for another request
            TempData.Keep("PendingTicket");

            var json = TempData["PendingTicket"] as string;
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("IssueTicket"); // fallback if data is missing
            }

            var ticket = JsonSerializer.Deserialize<Ticket?>(json);
            if (ticket == null) return RedirectToAction("IssueTicket");

            ViewBag.Violations = _db.Violations
                .Where(v => v.IsActive)
                .OrderBy(v => v.Name)
                .ToList();

            return View("IssueTicket", ticket); // pass the ticket back to the form
        }

        private static bool IsAllowedImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[12];
            var bytesRead = stream.Read(header);
            return HasKnownImageSignature(header.Slice(0, bytesRead));
        }

        private static bool HasKnownImageSignature(ReadOnlySpan<byte> header)
        {
            return (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                || (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                || (header.Length >= 6 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38 && (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61)
                || (header.Length >= 2 && header[0] == 0x42 && header[1] == 0x4D)
                || (header.Length >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50);
        }

        private string SaveEvidenceImageToTempStorage(IFormFile file)
        {
            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "ticket-evidence-temp");
            Directory.CreateDirectory(uploadsRoot);

            var extension = GetSafeImageExtension(file.ContentType);
            var fileName = $"ticket_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(fs);
            }

            return $"/uploads/ticket-evidence-temp/{fileName}";
        }

        private static string GetSafeImageExtension(string? contentType)
        {
            return contentType?.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/webp" => ".webp",
                _ => ".img"
            };
        }

        private string GetPhysicalPathFromWebPath(string webPath)
        {
            var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_env.WebRootPath ?? "wwwroot", relative);
        }


    }

}

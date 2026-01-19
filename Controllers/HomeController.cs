using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TicketSystem.Data;
using TicketSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;




[AllowAnonymous]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<HomeController> _logger;

    public HomeController(AppDbContext db, IWebHostEnvironment env, ILogger<HomeController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewBag.AccessDeniedMessage = TempData["AccessDeniedMessage"] as string;
        return View();
    }
    public IActionResult Status()
    {
        return View();
    }
    public IActionResult Error()
    {
        return View();
    }
    public IActionResult Index()
    {
        var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir = Path.Combine(webRoot, "tutorials");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

        var files = Directory.GetFiles(dir)
            .Where(f => exts.Contains(Path.GetExtension(f)))
            .Select(f => "/tutorials/" + Path.GetFileName(f))
            .OrderBy(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var match = Regex.Match(name, @"\d+");
                if (match.Success && int.TryParse(match.Value, out var n))
                    return n;
                return int.MaxValue;
            })
            .ThenBy(f => Path.GetFileName(f))
            .ToList();

        ViewBag.ManualImages = files;
        return View();
    }
    public IActionResult ContactUs()
    {
        return View();
    }
    public IActionResult Services()
    {
        return View();
    }
    public IActionResult Help()
    {
        return View();
    }
    public IActionResult TechnicalSupport()
    {
        return View();
    }
    public IActionResult TrafficLaws()
    {
        return View();
    }

    //               *****on development****

    // POST: Home/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        // Check if user exists
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // Check if account is locked
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var minutesLeft = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            ModelState.AddModelError(string.Empty, $"Account locked. Try again in {minutesLeft} minute(s).");
            return View(model);
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5) // Lock after 5 failed attempts
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15); // Lock for 15 minutes
                user.FailedLoginAttempts = 0; // Reset counter after lock
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving failed login attempt for user {Email}", model.Email);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // Successful login → reset counters
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting lockout counters for user {Email}", user.Email);
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
        // User model uses Email as the key, use it as the NameIdentifier claim
        new Claim(ClaimTypes.NameIdentifier, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : (DateTimeOffset?)null
            });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return user.Role switch
        {
            "Officer" => RedirectToAction("Index", "Officer"),
            "Admin" => RedirectToAction("Index", "Admin"),
            "Office" => RedirectToAction("Index", "Office"),
            "User" => RedirectToAction("Index", "User", new { email = user.Email }),
            _ => RedirectToAction("Index", "Home")
        };
    }

    // POST: Home/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // feed backs section actions
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFeedback(Feedback model)
    {
        if (model == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Message))
        {
            ModelState.AddModelError("", "الرجاء إدخال الاسم والرسالة.");
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Feedbacks.Add(model);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving feedback from {Name}", model.Name);
            ModelState.AddModelError("", "حدث خطأ أثناء إرسال الرسالة. حاول مرة أخرى لاحقًا.");
            return View(model);
        }

        TempData["SuccessMessage"] = "تم إرسال رسالتك بنجاح!";
        return RedirectToAction("Index");
    }

    // manual book
    public IActionResult Manual()
    {
        var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir = Path.Combine(webRoot, "tutorials");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

        var files = Directory.GetFiles(dir)
            .Where(f => exts.Contains(Path.GetExtension(f)))
            .Select(f => new
            {
                Path = "/tutorials/" + Path.GetFileName(f),
                Name = Path.GetFileNameWithoutExtension(f)
            })
            .OrderBy(x =>
            {
                var m = Regex.Match(x.Name, @"\d+");
                if (m.Success && int.TryParse(m.Value, out var n))
                    return n;
                return int.MaxValue;
            })
            .ThenBy(x => x.Name)
            .Select(x => x.Path)
            .ToList();

        return View(files);
    }

}
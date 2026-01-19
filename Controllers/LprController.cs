using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TicketSystem.Services;

namespace TicketSystem.Controllers;

public class LprController : Controller
{
    private readonly IPlateRecognizerClient _client;

    public LprController(IPlateRecognizerClient client)
    {
        _client = client;
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile file, string? regions)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please upload an image.");
            return View();
        }

        using var stream = file.OpenReadStream();
        var result = await _client.RecognizeAsync(stream, regions);
        return View("Result", result);
    }

    public IActionResult Index() => View();
}
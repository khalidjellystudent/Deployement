using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> RecognizeFrame([FromForm] IFormFile file, string? regions)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Empty frame." });

        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only image uploads are allowed." });

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _client.RecognizeAsync(stream, regions);

            var bestMatch = result?.Results?
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.Candidates?.Max(c => c.Score) ?? 0)
                .FirstOrDefault();

            var plate = bestMatch?.Plate;
            var confidencePercentage = bestMatch is null
                ? (double?)null
                : Math.Round(bestMatch.Score * 100, 2);

            return Json(new { plate, confidencePercentage, result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "LPR processing failed.", detail = ex.Message });
        }
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

        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Only image uploads are allowed.");
            return View();
        }

        using var stream = file.OpenReadStream();
        var result = await _client.RecognizeAsync(stream, regions);
        return View("Result", result);
    }

    public IActionResult Index() => View();
}
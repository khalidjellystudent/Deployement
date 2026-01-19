using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Threading.Tasks;

namespace TicketSystem.Services;



public interface IPlateRecognizerClient
{
    Task<PlateResult?> RecognizeAsync(Stream imageStream, string? regions = null, int? cameraId = null);
}
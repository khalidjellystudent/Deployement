using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace TicketSystem.Services;

public class PlateRecognizerClient : IPlateRecognizerClient
{
    private const string PlaceholderToken = "<956a7c46a0ca0a929fb2a25c01dda6450d25e916>";

    private readonly HttpClient _http;
    private readonly PlateRecognizerOptions _opts;
    private readonly ILogger<PlateRecognizerClient> _logger;

    public PlateRecognizerClient(HttpClient http, IOptions<PlateRecognizerOptions> opts, ILogger<PlateRecognizerClient> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;

        var token = _opts.ApiToken?.Trim() ?? string.Empty;

        _logger.LogInformation("PlateRecognizer ApiToken length: {Length}", token.Length);

        if (string.IsNullOrWhiteSpace(token) || string.Equals(token, PlaceholderToken, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Plate Recognizer ApiToken is missing or still set to placeholder. " +
                "Set PlateRecognizer:ApiToken using User Secrets or an environment variable before using LPR.");
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", token);
        }
    }

    public async Task<PlateResult?> RecognizeAsync(Stream imageStream, string? regions = null, int? cameraId = null)
    {
        using var form = new MultipartFormDataContent();
        var imgContent = new StreamContent(imageStream);
        imgContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(imgContent, "upload", "frame.jpg");

        try
        {
            _logger.LogDebug("Sending LPR request to {Endpoint} (regions={Regions}, cameraId={CameraId})", _opts.Endpoint, regions, cameraId);
            var resp = await _http.PostAsync(_opts.Endpoint, form);
            var body = await resp.Content.ReadAsStringAsync();

            _logger.LogDebug("LPR response {StatusCode}: {Body}", resp.StatusCode, body);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("LPR API error {StatusCode}: {Body}", resp.StatusCode, body);
                throw new InvalidOperationException($"LPR API {resp.StatusCode}: {body}");
            }

            return System.Text.Json.JsonSerializer.Deserialize<PlateResult>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call LPR service");
            throw;
        }
    }
}
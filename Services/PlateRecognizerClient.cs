using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace TicketSystem.Services;

public class PlateRecognizerClient : IPlateRecognizerClient
{
    private readonly HttpClient _http;
    private readonly PlateRecognizerOptions _opts;

    public PlateRecognizerClient(HttpClient http, IOptions<PlateRecognizerOptions> opts)
    {
        _http = http;
        _opts = opts.Value;

        if (string.IsNullOrWhiteSpace(_opts.ApiToken))
            throw new InvalidOperationException("Plate Recognizer ApiToken is missing. Set via user-secrets or appsettings.");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", _opts.ApiToken);
    }

    public async Task<PlateResult?> RecognizeAsync(Stream imageStream, string? regions = null, int? cameraId = null)
    {
        using var form = new MultipartFormDataContent();
        var imgContent = new StreamContent(imageStream);
        imgContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(imgContent, "upload", "frame.jpg");

        var resp = await _http.PostAsync(_opts.Endpoint, form);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"LPR API {resp.StatusCode}: {body}");

        return System.Text.Json.JsonSerializer.Deserialize<PlateResult>(
            body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
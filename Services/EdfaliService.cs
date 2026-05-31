using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TicketSystem.Services;

public class EdfaliService : IEdfaliService
{
    private readonly IHttpClientFactory _httpFactory;

    public EdfaliService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<(bool Success, string Payload, string? Error)> CreateSessionAsync(string gatewayToken, string gatewayBaseUrl, string openSessionPath, object requestBody)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        try
        {
            var response = await client.PostAsync(BuildApiUrl(gatewayBaseUrl, openSessionPath), content);
            var payload = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, payload, response.IsSuccessStatusCode ? null : payload);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Success, string Payload, string? Error)> VerifyOtpAndGetSessionAsync(string gatewayToken, string gatewayBaseUrl, string verifyPath, string sessionPathTemplate, int sessionId, string otp)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var verifyBody = new { session_id = sessionId, otp };
            var verifyContent = new StringContent(JsonSerializer.Serialize(verifyBody), Encoding.UTF8, "application/json");
            var verifyResponse = await client.PostAsync(BuildApiUrl(gatewayBaseUrl, verifyPath), verifyContent);
            var verifyPayload = await verifyResponse.Content.ReadAsStringAsync();

            if (!verifyResponse.IsSuccessStatusCode)
            {
                return (false, verifyPayload, verifyPayload);
            }

            // After verify, always fetch session state
            return await GetSessionAsync(gatewayToken, gatewayBaseUrl, sessionPathTemplate, sessionId);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Success, string Payload, string? Error)> GetSessionAsync(string gatewayToken, string gatewayBaseUrl, string sessionPathTemplate, int sessionId)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await client.GetAsync(BuildSessionLookupUrl(gatewayBaseUrl, sessionPathTemplate, sessionId));
            var payload = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, payload, response.IsSuccessStatusCode ? null : payload);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    private static string BuildApiUrl(string baseUrl, string path)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        if (!normalizedPath.StartsWith("/"))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return $"{baseUrl}{normalizedPath}";
    }

    private static string BuildSessionLookupUrl(string baseUrl, string pathTemplate, int sessionId)
    {
        var normalizedTemplate = string.IsNullOrWhiteSpace(pathTemplate)
            ? "/payment/sessions/{sessionId}"
            : pathTemplate.Trim();
        var resolvedPath = normalizedTemplate.Replace("{sessionId}", sessionId.ToString(), StringComparison.OrdinalIgnoreCase);
        return BuildApiUrl(baseUrl, resolvedPath);
    }
}

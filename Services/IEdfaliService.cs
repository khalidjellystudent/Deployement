using System.Threading.Tasks;

namespace TicketSystem.Services;

public interface IEdfaliService
{
    Task<(bool Success, string Payload, string? Error)> CreateSessionAsync(string gatewayToken, string gatewayBaseUrl, string openSessionPath, object requestBody);

    Task<(bool Success, string Payload, string? Error)> VerifyOtpAndGetSessionAsync(string gatewayToken, string gatewayBaseUrl, string verifyPath, string sessionPathTemplate, int sessionId, string otp);

    Task<(bool Success, string Payload, string? Error)> GetSessionAsync(string gatewayToken, string gatewayBaseUrl, string sessionPathTemplate, int sessionId);
}

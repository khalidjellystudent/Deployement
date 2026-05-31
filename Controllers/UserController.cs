using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Linq;
using TicketSystem.Data;
using TicketSystem.Models;
using TicketSystem.Services;



namespace TicketSystem.Controllers
{ 

    [Authorize(Roles = "User")]
    public class UserController : Controller
        {
            private readonly AppDbContext _db;
            private readonly IConfiguration _configuration;
            private readonly IHttpClientFactory _httpClientFactory;
            private readonly IEdfaliService _edfaliService;
            public UserController(AppDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory, IEdfaliService edfaliService)
            {
                _db = db;
                _configuration = configuration;
                _httpClientFactory = httpClientFactory;
                _edfaliService = edfaliService;
            }

        public IActionResult Index()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var tickets = _db.Tickets
                .Include(t => t.Users) // Include user data if needed
                .Where(t => t.Email == userEmail)
                .OrderByDescending(t => t.Ticket_Time)
                .ToList();

            return View(tickets);
        }

        public IActionResult DetailedTicket(int id)
            {
                var ticket = _db.Tickets
                    .Include(t => t.Users)
                    .FirstOrDefault(t => t.Ticket_Id == id);

                if (ticket == null)
                {
                    return TicketAccessDenied("No ticket was found with this id.");
                }

                if (!string.Equals(ticket.Email?.Trim(), User.FindFirstValue(ClaimTypes.Email)?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return TicketAccessDenied("You can't access another user's ticket.");
                }

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
                return TicketAccessDenied("No ticket was found with this id.");
            }

            if (!string.Equals(ticket.Email?.Trim(), User.FindFirstValue(ClaimTypes.Email)?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return TicketAccessDenied("You can't access another user's ticket.");
            }

            return View(ticket);
        }


            //   ****************Paying methods**********
            // In your TicketController.cs (or similar)
        public IActionResult PayTicket(int id)
        {
             var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == id);
              if (ticket == null)
              {
                  return TicketAccessDenied("No ticket was found with this id.");
              }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (!string.Equals(ticket.Email?.Trim(), userEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return TicketAccessDenied("You can't access another user's ticket.");
            }

            var pendingProvider = TempData["PendingProvider"]?.ToString();
            ViewBag.PendingSessionId = TempData["PendingSessionId"];
            ViewBag.PendingPaymentUrl = TempData["PendingPaymentUrl"];
            ViewBag.PaymentProvider = GetPaymentProvider(pendingProvider);

           return View(ticket); // Pass the ticket to the payment page
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(int ticketId, string? paymentProvider = null)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);

            if (ticket == null)
            {
                return TicketAccessDenied("No ticket was found with this id.");
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return TicketAccessDenied("Your account information is missing. Please log in again.");
            }

            if (!string.Equals(ticket.Email?.Trim(), userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return TicketAccessDenied("You can't access another user's ticket.");
            }

            if (string.Equals(ticket.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("DetailedTicket", new { id = ticket.Ticket_Id });
            }

            var provider = GetPaymentProvider(paymentProvider);
            var gatewayName = GetGatewayName(provider);
            var configSection = GetGatewayConfigSection(provider);

            var gatewayToken = ResolveGatewaySetting(provider, "ApiToken");
            var gatewayBaseUrl = (ResolveGatewaySetting(provider, "BaseUrl") ?? GetDefaultBaseUrl(provider)).TrimEnd('/');
            var payMethod = (ResolveGatewaySetting(provider, "PayMethod") ?? "moamalat").Trim().ToLowerInvariant();
            var openSessionPath = ResolveGatewaySetting(provider, "OpenSessionPath") ?? "/payment/sessions/open";
            var customerMobile = Request.Form["customerMobile"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(gatewayToken))
            {
                TempData["PaymentError"] = "Payment gateway is not configured. Set DPay:ApiToken.";
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            if (payMethod == "edfali" && string.IsNullOrWhiteSpace(customerMobile))
            {
                TempData["PaymentError"] = "EDFali requires a customer mobile number.";
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            if (payMethod == "edfali" && !Regex.IsMatch(customerMobile, "^09\\d{8}$"))
            {
                TempData["PaymentError"] = "Enter a valid EDFali mobile number (example: 0912345678).";
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            var amount = Math.Max(1, (int)Math.Round(ticket.FineAmount, MidpointRounding.AwayFromZero));

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new
            {
                pay_method = payMethod,
                amount,
                customer_mobile = payMethod == "edfali" ? customerMobile : null,
                data = new
                {
                    ticketId = ticket.Ticket_Id,
                    userEmail,
                    source = "traffic-ticket-system"
                }
            };

            try
            {
                string payload;

                if (payMethod == "edfali")
                {
                    var (Success, Payload, Error) = await _edfaliService.CreateSessionAsync(gatewayToken, gatewayBaseUrl, openSessionPath, requestBody);
                    if (!Success)
                    {
                        TempData["PaymentError"] = $"{gatewayName} error: {Error ?? "unknown"}";
                        return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                    }

                    payload = Payload;
                }
                else
                {
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(BuildApiUrl(gatewayBaseUrl, openSessionPath), content);
                    payload = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        TempData["PaymentError"] = $"{gatewayName} error ({(int)response.StatusCode}): {payload}";
                        return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                    }
                }

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var sessionId = ExtractSessionId(root);

                if (sessionId <= 0)
                {
                    TempData["PaymentError"] = $"{gatewayName} did not return a valid session id.";
                    return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                }

                TempData["PendingProvider"] = provider;
                TempData["PendingSessionId"] = sessionId.ToString();

                var paymentLink = ExtractPaymentLink(root);

                if (!string.IsNullOrWhiteSpace(paymentLink))
                {
                    var redirectUrl = BuildGatewayRedirectUrl(gatewayBaseUrl, paymentLink);
                    TempData["PendingPaymentUrl"] = redirectUrl;
                    TempData["PaymentSuccess"] = $"{gatewayName} session created. Complete payment, then click Verify Payment below.";
                }
                else
                {
                    TempData["PendingPaymentUrl"] = null;
                    TempData["PaymentSuccess"] = $"{gatewayName} session created. Enter OTP and click Verify Payment.";
                }

                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }
            catch (Exception ex)
            {
                TempData["PaymentError"] = $"Payment gateway error: {ex.Message}";
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentReturn(int ticketId, int? session_id = null)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);

            if (ticket == null)
            {
                return TicketAccessDenied("No ticket was found with this id.");
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return TicketAccessDenied("Your account information is missing. Please log in again.");
            }

            if (!string.Equals(ticket.Email?.Trim(), userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return TicketAccessDenied("You can't access another user's ticket.");
            }

            var provider = GetPaymentProvider();
            var configSection = GetGatewayConfigSection(provider);
            var gatewayToken = ResolveGatewaySetting(provider, "ApiToken");
            var gatewayBaseUrl = (ResolveGatewaySetting(provider, "BaseUrl") ?? GetDefaultBaseUrl(provider)).TrimEnd('/');
            var sessionPathTemplate = ResolveGatewaySetting(provider, "SessionPathTemplate") ?? "/payment/sessions/{sessionId}";

            if (string.IsNullOrWhiteSpace(gatewayToken) || !session_id.HasValue)
            {
                TempData["PaymentError"] = "Unable to verify payment session.";
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await client.GetAsync(BuildSessionLookupUrl(gatewayBaseUrl, sessionPathTemplate, session_id.Value));
                var payload = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["PaymentError"] = $"Payment verification failed ({(int)response.StatusCode}).";
                    return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                }

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var status = ExtractDPayStatus(root);
                var expiredAtText = ExtractDPayExpiredAt(root);
                var isExpired = IsDPaySessionExpired(expiredAtText);

                if (!IsPaidStatus(status))
                {
                    TempData["PaymentError"] = isExpired
                        ? "Payment session expired. Please click Pay Now to create a new session."
                        : $"Payment is not completed yet. Current status: {status ?? "unknown"}.";
                    return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                }
            }
            catch (Exception ex)
            {
                TempData["PaymentError"] = $"Payment verification failed: {ex.Message}";
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            ticket.Status = "Paid";
            _db.SaveChanges();

            TempData["PaymentSuccess"] = "Payment successful!";
            return RedirectToAction("DetailedTicket", new { id = ticket.Ticket_Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyDPaySession(int ticketId, int sessionId, string? paymentProvider = null)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);

            if (ticket == null)
            {
                return TicketAccessDenied("No ticket was found with this id.");
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return TicketAccessDenied("Your account information is missing. Please log in again.");
            }

            if (!string.Equals(ticket.Email?.Trim(), userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return TicketAccessDenied("You can't access another user's ticket.");
            }

            var provider = GetPaymentProvider(paymentProvider);
            var gatewayName = GetGatewayName(provider);
            var configSection = GetGatewayConfigSection(provider);

            var gatewayToken = ResolveGatewaySetting(provider, "ApiToken");
            var gatewayBaseUrl = (ResolveGatewaySetting(provider, "BaseUrl") ?? GetDefaultBaseUrl(provider)).TrimEnd('/');
            var payMethod = (ResolveGatewaySetting(provider, "PayMethod") ?? "moamalat").Trim().ToLowerInvariant();
            var verifyPath = ResolveGatewaySetting(provider, "VerifySessionPath") ?? "/payment/sessions/verify";
            var sessionPathTemplate = ResolveGatewaySetting(provider, "SessionPathTemplate") ?? "/payment/sessions/{sessionId}";
            var strictSessionValidation = bool.TryParse(ResolveGatewaySetting(provider, "StrictSessionValidation"), out var strictValidationFlag) && strictValidationFlag;
            var allowedFeeBuffer = int.TryParse(ResolveGatewaySetting(provider, "AllowedFeeBuffer"), out var feeBuffer) ? Math.Max(0, feeBuffer) : 0;

            if (string.IsNullOrWhiteSpace(gatewayToken))
            {
                TempData["PaymentError"] = "Unable to verify payment session: DPay:ApiToken is missing.";
                TempData["PendingProvider"] = provider;
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            var otp = Request.Form["otp"].ToString().Trim();
            if (payMethod == "edfali" && string.IsNullOrWhiteSpace(otp))
            {
                TempData["PaymentError"] = "OTP is required for EDFali verification.";
                TempData["PendingProvider"] = provider;
                TempData["PendingSessionId"] = sessionId.ToString();
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            if (payMethod == "edfali" && !Regex.IsMatch(otp, "^\\d{6}$"))
            {
                TempData["PaymentError"] = "OTP must be 6 digits.";
                TempData["PendingProvider"] = provider;
                TempData["PendingSessionId"] = sessionId.ToString();
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }

            try
            {
                string payload;

                if (payMethod == "edfali")
                {
                    var (Success, Payload, Error) = await _edfaliService.VerifyOtpAndGetSessionAsync(gatewayToken, gatewayBaseUrl, verifyPath, sessionPathTemplate, sessionId, otp);
                    if (!Success)
                    {
                        TempData["PaymentError"] = $"Payment verification failed: {Error ?? "unknown"}";
                        TempData["PendingProvider"] = provider;
                        TempData["PendingSessionId"] = sessionId.ToString();
                        return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                    }

                    payload = Payload;
                }
                else
                {
                    using var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(BuildSessionLookupUrl(gatewayBaseUrl, sessionPathTemplate, sessionId));
                    payload = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        TempData["PaymentError"] = $"Payment verification failed ({(int)response.StatusCode}).";
                        TempData["PendingProvider"] = provider;
                        TempData["PendingSessionId"] = sessionId.ToString();
                        return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                    }
                }

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var status = ExtractDPayStatus(root);
                var expiredAtText = ExtractDPayExpiredAt(root);
                var isExpired = IsDPaySessionExpired(expiredAtText);

                if (!IsPaidStatus(status))
                {
                    TempData["PaymentError"] = isExpired
                        ? "This payment session expired. Click Pay Now to create a new one."
                        : $"Current payment status: {status ?? "pending"}. Complete payment then verify again.";
                    TempData["PendingProvider"] = provider;
                    TempData["PendingSessionId"] = sessionId.ToString();
                    return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                }

                if (!ValidateDPaySessionForTicket(root, ticket.Ticket_Id, ticket.FineAmount, payMethod, strictSessionValidation, allowedFeeBuffer, out var validationError))
                {
                    TempData["PaymentError"] = validationError;
                    TempData["PendingProvider"] = provider;
                    TempData["PendingSessionId"] = sessionId.ToString();
                    return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
                }

                ticket.Status = "Paid";
                _db.SaveChanges();

                TempData["PaymentSuccess"] = "Payment successful!";
                return RedirectToAction("DetailedTicket", new { id = ticket.Ticket_Id });
            }
            catch (Exception ex)
            {
                TempData["PaymentError"] = $"Payment verification failed: {ex.Message}";
                TempData["PendingProvider"] = provider;
                TempData["PendingSessionId"] = sessionId.ToString();
                return RedirectToAction("PayTicket", new { id = ticket.Ticket_Id });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DPayWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest();
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var eventName = root.TryGetProperty("event", out var eventElement)
                    ? eventElement.GetString()
                    : string.Empty;
                var status = root.TryGetProperty("status", out var statusElement)
                    ? statusElement.GetString()
                    : string.Empty;

                if (!string.Equals(eventName, "payment.paid", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok();
                }

                if (!root.TryGetProperty("data", out var dataElement) ||
                    !dataElement.TryGetProperty("ticketId", out var ticketIdElement) ||
                    !ticketIdElement.TryGetInt32(out var ticketId))
                {
                    return Ok();
                }

                var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);
                if (ticket == null)
                {
                    return Ok();
                }

                ticket.Status = "Paid";
                _db.SaveChanges();

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }

        private string GetPaymentProvider(string? requestedProvider = null)
        {
            return "dpay";
        }

        private static string GetGatewayName(string provider)
        {
            return "DPay";
        }

        private static string GetGatewayConfigSection(string provider)
        {
            return "DPay";
        }

        private string? ResolveGatewaySetting(string provider, string key)
        {
            return _configuration[$"DPay:{key}"];
        }

        private static string GetDefaultBaseUrl(string provider)
        {
            return "https://dpay.ly/api/sandbox";
        }

        private static string BuildGatewayRedirectUrl(string gatewayBaseUrl, string paymentLink)
        {
            if (paymentLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return paymentLink;
            }

            var apiBaseUri = new Uri(gatewayBaseUrl, UriKind.Absolute);
            var origin = $"{apiBaseUri.Scheme}://{apiBaseUri.Host}";
            return $"{origin}{paymentLink}";
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

        private static int ExtractSessionId(JsonElement root)
        {
            if (TryReadInt(root, "session_id", out var sessionId) ||
                TryReadInt(root, "sessionId", out sessionId) ||
                TryReadInt(root, "id", out sessionId))
            {
                return sessionId;
            }

            foreach (var propName in new[] { "session", "payment", "data", "result" })
            {
                if (root.TryGetProperty(propName, out var child) &&
                    child.ValueKind == JsonValueKind.Object &&
                    (TryReadInt(child, "session_id", out sessionId) ||
                     TryReadInt(child, "sessionId", out sessionId) ||
                     TryReadInt(child, "id", out sessionId)))
                {
                    return sessionId;
                }
            }

            return 0;
        }

        private static string ExtractPaymentLink(JsonElement root)
        {
            if (TryReadString(root, "payment_link", out var paymentLink) ||
                TryReadString(root, "payment_url", out paymentLink) ||
                TryReadString(root, "checkout_url", out paymentLink) ||
                TryReadString(root, "redirect_url", out paymentLink) ||
                TryReadString(root, "url", out paymentLink))
            {
                return paymentLink;
            }

            foreach (var propName in new[] { "session", "payment", "data", "result" })
            {
                if (root.TryGetProperty(propName, out var child) &&
                    child.ValueKind == JsonValueKind.Object &&
                    (TryReadString(child, "payment_link", out paymentLink) ||
                     TryReadString(child, "payment_url", out paymentLink) ||
                     TryReadString(child, "checkout_url", out paymentLink) ||
                     TryReadString(child, "redirect_url", out paymentLink) ||
                     TryReadString(child, "url", out paymentLink)))
                {
                    return paymentLink;
                }
            }

            return string.Empty;
        }

        private static string? ExtractDPayStatus(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryReadStatus(root, out var directStatus))
                {
                    return directStatus;
                }

                foreach (var propName in new[] { "session", "payment", "data" })
                {
                    if (root.TryGetProperty(propName, out var child) && TryReadStatus(child, out var childStatus))
                    {
                        return childStatus;
                    }
                }
            }

            return null;
        }

        private static string? ExtractDPayExpiredAt(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("expired_at", out var expiredAt) && expiredAt.ValueKind == JsonValueKind.String)
            {
                return expiredAt.GetString();
            }

            foreach (var propName in new[] { "session", "payment", "data" })
            {
                if (root.TryGetProperty(propName, out var child) && child.ValueKind == JsonValueKind.Object)
                {
                    if (child.TryGetProperty("expired_at", out var nestedExpiredAt) && nestedExpiredAt.ValueKind == JsonValueKind.String)
                    {
                        return nestedExpiredAt.GetString();
                    }
                }
            }

            return null;
        }

        private static bool IsDPaySessionExpired(string? expiredAt)
        {
            if (string.IsNullOrWhiteSpace(expiredAt))
            {
                return false;
            }

            return DateTimeOffset.TryParse(expiredAt, out var expiredAtValue) && expiredAtValue <= DateTimeOffset.UtcNow;
        }

        private static bool TryReadStatus(JsonElement element, out string? status)
        {
            status = null;
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!element.TryGetProperty("status", out var statusElement))
            {
                return false;
            }

            status = statusElement.ValueKind switch
            {
                JsonValueKind.String => statusElement.GetString(),
                _ => statusElement.ToString()
            };

            return !string.IsNullOrWhiteSpace(status);
        }

        private static bool IsPaidStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            var normalized = status.Trim().ToLowerInvariant();
            return new[] { "paid", "success", "completed" }.Contains(normalized);
        }

        private static bool ValidateDPaySessionForTicket(
            JsonElement root,
            int expectedTicketId,
            decimal expectedFineAmount,
            string expectedPayMethod,
            bool strictValidation,
            int allowedFeeBuffer,
            out string error)
        {
            error = string.Empty;
            var expectedAmount = Math.Max(1, (int)Math.Round(expectedFineAmount, MidpointRounding.AwayFromZero));

            var hasAmount = TryExtractSessionAmount(root, out var paidAmount);
            if (hasAmount)
            {
                var amountDelta = paidAmount - expectedAmount;
                if (amountDelta < 0)
                {
                    error = $"Payment amount mismatch. Expected at least {expectedAmount}, but gateway returned {paidAmount}.";
                    return false;
                }

                if (amountDelta > allowedFeeBuffer)
                {
                    error = $"Payment amount mismatch. Expected {expectedAmount} (+ up to {allowedFeeBuffer} fees), but gateway returned {paidAmount}.";
                    return false;
                }
            }

            var hasMethod = TryExtractSessionPayMethod(root, out var actualMethod);
            if (hasMethod && !string.Equals(actualMethod, expectedPayMethod, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Payment method mismatch. Expected {expectedPayMethod}, but gateway returned {actualMethod}.";
                return false;
            }

            var hasTicketId = TryExtractSessionTicketId(root, out var actualTicketId);
            if (hasTicketId && actualTicketId != expectedTicketId)
            {
                error = "The verified payment session does not belong to this ticket.";
                return false;
            }

            if (strictValidation && (!hasAmount || !hasMethod || !hasTicketId))
            {
                error = "Payment session details are incomplete. Please create a new session and try again.";
                return false;
            }

            return true;
        }

        private static bool TryExtractSessionAmount(JsonElement root, out int amount)
        {
            amount = 0;
            foreach (var container in GetDPayContainers(root))
            {
                if (TryReadInt(container, "amount", out amount))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractSessionPayMethod(JsonElement root, out string method)
        {
            method = string.Empty;
            foreach (var container in GetDPayContainers(root))
            {
                if (TryReadString(container, "pay_method", out method) ||
                    TryReadString(container, "payment_method", out method) ||
                    TryReadString(container, "method", out method))
                {
                    method = method.Trim().ToLowerInvariant();
                    return !string.IsNullOrWhiteSpace(method);
                }
            }

            return false;
        }

        private static bool TryExtractSessionTicketId(JsonElement root, out int ticketId)
        {
            ticketId = 0;

            foreach (var container in GetDPayContainers(root))
            {
                if (TryReadInt(container, "ticketId", out ticketId) ||
                    TryReadInt(container, "ticket_id", out ticketId))
                {
                    return true;
                }

                if (container.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Object &&
                    (TryReadInt(dataElement, "ticketId", out ticketId) ||
                     TryReadInt(dataElement, "ticket_id", out ticketId)))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<JsonElement> GetDPayContainers(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }

            yield return root;

            foreach (var propName in new[] { "session", "payment", "data", "result" })
            {
                if (root.TryGetProperty(propName, out var child) && child.ValueKind == JsonValueKind.Object)
                {
                    yield return child;
                }
            }
        }

        private static bool TryReadInt(JsonElement source, string propertyName, out int value)
        {
            value = 0;
            if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty(propertyName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.Number)
            {
                return prop.TryGetInt32(out value);
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                return int.TryParse(prop.GetString(), out value);
            }

            return false;
        }

        private static bool TryReadString(JsonElement source, string propertyName, out string value)
        {
            value = string.Empty;
            if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty(propertyName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = prop.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        private IActionResult TicketAccessDenied(string message)
        {
            TempData["AccessDeniedMessage"] = message;
            return RedirectToAction("AccessDenied", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AskRevoke_Ticket(int ticketId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticketId);

            if (ticket == null || string.IsNullOrWhiteSpace(userEmail) ||
                !string.Equals(ticket.Email?.Trim(), userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            ticket.Status = "UnderProcess";
            ticket.Appealed = true;
            _db.SaveChanges();
            return RedirectToAction("Index", new { message = "tickets under individuals process" });
        }





    }
}
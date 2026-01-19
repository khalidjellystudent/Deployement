namespace TicketSystem.Services;

public class PlateRecognizerOptions
{
    public string Endpoint { get; set; } = "https://api.platerecognizer.com/v1/plate-reader/";
    public string ApiToken { get; set; } = string.Empty;
}
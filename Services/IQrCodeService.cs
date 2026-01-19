using System;

namespace TicketSystem.Services
{
    public interface IQrCodeService
    {
        byte[] GeneratePngBytes(string text, int pixelsPerModule = 10);
        string GeneratePngDataUri(string text, int pixelsPerModule = 10);
    }
}

using System;
using QRCoder;

namespace TicketSystem.Services
{
    public class QrCodeService : IQrCodeService
    {
        public byte[] GeneratePngBytes(string text, int pixelsPerModule = 10)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("QR content cannot be null or empty.", nameof(text));

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule);
        }

        public string GeneratePngDataUri(string text, int pixelsPerModule = 10)
        {
            var bytes = GeneratePngBytes(text, pixelsPerModule);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
    }
}

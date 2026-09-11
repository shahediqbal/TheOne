using QRCoder;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Infrastructure.Otp;

/// <summary>Renders enrollment QR codes locally using QRCoder.</summary>
public sealed class AuthenticatorQrCodeGenerator : IAuthenticatorQrCodeGenerator
{
    /// <inheritdoc />
    public string Generate(string authenticatorUri)
    {
        using var data = QRCodeGenerator.GenerateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return "data:image/png;base64," + Convert.ToBase64String(png.GetGraphic(6));
    }
}
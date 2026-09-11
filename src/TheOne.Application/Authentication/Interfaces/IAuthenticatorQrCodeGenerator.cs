namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Renders authenticator enrollment QR codes locally without disclosing secrets externally.</summary>
public interface IAuthenticatorQrCodeGenerator
{
    /// <summary>Returns a PNG data URI for the supplied enrollment URI.</summary>
    string Generate(string authenticatorUri);
}
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Infrastructure.Otp;

/// <summary>Encrypts short codes using the host's Data Protection key ring.</summary>
public sealed class DataProtectionOtpCodeProtector(IDataProtectionProvider provider) : IOtpCodeProtector
{
    /// <inheritdoc />
    public string Protect(Guid challengeId, string code) =>
        Protector(challengeId).Protect(code);
    /// <inheritdoc />
    public bool Matches(Guid challengeId, string protectedCode, string code)
    {
        try
        {
            var expected = Protector(challengeId).Unprotect(protectedCode);
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(code));
        }
        catch (CryptographicException) { return false; }
    }
    private IDataProtector Protector(Guid id) => provider.CreateProtector("TheOne.Otp.v1", id.ToString("N"));
}
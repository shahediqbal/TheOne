namespace TheOne.Application.Authentication.Interfaces;

/// <summary>Protects short codes at rest and binds them to a challenge.</summary>
public interface IOtpCodeProtector
{
    /// <summary>Encrypts a code using application-managed Data Protection keys.</summary>
    string Protect(Guid challengeId, string code);
    /// <summary>Checks a candidate code without exposing decrypted data.</summary>
    bool Matches(Guid challengeId, string protectedCode, string code);
}
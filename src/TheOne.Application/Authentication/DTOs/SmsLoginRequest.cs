namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains SmsLogin input.</summary>
public sealed class SmsLoginRequest
{
    /// <summary>Gets or sets MobileNumber.</summary>
    public string MobileNumber { get; set; } = string.Empty;
}
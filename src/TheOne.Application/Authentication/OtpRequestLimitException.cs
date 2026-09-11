namespace TheOne.Application.Authentication;

/// <summary>Indicates the account has reached an OTP resend limit.</summary>
public sealed class OtpRequestLimitException() : Exception("Please wait before requesting another code.");

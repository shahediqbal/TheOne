namespace TheOne.Application.Authentication;

/// <summary>Indicates temporary SMS delivery unavailability without provider secrets.</summary>
public sealed class OtpDeliveryException() : Exception("SMS delivery is unavailable. Please try again later.");
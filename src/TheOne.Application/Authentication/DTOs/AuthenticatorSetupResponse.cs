namespace TheOne.Application.Authentication.DTOs;

/// <summary>Contains enrollment secrets; never cache or log this response.</summary>
public sealed record AuthenticatorSetupResponse(string SharedKey, string AuthenticatorUri, string QrCodeDataUri);
/// <summary>Contains single-use recovery codes shown only when generated.</summary>
public sealed record RecoveryCodesResponse(IReadOnlyCollection<string> RecoveryCodes);
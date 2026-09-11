namespace TheOne.Infrastructure.Sms;

/// <summary>Configures BulkSMSBD and an explicitly enabled local development outbox.</summary>
public sealed class SmsProviderOptions
{
    /// <summary>Gets or sets the HTTPS SMS submission endpoint.</summary>
    public string BaseUrl { get; set; } = "https://bulksmsbd.net/api/smsapi";
    /// <summary>Gets or sets the API key supplied from secret configuration.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider-approved sender.</summary>
    public string SenderId { get; set; } = "8809617613593";
    /// <summary>Gets or sets the provider message type.</summary>
    public string Type { get; set; } = "text";
    /// <summary>Gets or sets whether real SMS delivery is enabled.</summary>
    public bool Enabled { get; set; }
    /// <summary>Enables local file delivery only when the host environment is Development and real SMS is disabled.</summary>
    public bool DevelopmentOutboxEnabled { get; set; }
}
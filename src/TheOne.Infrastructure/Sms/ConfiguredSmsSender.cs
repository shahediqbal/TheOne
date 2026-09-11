using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Infrastructure.Sms;

/// <summary>Selects live delivery or an opt-in, local-only development outbox.</summary>
public sealed class ConfiguredSmsSender(BulkSmsBdSender live, IOptions<SmsProviderOptions> configured,
    IHostEnvironment environment) : ISmsSender
{
    /// <inheritdoc />
    public void EnsureAvailable()
    {
        if (configured.Value.Enabled) { live.EnsureAvailable(); return; }
        if (!environment.IsDevelopment() || !configured.Value.DevelopmentOutboxEnabled)
            throw new OtpDeliveryException();
    }

    /// <inheritdoc />
    public async Task SendAsync(string mobileNumber, string message, CancellationToken cancellationToken)
    {
        EnsureAvailable();
        if (configured.Value.Enabled)
        {
            await live.SendAsync(mobileNumber, message, cancellationToken);
            return;
        }
        // Never served by the API or placed in the repository; only the local development user reads it.
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheOne", "DevelopmentSmsOutbox");
        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".txt"),
                $"DEVELOPMENT ONLY — not sent\nTo: {mobileNumber}\n{message}\n", cancellationToken);
        }
        catch (IOException) { throw new OtpDeliveryException(); }
        catch (UnauthorizedAccessException) { throw new OtpDeliveryException(); }
    }
}
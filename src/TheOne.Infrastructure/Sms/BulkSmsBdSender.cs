using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TheOne.Application.Authentication;
using TheOne.Application.Authentication.Interfaces;

namespace TheOne.Infrastructure.Sms;

/// <summary>Submits one SMS through BulkSMSBD over TLS without automatic retries.</summary>
public sealed class BulkSmsBdSender(HttpClient client, IOptions<SmsProviderOptions> configured) : ISmsSender
{
    /// <inheritdoc />
    public void EnsureAvailable()
    {
        var options = configured.Value;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.SenderId) || options.Type != "text" ||
            !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new OtpDeliveryException();
    }

    /// <inheritdoc />
    public async Task SendAsync(string mobileNumber, string message, CancellationToken cancellationToken)
    {
        EnsureAvailable();
        var options = configured.Value;
        var number = mobileNumber.TrimStart('+');
        if (number.StartsWith("01", StringComparison.Ordinal)) number = "88" + number;
        if (!Regex.IsMatch(number, @"^8801[3-9][0-9]{8}$")) throw new OtpDeliveryException();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["api_key"] = options.ApiKey, ["senderid"] = options.SenderId,
            ["type"] = options.Type, ["number"] = number, ["message"] = message
        });
        try
        {
            using var response = await client.PostAsync(options.BaseUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new OtpDeliveryException();
            using var body = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            // HTTP 200 alone does not mean the provider accepted the SMS.
            if (body is null || body.RootElement.ValueKind != JsonValueKind.Object ||
                !body.RootElement.TryGetProperty("response_code", out var code) ||
                code.ToString() != "202") throw new OtpDeliveryException();
        }
        catch (HttpRequestException) { throw new OtpDeliveryException(); }
        catch (JsonException) { throw new OtpDeliveryException(); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new OtpDeliveryException(); }
    }
}
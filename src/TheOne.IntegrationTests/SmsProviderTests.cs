using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TheOne.Application.Authentication;
using TheOne.Infrastructure.Otp;
using TheOne.Infrastructure.Sms;
using Xunit;

namespace TheOne.IntegrationTests;

/// <summary>Checks the provider contract and fail-closed behavior without making network requests.</summary>
public sealed class SmsProviderTests
{
    [Theory]
    [InlineData("{\"response_code\":202}")]
    [InlineData("{\"response_code\":\"202\"}")]
    public async Task Posts_encoded_fields_and_accepts_explicit_success(string response)
    {
        var handler = new RecordingHandler(response);
        using var client = new HttpClient(handler);
        var sender = new BulkSmsBdSender(client, Options.Create(Settings()));
        await sender.SendAsync("01712345678", "Your The One OTP is 123456", default);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://bulksmsbd.net/api/smsapi", handler.Uri);
        Assert.Contains("api_key=test-key", handler.Body);
        Assert.Contains("number=8801712345678", handler.Body);
        Assert.Contains("senderid=8809617613593", handler.Body);
        Assert.Contains("message=Your+The+One+OTP+is+123456", handler.Body);
        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
    }

    [Theory]
    [InlineData("{\"response_code\":1007}")]
    [InlineData("{}")]
    [InlineData("not-json")]
    public async Task Provider_errors_and_malformed_responses_fail_closed(string response)
    {
        using var client = new HttpClient(new RecordingHandler(response));
        var sender = new BulkSmsBdSender(client, Options.Create(Settings()));
        var exception = await Assert.ThrowsAsync<OtpDeliveryException>(() =>
            sender.SendAsync("+8801712345678", "Your The One OTP is 123456", default));
        Assert.DoesNotContain("123456", exception.Message);
        Assert.DoesNotContain("test-key", exception.Message);
    }

    [Theory]
    [InlineData(false, "https://bulksmsbd.net/api/smsapi")]
    [InlineData(true, "http://bulksmsbd.net/api/smsapi")]
    [InlineData(true, "https://bulksmsbd.net/api/smsapi?api_key=secret")]
    public async Task Disabled_or_unsafe_configuration_never_sends(bool enabled, string url)
    {
        var handler = new RecordingHandler("{\"response_code\":202}");
        using var client = new HttpClient(handler);
        var settings = Settings();
        settings.Enabled = enabled;
        settings.BaseUrl = url;
        var sender = new BulkSmsBdSender(client, Options.Create(settings));
        await Assert.ThrowsAsync<OtpDeliveryException>(() =>
            sender.SendAsync("01712345678", "Your The One OTP is 123456", default));
        Assert.Null(handler.Method);
    }

    [Fact]
    public void Development_outbox_is_rejected_in_production()
    {
        using var client = new HttpClient(new RecordingHandler("{}"));
        var settings = Options.Create(new SmsProviderOptions { DevelopmentOutboxEnabled = true });
        var sender = new ConfiguredSmsSender(new BulkSmsBdSender(client, settings), settings, new ProductionEnvironment());
        Assert.Throws<OtpDeliveryException>(sender.EnsureAvailable);
    }

    [Fact]
    public void Codes_are_bound_to_challenge_and_tampering_is_rejected()
    {
        var protector = new DataProtectionOtpCodeProtector(new EphemeralDataProtectionProvider());
        var id = Guid.NewGuid();
        var encrypted = protector.Protect(id, "123456");
        Assert.True(protector.Matches(id, encrypted, "123456"));
        Assert.False(protector.Matches(Guid.NewGuid(), encrypted, "123456"));
        Assert.False(protector.Matches(id, encrypted, "123457"));
        Assert.False(protector.Matches(id, "tampered", "123456"));
    }

    private static SmsProviderOptions Settings() => new() { Enabled = true, ApiKey = "test-key" };
    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? ContentType { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri!.ToString();
            ContentType = request.Content!.Headers.ContentType!.MediaType;
            Body = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }
    }
    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "TheOne";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
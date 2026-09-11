using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Infrastructure.Sms;

namespace TheOne.Infrastructure.Otp;

/// <summary>Registers code protection and SMS delivery infrastructure.</summary>
public static class DependencyInjection
{
    /// <summary>Adds OTP dependencies without enabling real SMS delivery.</summary>
    public static IServiceCollection AddOtpInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection().SetApplicationName("TheOne");
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddOptions<SmsProviderOptions>().Bind(configuration.GetSection("SmsProvider"));
        services.AddSingleton<IOtpCodeProtector, DataProtectionOtpCodeProtector>();
        services.AddSingleton<IAuthenticatorQrCodeGenerator, AuthenticatorQrCodeGenerator>();
        services.AddHttpClient<BulkSmsBdSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.MaxResponseContentBufferSize = 64 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
          .RemoveAllLoggers();
        services.AddTransient<ISmsSender, ConfiguredSmsSender>();
        return services;
    }
}

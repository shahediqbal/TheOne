using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Abstractions.Translation;

namespace TheOne.Infrastructure.Translation;

/// <summary>Registers the tiered translation infrastructure: self-hosted LibreTranslate plus Google NMT.</summary>
public static class DependencyInjection
{
    /// <summary>Adds translation clients and the tier router.</summary>
    public static IServiceCollection AddTranslationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleTranslateOptions>().Bind(configuration.GetSection("GoogleTranslate"));
        services.AddHttpClient<LibreTranslateClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["LibreTranslate:Url"]
                ?? throw new InvalidOperationException("LibreTranslate:Url is not configured."));
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<GoogleTranslateClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<ITranslationService, TieredTranslationService>();
        return services;
    }
}

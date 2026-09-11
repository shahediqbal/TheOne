using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TheOne.Application.Abstractions.Authentication;

namespace TheOne.Infrastructure.Authentication;

/// <summary>Registers JWT validation and token-generation services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds JWT authentication with validated configuration.</summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "JWT issuer is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "JWT audience is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.SecretKey) && Encoding.UTF8.GetByteCount(x.SecretKey) >= 32,
                "JWT secret key must contain at least 32 bytes.")
            .Validate(x => x.AccessTokenExpirationMinutes is > 0 and <= 60,
                "JWT access-token lifetime must be between 1 and 60 minutes.")
            .Validate(x => x.RefreshTokenExpirationDays is > 0 and <= 90,
                "Refresh-token lifetime must be between 1 and 90 days.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        // Resolve validated options after all configuration providers have been registered.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, configured) =>
            {
                var jwt = configured.Value;
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                    ValidateAudience = true, ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        services.AddAuthorization();
        services.AddScoped<ITokenService, JwtTokenService>();
        return services;
    }
}
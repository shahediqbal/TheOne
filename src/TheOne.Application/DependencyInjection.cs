using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Application.Authentication.Services;
using TheOne.Application.Authentication.Validators;

namespace TheOne.Application;

/// <summary>Registers application use cases and validation.</summary>
public static class DependencyInjection
{
    /// <summary>Adds authentication services and validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAccountRecoveryService, AccountRecoveryService>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IAdministratorRoleService, AdministratorRoleService>();
        services.AddScoped<TheOne.Application.UserManagement.IUserManagementService, TheOne.Application.UserManagement.UserManagementService>();
        services.AddScoped<TheOne.Application.Administration.IAdministrationService, TheOne.Application.Administration.AdministrationService>();
        return services;
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Authentication.Interfaces;
using TheOne.Persistence.Authentication;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;

namespace TheOne.Persistence;

/// <summary>Registers database and Identity adapters.</summary>
public static class DependencyInjection
{
    /// <summary>Adds PostgreSQL persistence and Identity authentication stores.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TheOneDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddSignInManager()
        .AddEntityFrameworkStores<TheOneDbContext>()
        .AddUserStore<ProtectedIdentityUserStore>()
        .AddDefaultTokenProviders();
        services.AddScoped<IAuthenticationStore, IdentityAuthenticationStore>();
        services.AddScoped<IAccountRecoveryStore, IdentityAccountRecoveryStore>();
        services.AddScoped<IUserSessionStore, IdentityUserSessionStore>();
        services.AddScoped<SessionTokenIssuer>();
        services.AddScoped<IMfaStore, IdentityMfaStore>();
        services.AddScoped<IPrivilegedSessionValidator, PrivilegedSessionValidator>();
        services.AddScoped<IAdministratorRoleStore, AdministratorRoleStore>();
        services.AddScoped<TheOne.Application.UserManagement.IUserManagementStore, TheOne.Persistence.UserManagement.IdentityUserManagementStore>();
        services.AddScoped<TheOne.Application.Administration.IAdministrationStore, TheOne.Persistence.Administration.AdministrationStore>();
        services.AddScoped<TheOne.Application.Administration.IPermissionReader, TheOne.Persistence.Administration.PermissionReader>();
        services.AddScoped<TheOne.Application.Administration.ISecurityRequestAudit, TheOne.Persistence.Administration.SecurityRequestAudit>();
        return services;
    }
}

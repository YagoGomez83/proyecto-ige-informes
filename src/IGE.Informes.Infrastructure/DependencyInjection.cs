using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.FileStorage;
using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Infrastructure.PdfParsing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IGE.Informes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<AuditLogInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
            options.AddInterceptors(sp.GetRequiredService<AuditLogInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IPasswordHasher<ApplicationUser>, Argon2PasswordHasher>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        services.AddSingleton<IInformePdfParser, InformePdfParserAdapter>();

        return services;
    }
}

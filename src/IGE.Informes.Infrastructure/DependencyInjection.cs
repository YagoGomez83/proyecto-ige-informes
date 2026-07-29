using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.BuscarInformes;
using IGE.Informes.Infrastructure.Antivirus;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.FileStorage;
using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Infrastructure.PdfParsing;
using IGE.Informes.Infrastructure.Persistence;
using MediatR;
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
        services.AddScoped<IUserManagementService, UserManagementService>();

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.SectionName));
        services.AddSingleton<IAntivirusScanner, ClamAvAntivirusScanner>();

        services.AddSingleton<IInformePdfParser, InformePdfParserAdapter>();

        // Único Handler que vive en Infrastructure en vez de Application:
        // depende de EF.Functions.ToTsVector/PlainToTsQuery (Npgsql), que
        // Application no puede referenciar — ver el comentario en
        // BuscarInformesQueryHandler. MediatR no lo descubre por escaneo de
        // ensamblado (solo escanea Application), así que se registra
        // explícitamente acá.
        services.AddScoped<IRequestHandler<BuscarInformesQuery, IReadOnlyCollection<InformeBusquedaResultDto>>, BuscarInformesQueryHandler>();

        return services;
    }
}

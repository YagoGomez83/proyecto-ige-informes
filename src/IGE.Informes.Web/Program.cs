using System.Threading.RateLimiting;
using IGE.Informes.Application;
using IGE.Informes.Infrastructure;
using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Web.Components;
using IGE.Informes.Web.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// Necesario para que RemoteIpAddress refleje la IP real del cliente y no
// la del reverse proxy (Caddy) — sin esto, el rate limiter de abajo
// particiona por la IP interna de Caddy, compartiendo el límite entre
// todos los usuarios en vez de aislar por atacante (hallazgo del
// security-reviewer al cerrar la Fase 5). "web" nunca es accesible
// directamente desde fuera del compose (solo "reverse-proxy" publica
// puerto al host), así que confiar en cualquier proxy sin restringir
// ForwardedHeadersOptions.KnownNetworks/KnownProxies es aceptable acá —
// no hay forma de que un cliente externo alcance "web" sin pasar por
// Caddy primero.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Complementa el lockout de Identity (5 intentos fallidos por cuenta,
// ver DependencyInjection.cs): este limiter limita por IP, para frenar a
// un atacante que rota emails distintos en vez de insistir sobre uno
// solo — el lockout por cuenta no lo detiene, esto sí (docs/06-seguridad-
// amenazas.md, sección Autenticación). Se aplica como GlobalLimiter (no
// como política nombrada) porque Blazor Server no permite atar
// RequireRateLimiting a un componente Razor individual — ver dónde se
// activa el middleware en Program.cs (UseWhen acotado a /Account).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

// Debe ir antes que cualquier middleware que lea Connection.RemoteIpAddress
// o Request.Scheme (rate limiter, HTTPS redirect, HSTS, etc.).
app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IGE.Informes.Infrastructure.Persistence.AppDbContext>();
    dbContext.Database.Migrate();
}

await IdentitySeeder.SeedAsync(app.Services);
await TestDataSeeder.SeedAsync(app.Services, app.Environment);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Blazor Server no permite atar una política de rate limiting a un
// componente Razor individual (MapRazorComponents no expone
// RequireRateLimiting por ruta) — se acota el middleware a las rutas de
// Account vía UseWhen, para no limitar el resto de la app (ej. el
// circuito de SignalR, que se rompería con un límite tan bajo).
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/Account"),
    branch => branch.UseRateLimiter());

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapHealthChecks("/health");

app.Run();

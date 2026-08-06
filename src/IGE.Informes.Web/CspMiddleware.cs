using System.Security.Cryptography;
using System.Text;
using IGE.Informes.Infrastructure.FileStorage;
using Microsoft.Extensions.Options;

namespace IGE.Informes.Web;

/// <summary>
/// Arma el header Content-Security-Policy en la app en vez de en el
/// reverse proxy (Caddy) porque necesita el hash SHA-256 del ImportMap
/// —el import map de Blazor cambia entre builds (fingerprint de assets),
/// no entre requests, así que se calcula una sola vez al arrancar la
/// app— y los orígenes públicos de MinIO ya configurados en
/// <see cref="MinioOptions"/>, evitando duplicar esa IP/host en dos
/// archivos de configuración distintos.
/// </summary>
public sealed class CspMiddleware(RequestDelegate next, IOptions<MinioOptions> minioOptions, EndpointDataSource endpointDataSource)
{
    private readonly string _scriptSrc = ConstruirScriptSrc(endpointDataSource);

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            // Sobrescribe siempre, sin chequear si ya existe: Blazor Web
            // App agrega por su cuenta un Content-Security-Policy propio
            // con solo "frame-ancestors 'self'" en algunas respuestas
            // (páginas de error/not-found reejecutadas) — un guard tipo
            // "no pisar si ya existe" dejaría pasar esa política parcial
            // y perdería todas las demás directivas (script-src, img-src,
            // etc.), exponiendo esas rutas sin protección real.
            var minioOrigenes = ObtenerOrigenesMinio(minioOptions.Value);
            context.Response.Headers["Content-Security-Policy"] = ConstruirPolitica(minioOrigenes);

            return Task.CompletedTask;
        });

        await next(context);
    }

    private string ConstruirPolitica(string minioOrigenes) =>
        "base-uri 'self'; "
        + "default-src 'self'; "
        + $"img-src 'self' data: {minioOrigenes}; "
        + "object-src 'none'; "
        + $"{_scriptSrc}; "
        + "style-src 'self' 'unsafe-inline'; "
        + "connect-src 'self' wss:; "
        + $"frame-src 'self' {minioOrigenes}; "
        + "frame-ancestors 'none'; "
        + "upgrade-insecure-requests";

    private static string ConstruirScriptSrc(EndpointDataSource endpointDataSource)
    {
        // 'unsafe-hashes' + hash fijo: el atributo onclick inline que trae
        // el NavMenu por defecto de la plantilla Blazor (ver doc oficial,
        // "Enforce a Content Security Policy for ASP.NET Core Blazor").
        var scriptSrc = "script-src 'self' 'wasm-unsafe-eval' 'unsafe-hashes' "
            + "'sha256-qnHnQs7NjQNHHNYv/I9cW+I62HzDJjbnyS/OFzqlix0='";

        var importMapHash = CalcularHashImportMap(endpointDataSource);
        if (importMapHash is not null)
        {
            scriptSrc += $" '{importMapHash}'";
        }

        return scriptSrc;
    }

    /// <summary>
    /// Expuesto para que App.razor use exactamente el mismo hash en el
    /// atributo integrity="" del ImportMap — si difiriera del que va en
    /// el header CSP, el navegador bloquearía el script igual.
    ///
    /// Usa reflection para leer Microsoft.AspNetCore.Components.Endpoints.
    /// ImportMapDefinition porque el tipo es internal al ensamblado del
    /// framework — no se puede referenciar por nombre desde un .cs propio
    /// (a diferencia de App.razor, que sí puede: los componentes Razor se
    /// compilan con acceso a internals del framework).
    /// </summary>
    public static string? CalcularHashImportMap(EndpointDataSource endpointDataSource)
    {
        var metadataItem = endpointDataSource.Endpoints
            .SelectMany(e => e.Metadata)
            .FirstOrDefault(m => m.GetType().Name == "ImportMapDefinition");

        if (metadataItem is null)
        {
            return null;
        }

        var metadataBytes = Encoding.UTF8.GetBytes(metadataItem.ToString()!.ReplaceLineEndings("\n"));
        return $"sha256-{Convert.ToBase64String(SHA256.HashData(metadataBytes))}";
    }

    private static string ObtenerOrigenesMinio(MinioOptions options)
    {
        var host = options.EndpointPublico ?? options.Endpoint;
        return $"http://{host} https://{host}";
    }
}

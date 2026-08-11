namespace IGE.Informes.Infrastructure.FileStorage;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "ige-informes";
    public bool UseSsl { get; init; }
    public int UrlDescargaExpiracionSegundos { get; init; } = 300;

    /// <summary>
    /// Esquema (http/https) con el que se firman las URLs prefirmadas para
    /// <see cref="EndpointPublico"/> — independiente de <see cref="UseSsl"/>,
    /// que rige la conexión interna del backend a MinIO (siempre HTTP en la
    /// red de Docker). El navegador del cliente sí necesita HTTPS real acá:
    /// la CSP de la app tiene "upgrade-insecure-requests" (CspMiddleware.cs)
    /// y reescribe cualquier URL http:// de esta página a https:// antes de
    /// pedirla, así que sin TLS real en el puerto público el handshake
    /// falla (ver project_bug_pdfpath_migracion_2026-08-11 en memoria). Si
    /// no se configura, sigue el valor de <see cref="UseSsl"/> (entornos
    /// donde ambos coinciden, ej. sin Docker).
    /// </summary>
    public bool? UseSslPublico { get; init; }

    /// <summary>
    /// Host:puerto público (accesible desde el navegador del cliente) para
    /// generar URLs prefirmadas de descarga/vista previa. Distinto de
    /// <see cref="Endpoint"/>, que es el hostname interno de red que usa el
    /// propio backend para hablar con MinIO (ej. "minio:9000" en Docker
    /// Compose, no resoluble fuera de esa red) — sin este valor, el
    /// visor de PDF embebido en el navegador nunca podría cargar el
    /// archivo. Si no se configura, se usa Endpoint como fallback (entornos
    /// donde ambos coinciden, ej. sin Docker).
    /// </summary>
    public string? EndpointPublico { get; init; }
}

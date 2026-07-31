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

namespace IGE.Informes.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia el almacenamiento de archivos (MinIO/S3-compatible). El
/// bucket es privado por defecto — el acceso de lectura siempre pasa por
/// una URL prefirmada de corta expiración, nunca una URL pública
/// permanente (ver docs/06-seguridad-amenazas.md).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Sube un archivo y devuelve la clave (path) con la que se guardó —
    /// esa clave es lo que se persiste en Informe.PdfPath/Evidencia.ImagenPath,
    /// nunca una URL directa.
    /// </summary>
    Task<string> SubirAsync(string nombreArchivo, Stream contenido, string tipoMime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera una URL prefirmada de corta expiración para leer el archivo
    /// identificado por su clave.
    /// </summary>
    Task<string> ObtenerUrlDescargaAsync(string clave, CancellationToken cancellationToken = default);
}

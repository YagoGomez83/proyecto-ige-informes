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

    /// <summary>
    /// Descarga el contenido del archivo identificado por su clave — para
    /// procesamiento server-side (ej. re-parsear un PDF ya guardado), no
    /// para exponerlo al cliente (eso sigue siendo <see cref="ObtenerUrlDescargaAsync"/>).
    /// </summary>
    Task<byte[]> DescargarAsync(string clave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina el archivo identificado por su clave. Idempotente: no falla
    /// si la clave ya no existe.
    /// </summary>
    Task EliminarAsync(string clave, CancellationToken cancellationToken = default);
}

namespace IGE.Informes.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia el escaneo antivirus de archivos subidos por el usuario
/// (ClamAV, ver docs/06-seguridad-amenazas.md, sección "Ingesta de PDFs")
/// — Application no depende de la librería/protocolo concreto (ver
/// ADR-004 para el mismo criterio con el parser de PDF).
/// </summary>
public interface IAntivirusScanner
{
    /// <summary>
    /// Devuelve true si el contenido está limpio, false si se detectó una
    /// amenaza. No lanza excepción ante una detección — solo ante un
    /// problema de infraestructura (ClamAV caído/inalcanzable), que el
    /// caller decide cómo tratar (ver <see cref="AntivirusNoDisponibleException"/>).
    /// </summary>
    Task<bool> EstaLimpioAsync(byte[] contenido, CancellationToken cancellationToken = default);
}

/// <summary>
/// El servicio de ClamAV no respondió o no se pudo conectar — se trata
/// distinto de "se encontró una amenaza": fail-closed (rechazar la subida)
/// en vez de fail-open (aceptarla igual), porque un archivo sin escanear
/// nunca debería persistirse silenciosamente.
/// </summary>
public sealed class AntivirusNoDisponibleException(string message, Exception? innerException = null)
    : Exception(message, innerException);

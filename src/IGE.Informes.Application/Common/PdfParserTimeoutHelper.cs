using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.Application.Common;

/// <summary>
/// Envuelve <see cref="IInformePdfParser.Parsear"/> (síncrono, PdfPig, sin
/// CancellationToken en su firma por decisión de ADR-004) con un timeout
/// por archivo — mitiga el hallazgo del threat model
/// (docs/06-seguridad-amenazas.md: "Timeout ... en el proceso de
/// extracción") de que un PDF corrupto/malformado puede colgar el hilo sin
/// lanzar excepción nunca. Usado tanto por la carga individual (HU-01,
/// <c>ParsearPdfInformeQueryHandler</c>) como por la migración masiva
/// (HU-04, <c>MigrarInformesCommandHandler</c>).
///
/// Limitación conocida y aceptada: no evita que el hilo de fondo del
/// Task.Run siga vivo en el thread pool tras el timeout — PdfPig no coopera
/// con cancelación, así que esto corta el bloqueo percibido por el
/// usuario/circuito, no garantiza la liberación inmediata del hilo.
/// </summary>
public static class PdfParserTimeoutHelper
{
    public static readonly TimeSpan TimeoutPorDefecto = TimeSpan.FromSeconds(30);

    public static async Task<InformeExtraidoDto> ParsearConTimeoutAsync(
        this IInformePdfParser parser,
        byte[] contenido,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout ?? TimeoutPorDefecto);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var tarea = Task.Run(() =>
        {
            using var stream = new MemoryStream(contenido);
            return parser.Parsear(stream);
        });

        var completada = await Task.WhenAny(tarea, Task.Delay(Timeout.Infinite, linkedCts.Token));
        if (completada != tarea)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(
                $"El parseo del PDF superó el tiempo máximo permitido ({(timeout ?? TimeoutPorDefecto).TotalSeconds:0} s).");
        }

        return await tarea;
    }
}

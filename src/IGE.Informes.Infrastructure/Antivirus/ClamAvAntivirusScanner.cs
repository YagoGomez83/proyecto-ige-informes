using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using IGE.Informes.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace IGE.Informes.Infrastructure.Antivirus;

/// <summary>
/// Cliente del protocolo INSTREAM de clamd (ClamAV) sobre TCP — implementado
/// directo en vez de sumar una librería NuGet de terceros para un protocolo
/// simple (comando + chunks de tamaño+datos + chunk vacío final, respuesta
/// de texto). Ver
/// https://linux.die.net/man/8/clamd (sección INSTREAM) para el protocolo.
/// </summary>
public sealed class ClamAvAntivirusScanner(IOptions<ClamAvOptions> options) : IAntivirusScanner
{
    private const int TamanioMaximoChunk = 1024 * 1024; // 1 MB, límite razonable de INSTREAM

    public async Task<bool> EstaLimpioAsync(byte[] contenido, CancellationToken cancellationToken = default)
    {
        var opciones = options.Value;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(opciones.TimeoutSegundos));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var cliente = new TcpClient();
            await cliente.ConnectAsync(opciones.Host, opciones.Port, linkedCts.Token);

            using var stream = cliente.GetStream();

            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), linkedCts.Token);

            for (var offset = 0; offset < contenido.Length; offset += TamanioMaximoChunk)
            {
                var largoChunk = Math.Min(TamanioMaximoChunk, contenido.Length - offset);
                var encabezado = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(encabezado, (uint)largoChunk);

                await stream.WriteAsync(encabezado, linkedCts.Token);
                await stream.WriteAsync(contenido.AsMemory(offset, largoChunk), linkedCts.Token);
            }

            // Chunk final de tamaño 0 — le indica a clamd que terminó el stream.
            await stream.WriteAsync(new byte[4], linkedCts.Token);

            using var lector = new StreamReader(stream, Encoding.ASCII);
            var respuesta = await lector.ReadLineAsync(linkedCts.Token) ?? string.Empty;

            // Respuestas posibles: "stream: OK", "stream: <virus> FOUND",
            // "stream: <motivo> ERROR".
            return respuesta.Contains("OK", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            // Fail-closed: si ClamAV no responde, el caller decide rechazar
            // la subida en vez de aceptarla sin escanear (ver comentario en
            // IAntivirusScanner).
            throw new AntivirusNoDisponibleException("No se pudo contactar al servicio de escaneo antivirus.", ex);
        }
    }
}

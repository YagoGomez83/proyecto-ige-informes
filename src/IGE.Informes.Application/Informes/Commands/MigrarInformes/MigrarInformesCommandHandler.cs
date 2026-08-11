using IGE.Informes.Application.Common;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.MigrarInformes;

public sealed class MigrarInformesCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IInformePdfParser parser,
    IAuditLogger auditLogger,
    IFileStorage fileStorage,
    IAntivirusScanner antivirusScanner,
    TimeSpan? timeoutPorArchivo = null) : IRequestHandler<MigrarInformesCommand, MigracionLoteResultDto>
{
    private const string TipoMimePdf = "application/pdf";

    // Parámetro configurable (default: PdfParserTimeoutHelper.TimeoutPorDefecto,
    // 30s) solo para poder testear el timeout sin esperar 30s reales por
    // test — DependencyInjection.cs no lo pasa, así que en producción
    // siempre usa el default. Ver PdfParserTimeoutHelper para el porqué del
    // timeout (mismo mecanismo que usa ParsearPdfInformeQueryHandler, HU-01).
    private readonly TimeSpan? _timeoutPorArchivo = timeoutPorArchivo;

    public async Task<MigracionLoteResultDto> Handle(MigrarInformesCommand request, CancellationToken cancellationToken)
    {
        var dependenciaExiste = await dbContext.Dependencias.AnyAsync(d => d.Id == request.DependenciaDestinoId, cancellationToken);
        if (!dependenciaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaDestinoId);
        }

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        // El AuditLogInterceptor solo audita entidades que efectivamente se
        // persisten — un lote donde todos los PDFs quedan con Advertencia o
        // Fallido no dejaría ningún rastro de que la migración se ejecutó.
        // Se registra el intento en sí, independiente del resultado, porque
        // es una operación sensible de Admin sobre datos históricos
        // (hallazgo del security-reviewer).
        await auditLogger.RegistrarAccesoAsync("MigracionLote", nameof(Informe), null, cancellationToken);

        // Incluye tanto Informe.IdRegistro como MigracionPendiente.IdRegistro
        // (de MigracionesPendientes con IdRegistro ya reconocido) — si un
        // PDF ya migrado en una corrida anterior queda pendiente por falta
        // de fecha, y se vuelve a incluir el mismo archivo en un lote
        // posterior, este chequeo evita chocar contra el índice único de
        // MigracionesPendientes.IdRegistro (bug real detectado en
        // producción: sin este chequeo, el DbUpdateException sin capturar
        // abortaba el lote entero, no solo el archivo repetido).
        var idsRegistrados = await dbContext.Informes
            .Select(i => i.IdRegistro)
            .Union(dbContext.MigracionesPendientes
                .Where(m => m.IdRegistro != null)
                .Select(m => m.IdRegistro!))
            .ToListAsync(cancellationToken);
        var idsRegistradosEnLote = new HashSet<string>();

        var detalle = new List<MigracionArchivoResultDto>();

        foreach (var pdf in request.Pdfs)
        {
            InformeExtraidoDto extraido;
            try
            {
                extraido = await parser.ParsearConTimeoutAsync(pdf.Contenido, cancellationToken, _timeoutPorArchivo);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // No se expone ex.Message al usuario final — podría filtrar
                // detalles internos de la librería de parseo (paths, stack
                // trace parcial), aunque el endpoint ya exige rol Admin.
                // Cubre tanto PDFs no legibles como el timeout de
                // ParsearConTimeoutAsync (TimeoutException).
                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Fallido, "El archivo no es un PDF legible, no sigue la plantilla esperada, o tardó demasiado en procesarse."));
                continue;
            }

            if (extraido.RequiereRevisionManual)
            {
                // A diferencia de la versión inicial de esta HU, el PDF ya no
                // se descarta: se sube a MinIO (mismo fail-closed de
                // antivirus que el resto de los caminos) y se persiste una
                // MigracionPendiente con IdRegistro = null, para que el
                // Administrador lo complete después sin volver a subir el
                // archivo (HU-04, escenario "PDF con ID Registro no
                // reconocido también queda pendiente, no se pierde"). No hay
                // chequeo de "ID Registro duplicado en el lote" posible acá
                // — todavía no hay ID Registro.
                var estaLimpioSinId = await antivirusScanner.EstaLimpioAsync(pdf.Contenido, cancellationToken);
                if (!estaLimpioSinId)
                {
                    detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Fallido, "El archivo fue rechazado por el escaneo antivirus — no se subió ni se guardó ningún dato."));
                    continue;
                }

                using var streamPdfSinId = new MemoryStream(pdf.Contenido);
                var pdfPathSinId = await fileStorage.SubirAsync(pdf.NombreArchivo, streamPdfSinId, TipoMimePdf, cancellationToken);

                var migracionPendienteSinId = new MigracionPendiente(
                    null,
                    pdfPathSinId,
                    request.DependenciaDestinoId,
                    usuarioId,
                    extraido.CausaCaratula,
                    extraido.PiezaSumarial,
                    extraido.Relato);

                dbContext.MigracionesPendientes.Add(migracionPendienteSinId);

                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.ConAdvertencia, "No se reconoció el ID Registro.", extraido.Destino));
                continue;
            }

            var idRegistro = extraido.IdRegistro!;

            if (idsRegistrados.Contains(idRegistro) || idsRegistradosEnLote.Contains(idRegistro))
            {
                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.ConAdvertencia, $"ID Registro duplicado: {idRegistro}.", extraido.Destino));
                continue;
            }

            if (extraido.FechaAnalisis is null)
            {
                // No inventar la fecha con DateTime.UtcNow: un fallback silencioso
                // dejaba Informes "Exitosos" con la fecha de la migración en vez de
                // la fecha real del análisis, sin ninguna advertencia visible en el
                // reporte (hallazgo detectado revisando una migración real).
                //
                // A diferencia del "ID Registro no reconocido" de arriba, acá sí
                // hay una clave (idRegistro) para poder completar el dato después
                // sin volver a subir el archivo — se guarda el PDF en MinIO y se
                // persiste una MigracionPendiente con los datos ya extraídos
                // (HU-04, escenario "PDF con Fecha de Análisis no reconocida
                // queda pendiente, no se pierde").
                var estaLimpio = await antivirusScanner.EstaLimpioAsync(pdf.Contenido, cancellationToken);
                if (!estaLimpio)
                {
                    detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Fallido, "El archivo fue rechazado por el escaneo antivirus — no se subió ni se guardó ningún dato."));
                    continue;
                }

                using var streamPdf = new MemoryStream(pdf.Contenido);
                var pdfPath = await fileStorage.SubirAsync(pdf.NombreArchivo, streamPdf, TipoMimePdf, cancellationToken);

                var migracionPendiente = new MigracionPendiente(
                    idRegistro,
                    pdfPath,
                    request.DependenciaDestinoId,
                    usuarioId,
                    extraido.CausaCaratula,
                    extraido.PiezaSumarial,
                    extraido.Relato);

                dbContext.MigracionesPendientes.Add(migracionPendiente);
                idsRegistradosEnLote.Add(idRegistro);

                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.ConAdvertencia, "No se reconoció la Fecha de Análisis.", extraido.Destino));
                continue;
            }

            // El parser nunca extrae Circunscripción judicial (no es un
            // patrón presente en el texto del PDF, ver skill
            // pdf-informe-parser) y Causa exige los 3 campos no vacíos —
            // por eso ningún Informe migrado nace con Causa asociada; el
            // Admin la completa después vía HU-02 si hace falta. Mismo
            // criterio "los 3 campos o ninguno" que ya usa
            // ConfirmarCargaInformeCommandHandler.
            var estaLimpioExitoso = await antivirusScanner.EstaLimpioAsync(pdf.Contenido, cancellationToken);
            if (!estaLimpioExitoso)
            {
                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Fallido, "El archivo fue rechazado por el escaneo antivirus — no se subió ni se guardó ningún dato."));
                continue;
            }

            using var streamPdfExitoso = new MemoryStream(pdf.Contenido);
            var pdfPathExitoso = await fileStorage.SubirAsync(pdf.NombreArchivo, streamPdfExitoso, TipoMimePdf, cancellationToken);

            var informe = Informe.CrearMigrado(idRegistro, extraido.FechaAnalisis.Value, request.DependenciaDestinoId, usuarioId);
            informe.AsignarPdf(pdfPathExitoso);

            if (!string.IsNullOrWhiteSpace(extraido.Relato))
            {
                informe.CompletarRelato(extraido.Relato);
            }

            dbContext.Informes.Add(informe);
            idsRegistradosEnLote.Add(idRegistro);

            detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Exitoso, null, extraido.Destino));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MigracionLoteResultDto(
            detalle.Count,
            detalle.Count(d => d.Resultado == ResultadoMigracionArchivo.Exitoso),
            detalle.Count(d => d.Resultado == ResultadoMigracionArchivo.ConAdvertencia),
            detalle.Count(d => d.Resultado == ResultadoMigracionArchivo.Fallido),
            detalle);
    }
}

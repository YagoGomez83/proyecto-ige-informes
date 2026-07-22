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
    IAuditLogger auditLogger) : IRequestHandler<MigrarInformesCommand, MigracionLoteResultDto>
{
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

        var idsRegistrados = await dbContext.Informes
            .Select(i => i.IdRegistro)
            .ToListAsync(cancellationToken);
        var idsRegistradosEnLote = new HashSet<string>();

        var detalle = new List<MigracionArchivoResultDto>();

        foreach (var pdf in request.Pdfs)
        {
            InformeExtraidoDto extraido;
            try
            {
                using var stream = new MemoryStream(pdf.Contenido);
                extraido = parser.Parsear(stream);
            }
            catch (Exception)
            {
                // No se expone ex.Message al usuario final — podría filtrar
                // detalles internos de la librería de parseo (paths, stack
                // trace parcial), aunque el endpoint ya exige rol Admin.
                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Fallido, "El archivo no es un PDF legible o no sigue la plantilla esperada."));
                continue;
            }

            if (extraido.RequiereRevisionManual)
            {
                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.ConAdvertencia, "No se reconoció el ID Registro."));
                continue;
            }

            var idRegistro = extraido.IdRegistro!;

            if (idsRegistrados.Contains(idRegistro) || idsRegistradosEnLote.Contains(idRegistro))
            {
                detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.ConAdvertencia, $"ID Registro duplicado: {idRegistro}."));
                continue;
            }

            // El parser nunca extrae Circunscripción judicial (no es un
            // patrón presente en el texto del PDF, ver skill
            // pdf-informe-parser) y Causa exige los 3 campos no vacíos —
            // por eso ningún Informe migrado nace con Causa asociada; el
            // Admin la completa después vía HU-02 si hace falta. Mismo
            // criterio "los 3 campos o ninguno" que ya usa
            // ConfirmarCargaInformeCommandHandler.
            var fechaAnalisis = extraido.FechaAnalisis ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var informe = Informe.CrearMigrado(idRegistro, fechaAnalisis, request.DependenciaDestinoId, usuarioId);

            if (!string.IsNullOrWhiteSpace(extraido.Relato))
            {
                informe.CompletarRelato(extraido.Relato);
            }

            dbContext.Informes.Add(informe);
            idsRegistradosEnLote.Add(idRegistro);

            detalle.Add(new MigracionArchivoResultDto(pdf.NombreArchivo, ResultadoMigracionArchivo.Exitoso, null));
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

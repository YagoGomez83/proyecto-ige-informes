using IGE.Informes.Application.Common;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ReprocesarMigracionPendiente;

public sealed class ReprocesarMigracionPendienteQueryHandler(
    IAppDbContext dbContext,
    IFileStorage fileStorage,
    IInformePdfParser parser,
    IAuditLogger auditLogger,
    TimeSpan? timeoutParseo = null) : IRequestHandler<ReprocesarMigracionPendienteQuery, ReprocesarMigracionPendienteResultDto>
{
    public async Task<ReprocesarMigracionPendienteResultDto> Handle(
        ReprocesarMigracionPendienteQuery request, CancellationToken cancellationToken)
    {
        var migracionPendiente = await dbContext.MigracionesPendientes
            .FirstOrDefaultAsync(m => m.Id == request.MigracionPendienteId, cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(MigracionPendiente), request.MigracionPendienteId);

        // Mismo criterio que ListarMigracionesPendientesQueryHandler: leer
        // el PDF de una MigracionPendiente es acceso a datos de
        // investigación (Relato/Carátula), auditable aunque no se persista
        // ni se devuelva nada al cliente (hallazgo del security-reviewer —
        // CLAUDE.md exige auditar toda lectura, no solo escritura).
        await auditLogger.RegistrarAccesoAsync("ReprocesarPdf", nameof(MigracionPendiente), migracionPendiente.Id, cancellationToken);

        var contenido = await fileStorage.DescargarAsync(migracionPendiente.PdfPath, cancellationToken);

        InformeExtraidoDto extraido;
        try
        {
            extraido = await parser.ParsearConTimeoutAsync(contenido, cancellationToken, timeoutParseo);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Mismo criterio que ParsearPdfInformeQueryHandler/
            // MigrarInformesCommandHandler: no exponer el detalle interno
            // de la excepción, solo informar que no se pudo reprocesar.
            throw new ReglaDeNegocioVioladaException(
                "No se pudo reprocesar el archivo: no es un PDF legible o tardó demasiado en procesarse.");
        }

        if (extraido.Personas.Count > 0)
        {
            // Mismo criterio que ParsearPdfInformeQueryHandler: el DNI
            // extraído del PDF es un dato personal sensible aunque no
            // corresponda a una fila de Persona persistida ni se exponga en
            // el resultado de esta Query — el checklist de amenazas exige
            // auditar el acceso al dato, no solo la lectura de una entidad
            // ya existente en la base.
            await auditLogger.RegistrarAccesoAsync("ExtraccionPdf", nameof(Persona), null, cancellationToken);
        }

        return new ReprocesarMigracionPendienteResultDto(extraido.IdRegistro, extraido.FechaAnalisis);
    }
}

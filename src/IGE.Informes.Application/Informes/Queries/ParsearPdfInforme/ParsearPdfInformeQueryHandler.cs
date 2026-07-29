using IGE.Informes.Application.Common;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ParsearPdfInforme;

public sealed class ParsearPdfInformeQueryHandler(
    IAppDbContext dbContext,
    IInformePdfParser parser,
    IAuditLogger auditLogger,
    TimeSpan? timeoutParseo = null) : IRequestHandler<ParsearPdfInformeQuery, ParsearPdfInformeResultDto>
{
    public async Task<ParsearPdfInformeResultDto> Handle(ParsearPdfInformeQuery request, CancellationToken cancellationToken)
    {
        InformeExtraidoDto extraido;
        try
        {
            extraido = await parser.ParsearConTimeoutAsync(request.ContenidoPdf, cancellationToken, timeoutParseo);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // No se expone ex.Message al usuario final (mismo criterio que
            // MigrarInformesCommandHandler) — cubre tanto PDFs no legibles
            // como el timeout de ParsearConTimeoutAsync (TimeoutException).
            throw new ReglaDeNegocioVioladaException(
                "No se pudo procesar el archivo: no es un PDF legible, no sigue la plantilla esperada, o tardó demasiado en procesarse.");
        }

        var yaExiste = extraido.IdRegistro is not null
            && await dbContext.Informes.AnyAsync(i => i.IdRegistro == extraido.IdRegistro, cancellationToken);

        var codigosCamara = extraido.Evidencias
            .Where(e => e.CodigoCamara is not null)
            .Select(e => e.CodigoCamara!)
            .Distinct()
            .ToList();

        var camarasPorCodigo = codigosCamara.Count == 0
            ? []
            : await dbContext.Camaras
                .Where(c => codigosCamara.Contains(c.Codigo))
                .ToDictionaryAsync(c => c.Codigo, c => c.Id, cancellationToken);

        if (extraido.Personas.Count > 0)
        {
            // El DNI extraído del PDF es un dato personal sensible aunque
            // todavía no corresponda a una fila de Persona persistida — el
            // checklist de amenazas exige auditar el acceso al dato, no
            // solo la lectura de una entidad ya existente en la base.
            await auditLogger.RegistrarAccesoAsync("ExtraccionPdf", nameof(Persona), null, cancellationToken);
        }

        return new ParsearPdfInformeResultDto(
            extraido.IdRegistro,
            extraido.FechaAnalisis,
            extraido.CausaCaratula,
            extraido.Destino,
            extraido.PiezaSumarial,
            extraido.Relato,
            extraido.Vehiculos.Select(v => new VehiculoExtraidoResultDto(v.Marca, v.Modelo, v.Dominio, v.DominioOriginal)).ToList(),
            extraido.Personas.Select(p => new PersonaExtraidaResultDto(p.Dni, p.RolSugerido)).ToList(),
            extraido.Evidencias.Select(e => new EvidenciaExtraidaResultDto(
                e.NumeroImagen,
                e.CodigoCamara,
                e.CodigoCamara is not null && camarasPorCodigo.TryGetValue(e.CodigoCamara, out var camaraId) ? camaraId : null,
                e.Ubicacion,
                e.FechaHoraCaptura,
                e.Descripcion)).ToList(),
            extraido.RequiereRevisionManual,
            yaExiste);
    }
}

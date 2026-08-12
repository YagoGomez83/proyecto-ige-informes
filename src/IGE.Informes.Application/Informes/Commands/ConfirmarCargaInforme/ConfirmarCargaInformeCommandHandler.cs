using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.ConfirmarCargaInforme;

public sealed class ConfirmarCargaInformeCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    IAntivirusScanner antivirusScanner) : IRequestHandler<ConfirmarCargaInformeCommand, Guid>
{
    private const string TipoMimePdf = "application/pdf";

    public async Task<Guid> Handle(ConfirmarCargaInformeCommand request, CancellationToken cancellationToken)
    {
        if (request.CasoAnalisisId is { } casoAnalisisId)
        {
            var casoExiste = await dbContext.CasosAnalisis.AnyAsync(c => c.Id == casoAnalisisId, cancellationToken);
            if (!casoExiste)
            {
                throw new EntidadNoEncontradaException(nameof(CasoAnalisis), casoAnalisisId);
            }
        }

        var dependenciaExiste = await dbContext.Dependencias.AnyAsync(d => d.Id == request.DependenciaDestinoId, cancellationToken);
        if (!dependenciaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaDestinoId);
        }

        var camaraIdsInformados = request.Evidencias
            .Where(e => e.CamaraId is not null)
            .Select(e => e.CamaraId!.Value)
            .Distinct()
            .ToList();

        if (camaraIdsInformados.Count > 0)
        {
            var camaraIdsExistentes = await dbContext.Camaras
                .Where(c => camaraIdsInformados.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var camaraIdInexistente = camaraIdsInformados.Except(camaraIdsExistentes).FirstOrDefault();
            if (camaraIdInexistente != default)
            {
                throw new EntidadNoEncontradaException(nameof(Camara), camaraIdInexistente);
            }
        }

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        var informeExistente = await dbContext.Informes
            .FirstOrDefaultAsync(i => i.IdRegistro == request.IdRegistro, cancellationToken);

        if (informeExistente is not null && !request.ReemplazarSiExiste)
        {
            throw new EntidadDuplicadaException(nameof(Informe), nameof(Informe.IdRegistro), request.IdRegistro);
        }

        if (informeExistente is { Estado: EstadoInforme.Publicado })
        {
            throw new InvalidOperationException(
                $"El Informe '{request.IdRegistro}' ya está Publicado y es inmutable — no se puede reemplazar por esta vía.");
        }

        Causa? causa = null;
        if (!string.IsNullOrWhiteSpace(request.CausaCaratula)
            && !string.IsNullOrWhiteSpace(request.CausaNroPiezaSumarial))
        {
            causa = new Causa(request.CausaCaratula, request.CausaNroPiezaSumarial, request.CausaCircunscripcionJudicial);
            dbContext.Causas.Add(causa);
        }

        // Escaneo antivirus antes de persistir en MinIO (ver
        // docs/06-seguridad-amenazas.md, "Ingesta de PDFs" — "subida de
        // archivo malicioso disfrazado de PDF"). Fail-closed: si ClamAV no
        // responde, AntivirusNoDisponibleException se propaga sin capturar
        // — se prefiere rechazar la carga a aceptar un archivo sin escanear.
        var estaLimpio = await antivirusScanner.EstaLimpioAsync(request.ContenidoPdf, cancellationToken);
        if (!estaLimpio)
        {
            throw new ReglaDeNegocioVioladaException(
                "El archivo fue rechazado por el escaneo antivirus — no se subió ni se guardó ningún dato.");
        }

        using var streamPdf = new MemoryStream(request.ContenidoPdf);
        var pdfPath = await fileStorage.SubirAsync(request.NombreArchivo, streamPdf, TipoMimePdf, cancellationToken);

        Informe informe;
        if (informeExistente is not null)
        {
            // Reemplazo explícito de una versión existente (HU-01,
            // escenario "ID Registro duplicado") — se corrige el mismo
            // registro; sus Evidencia anteriores se reemplazan por las de
            // esta nueva extracción, no se acumulan.
            informe = informeExistente;

            var evidenciasAnteriores = await dbContext.Evidencias
                .Where(e => e.InformeId == informe.Id)
                .ToListAsync(cancellationToken);
            dbContext.Evidencias.RemoveRange(evidenciasAnteriores);
        }
        else
        {
            informe = request.CasoAnalisisId is { } casoId
                ? new Informe(
                    request.IdRegistro,
                    request.FechaAnalisis,
                    casoId,
                    request.DependenciaDestinoId,
                    usuarioId,
                    causa?.Id)
                : Informe.CrearMigrado(
                    request.IdRegistro,
                    request.FechaAnalisis,
                    request.DependenciaDestinoId,
                    usuarioId,
                    causa?.Id);

            dbContext.Informes.Add(informe);
        }

        if (causa is not null)
        {
            informe.AsignarCausa(causa.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.Relato))
        {
            informe.CompletarRelato(request.Relato);
        }

        informe.AsignarPdf(pdfPath);

        foreach (var evidenciaDto in request.Evidencias)
        {
            var evidencia = new Evidencia(
                evidenciaDto.NumeroImagen,
                informe.Id,
                evidenciaDto.CamaraId,
                evidenciaDto.FechaHoraCaptura,
                evidenciaDto.Descripcion);

            dbContext.Evidencias.Add(evidencia);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return informe.Id;
    }
}

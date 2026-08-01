using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;

public sealed class ObtenerInformePorIdQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger, IFileStorage fileStorage)
    : IRequestHandler<ObtenerInformePorIdQuery, InformeDto?>
{
    public async Task<InformeDto?> Handle(ObtenerInformePorIdQuery request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InformeId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Informe), request.InformeId, cancellationToken);

        if (informe is null)
        {
            return null;
        }

        var causa = informe.CausaId is null
            ? null
            : await dbContext.Causas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == informe.CausaId, cancellationToken);

        // URL prefirmada de corta expiración para la vista previa del PDF en
        // la ficha de detalle — nunca se persiste ni se expone la clave cruda
        // del bucket, se regenera en cada consulta (ver IFileStorage).
        var pdfUrl = informe.PdfPath is null
            ? null
            : await fileStorage.ObtenerUrlDescargaAsync(informe.PdfPath, cancellationToken);

        var evidencias = await dbContext.Evidencias.AsNoTracking()
            .Where(e => e.InformeId == informe.Id)
            .ToListAsync(cancellationToken);

        var vehiculoIds = evidencias.SelectMany(e => e.VehiculoIds).Distinct().ToList();
        var personaIds = evidencias.SelectMany(e => e.PersonaIds).Distinct().ToList();

        var vehiculosVinculados = vehiculoIds.Count == 0
            ? []
            : await dbContext.Vehiculos.AsNoTracking()
                .Where(v => vehiculoIds.Contains(v.Id))
                .Select(v => new VehiculoVinculadoDto(v.Id, v.Marca, v.Modelo, v.Dominio))
                .ToListAsync(cancellationToken);

        var personasVinculadas = personaIds.Count == 0
            ? []
            : await dbContext.Personas.AsNoTracking()
                .Where(p => personaIds.Contains(p.Id))
                .Select(p => new PersonaVinculadaDto(p.Id, p.Nombre, p.Dni, p.Rol))
                .ToListAsync(cancellationToken);

        // Un registro de auditoría por Id, no agregado — un acceso que
        // expone varios DNI/dominios distintos debe poder reconstruirse
        // individualmente (CLAUDE.md, sección Seguridad).
        foreach (var vehiculo in vehiculosVinculados)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), vehiculo.Id, cancellationToken);
        }

        foreach (var persona in personasVinculadas)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), persona.Id, cancellationToken);
        }

        return new InformeDto(
            informe.Id,
            informe.IdRegistro,
            informe.FechaAnalisis,
            informe.Relato,
            informe.CasoAnalisisId,
            informe.CausaId,
            causa?.Caratula,
            causa?.NroPiezaSumarial,
            causa?.CircunscripcionJudicial,
            informe.DependenciaDestinoId,
            informe.PdfPath,
            pdfUrl,
            informe.Estado,
            informe.Origen,
            vehiculosVinculados,
            personasVinculadas);
    }
}

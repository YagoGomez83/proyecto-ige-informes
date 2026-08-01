using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Queries.ListarImagenesPersona;

public sealed class ListarImagenesPersonaQueryHandler(IAppDbContext dbContext, IFileStorage fileStorage, IAuditLogger auditLogger)
    : IRequestHandler<ListarImagenesPersonaQuery, IReadOnlyCollection<PersonaImagenDto>>
{
    public async Task<IReadOnlyCollection<PersonaImagenDto>> Handle(
        ListarImagenesPersonaQuery request, CancellationToken cancellationToken)
    {
        var imagenes = await dbContext.PersonaImagenes.AsNoTracking()
            .Where(pi => pi.PersonaId == request.PersonaId)
            .OrderByDescending(pi => pi.FechaCarga)
            .ToListAsync(cancellationToken);

        var resultado = new List<PersonaImagenDto>(imagenes.Count);
        foreach (var imagen in imagenes)
        {
            var url = await fileStorage.ObtenerUrlDescargaAsync(imagen.ImagenPath, cancellationToken);
            resultado.Add(new PersonaImagenDto(imagen.Id, url, imagen.FechaCarga));
        }

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), request.PersonaId, cancellationToken);

        return resultado;
    }
}

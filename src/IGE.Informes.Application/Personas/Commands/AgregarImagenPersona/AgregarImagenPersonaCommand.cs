using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.AgregarImagenPersona;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AgregarImagenPersonaCommand(
    Guid PersonaId,
    byte[] Contenido,
    string NombreArchivo,
    string TipoMime) : IRequest<Guid>;

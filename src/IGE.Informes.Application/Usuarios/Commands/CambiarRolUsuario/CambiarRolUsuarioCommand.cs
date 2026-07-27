using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.CambiarRolUsuario;

[Autorizar(Roles.Admin)]
public sealed record CambiarRolUsuarioCommand(Guid UsuarioId, string NuevoRol) : IRequest;

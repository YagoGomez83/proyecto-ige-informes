using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.CrearUsuario;

[Autorizar(Roles.Admin)]
public sealed record CrearUsuarioCommand(string NombreCompleto, string Email, string Password, string Rol) : IRequest<Guid>;

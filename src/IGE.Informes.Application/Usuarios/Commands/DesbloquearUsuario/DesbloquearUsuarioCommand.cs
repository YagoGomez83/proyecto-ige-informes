using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.DesbloquearUsuario;

[Autorizar(Roles.Admin)]
public sealed record DesbloquearUsuarioCommand(Guid UsuarioId) : IRequest;

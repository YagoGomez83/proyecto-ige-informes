using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.BloquearUsuario;

[Autorizar(Roles.Admin)]
public sealed record BloquearUsuarioCommand(Guid UsuarioId) : IRequest;

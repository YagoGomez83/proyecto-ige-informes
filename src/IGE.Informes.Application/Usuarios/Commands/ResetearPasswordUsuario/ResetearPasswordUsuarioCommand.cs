using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.ResetearPasswordUsuario;

[Autorizar(Roles.Admin)]
public sealed record ResetearPasswordUsuarioCommand(Guid UsuarioId, string NuevaPassword) : IRequest;

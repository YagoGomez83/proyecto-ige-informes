using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Queries.ListarUsuarios;

[Autorizar(Roles.Admin)]
public sealed record ListarUsuariosQuery : IRequest<IReadOnlyCollection<UsuarioDto>>;

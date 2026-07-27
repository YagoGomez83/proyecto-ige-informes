using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Queries.ListarUsuarios;

public sealed class ListarUsuariosQueryHandler(IUserManagementService userManagementService)
    : IRequestHandler<ListarUsuariosQuery, IReadOnlyCollection<UsuarioDto>>
{
    public Task<IReadOnlyCollection<UsuarioDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken) =>
        userManagementService.ListarUsuariosAsync(cancellationToken);
}

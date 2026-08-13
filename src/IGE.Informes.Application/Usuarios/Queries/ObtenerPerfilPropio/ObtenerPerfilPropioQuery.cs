using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Queries.ObtenerPerfilPropio;

public sealed record PerfilPropioDto(string NombreCompleto, string Email, string Rol, string? ImagenPerfilUrl);

/// <summary>
/// Datos del propio usuario autenticado para la pantalla de Perfil —
/// nunca recibe un UsuarioId del cliente, siempre resuelve sobre
/// ICurrentUserService (mismo criterio que CambiarPasswordPropiaCommand).
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerPerfilPropioQuery : IRequest<PerfilPropioDto>;

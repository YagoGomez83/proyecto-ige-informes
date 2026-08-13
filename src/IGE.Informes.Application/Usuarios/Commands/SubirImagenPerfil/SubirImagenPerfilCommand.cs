using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.SubirImagenPerfil;

/// <summary>
/// Reemplaza la foto de perfil del propio usuario autenticado — nunca
/// recibe un UsuarioId del cliente, mismo criterio que
/// CambiarPasswordPropiaCommand. A diferencia de AgregarImagenVehiculo/
/// PersonaCommand (colección con histórico), acá es un campo único: subir
/// una nueva reemplaza la anterior.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record SubirImagenPerfilCommand(byte[] Contenido, string NombreArchivo, string TipoMime) : IRequest;

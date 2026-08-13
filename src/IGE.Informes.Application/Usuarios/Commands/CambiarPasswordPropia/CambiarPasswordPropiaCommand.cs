using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.CambiarPasswordPropia;

/// <summary>
/// Cambio de la propia contraseña, con la actual como requisito (a
/// diferencia de ResetearPasswordUsuarioCommand, que es exclusivo de Admin
/// sobre la cuenta de otro usuario y no requiere conocer la anterior).
/// Abierto a los 3 roles — todo usuario autenticado tiene una cuenta propia
/// que administrar.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CambiarPasswordPropiaCommand(string PasswordActual, string PasswordNueva) : IRequest;

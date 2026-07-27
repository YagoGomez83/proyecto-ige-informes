using FluentValidation;
using IGE.Informes.Application.Common.Security;

namespace IGE.Informes.Application.Usuarios.Commands.CambiarRolUsuario;

public sealed class CambiarRolUsuarioCommandValidator : AbstractValidator<CambiarRolUsuarioCommand>
{
    public CambiarRolUsuarioCommandValidator()
    {
        RuleFor(x => x.UsuarioId).NotEmpty();
        RuleFor(x => x.NuevoRol).NotEmpty().Must(rol => Roles.Todos.Contains(rol))
            .WithMessage($"El rol debe ser uno de: {string.Join(", ", Roles.Todos)}.");
    }
}

using FluentValidation;
using IGE.Informes.Application.Common.Security;

namespace IGE.Informes.Application.Usuarios.Commands.CrearUsuario;

public sealed class CrearUsuarioCommandValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioCommandValidator()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12);
        RuleFor(x => x.Rol).NotEmpty().Must(rol => Roles.Todos.Contains(rol))
            .WithMessage($"El rol debe ser uno de: {string.Join(", ", Roles.Todos)}.");
    }
}

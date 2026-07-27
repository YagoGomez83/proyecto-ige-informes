using FluentValidation;

namespace IGE.Informes.Application.Usuarios.Commands.DesbloquearUsuario;

public sealed class DesbloquearUsuarioCommandValidator : AbstractValidator<DesbloquearUsuarioCommand>
{
    public DesbloquearUsuarioCommandValidator()
    {
        RuleFor(x => x.UsuarioId).NotEmpty();
    }
}

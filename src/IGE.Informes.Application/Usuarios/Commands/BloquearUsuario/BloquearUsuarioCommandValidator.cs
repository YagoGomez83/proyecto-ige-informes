using FluentValidation;

namespace IGE.Informes.Application.Usuarios.Commands.BloquearUsuario;

public sealed class BloquearUsuarioCommandValidator : AbstractValidator<BloquearUsuarioCommand>
{
    public BloquearUsuarioCommandValidator()
    {
        RuleFor(x => x.UsuarioId).NotEmpty();
    }
}

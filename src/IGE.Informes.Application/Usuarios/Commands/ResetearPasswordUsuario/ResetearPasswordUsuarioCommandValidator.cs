using FluentValidation;

namespace IGE.Informes.Application.Usuarios.Commands.ResetearPasswordUsuario;

public sealed class ResetearPasswordUsuarioCommandValidator : AbstractValidator<ResetearPasswordUsuarioCommand>
{
    public ResetearPasswordUsuarioCommandValidator()
    {
        RuleFor(x => x.UsuarioId).NotEmpty();
        RuleFor(x => x.NuevaPassword).NotEmpty().MinimumLength(12);
    }
}

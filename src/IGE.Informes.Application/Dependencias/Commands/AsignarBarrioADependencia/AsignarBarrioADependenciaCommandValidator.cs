using FluentValidation;

namespace IGE.Informes.Application.Dependencias.Commands.AsignarBarrioADependencia;

public sealed class AsignarBarrioADependenciaCommandValidator : AbstractValidator<AsignarBarrioADependenciaCommand>
{
    public AsignarBarrioADependenciaCommandValidator()
    {
        RuleFor(x => x.DependenciaId).NotEmpty();
        RuleFor(x => x.BarrioId).NotEmpty();
    }
}

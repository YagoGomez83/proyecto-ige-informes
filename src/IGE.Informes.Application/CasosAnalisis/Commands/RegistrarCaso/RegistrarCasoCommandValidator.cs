using FluentValidation;

namespace IGE.Informes.Application.CasosAnalisis.Commands.RegistrarCaso;

public sealed class RegistrarCasoCommandValidator : AbstractValidator<RegistrarCasoCommand>
{
    public RegistrarCasoCommandValidator()
    {
        RuleFor(x => x.Fecha).NotEqual(default(DateOnly));
        RuleFor(x => x.DependenciaId).NotEmpty();
        RuleFor(x => x.TipoIncidenteId).NotEmpty();
        RuleFor(x => x.NroLlamado911).MaximumLength(50);
        RuleFor(x => x.Observaciones).MaximumLength(4000);
    }
}

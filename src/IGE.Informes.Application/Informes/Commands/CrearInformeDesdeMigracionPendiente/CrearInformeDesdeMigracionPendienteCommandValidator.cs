using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;

public sealed class CrearInformeDesdeMigracionPendienteCommandValidator : AbstractValidator<CrearInformeDesdeMigracionPendienteCommand>
{
    public CrearInformeDesdeMigracionPendienteCommandValidator()
    {
        RuleFor(x => x.MigracionPendienteId).NotEmpty();

        RuleFor(x => x.FechaAnalisis)
            .NotEqual(default(DateOnly))
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La Fecha de Análisis no puede ser futura.");
    }
}

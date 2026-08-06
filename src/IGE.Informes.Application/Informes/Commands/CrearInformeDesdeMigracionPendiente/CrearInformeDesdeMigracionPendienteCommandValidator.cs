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

        // Si el IdRegistro es obligatorio (la MigracionPendiente no
        // tenía uno propio) es una regla que depende de la base — queda
        // en el Handler. Acá solo se rechaza un dato explícitamente
        // inválido: string vacío/blanco es distinto de null ("no se
        // informa, tal vez no hacía falta").
        RuleFor(x => x.IdRegistro)
            .Must(idRegistro => idRegistro is null || !string.IsNullOrWhiteSpace(idRegistro))
            .WithMessage("El ID Registro no puede ser una cadena vacía.");
    }
}

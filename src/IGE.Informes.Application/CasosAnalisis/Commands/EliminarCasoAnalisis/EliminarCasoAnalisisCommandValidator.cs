using FluentValidation;

namespace IGE.Informes.Application.CasosAnalisis.Commands.EliminarCasoAnalisis;

public sealed class EliminarCasoAnalisisCommandValidator : AbstractValidator<EliminarCasoAnalisisCommand>
{
    public EliminarCasoAnalisisCommandValidator()
    {
        RuleFor(x => x.CasoId).NotEmpty();
    }
}

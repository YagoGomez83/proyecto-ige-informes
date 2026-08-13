using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.EliminarInforme;

public sealed class EliminarInformeCommandValidator : AbstractValidator<EliminarInformeCommand>
{
    public EliminarInformeCommandValidator()
    {
        RuleFor(x => x.InformeId).NotEmpty();
    }
}

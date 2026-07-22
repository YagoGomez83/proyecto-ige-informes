using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.PublicarInforme;

public sealed class PublicarInformeCommandValidator : AbstractValidator<PublicarInformeCommand>
{
    public PublicarInformeCommandValidator()
    {
        RuleFor(x => x.InformeId).NotEmpty();
    }
}

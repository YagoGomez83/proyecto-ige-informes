using FluentValidation;

namespace IGE.Informes.Application.Alertas.Commands.MarcarAlertaAtendida;

public sealed class MarcarAlertaAtendidaCommandValidator : AbstractValidator<MarcarAlertaAtendidaCommand>
{
    public MarcarAlertaAtendidaCommandValidator()
    {
        RuleFor(x => x.AlertaId).NotEmpty();
    }
}

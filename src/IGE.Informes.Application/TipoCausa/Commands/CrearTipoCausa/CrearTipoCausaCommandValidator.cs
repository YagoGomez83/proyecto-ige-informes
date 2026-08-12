using FluentValidation;

namespace IGE.Informes.Application.TipoCausa.Commands.CrearTipoCausa;

public sealed class CrearTipoCausaCommandValidator : AbstractValidator<CrearTipoCausaCommand>
{
    public CrearTipoCausaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
    }
}

using FluentValidation;

namespace IGE.Informes.Application.TiposIncidente.Commands.CrearTipoIncidente;

public sealed class CrearTipoIncidenteCommandValidator : AbstractValidator<CrearTipoIncidenteCommand>
{
    public CrearTipoIncidenteCommandValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Descripcion).NotEmpty().MaximumLength(200);
    }
}

using FluentValidation;

namespace IGE.Informes.Application.Camaras.Commands.AsignarLocalidadACamara;

public sealed class AsignarLocalidadACamaraCommandValidator : AbstractValidator<AsignarLocalidadACamaraCommand>
{
    public AsignarLocalidadACamaraCommandValidator()
    {
        RuleFor(x => x.CamaraId).NotEmpty();
        RuleFor(x => x.LocalidadId).NotEmpty();
    }
}

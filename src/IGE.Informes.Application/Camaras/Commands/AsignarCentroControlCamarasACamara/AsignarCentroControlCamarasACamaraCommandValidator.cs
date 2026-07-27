using FluentValidation;

namespace IGE.Informes.Application.Camaras.Commands.AsignarCentroControlCamarasACamara;

public sealed class AsignarCentroControlCamarasACamaraCommandValidator : AbstractValidator<AsignarCentroControlCamarasACamaraCommand>
{
    public AsignarCentroControlCamarasACamaraCommandValidator()
    {
        RuleFor(x => x.CamaraId).NotEmpty();
        RuleFor(x => x.CentroControlCamarasId).NotEmpty();
    }
}

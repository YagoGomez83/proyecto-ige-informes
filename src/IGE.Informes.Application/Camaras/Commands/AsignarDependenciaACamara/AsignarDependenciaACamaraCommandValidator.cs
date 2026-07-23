using FluentValidation;

namespace IGE.Informes.Application.Camaras.Commands.AsignarDependenciaACamara;

public sealed class AsignarDependenciaACamaraCommandValidator : AbstractValidator<AsignarDependenciaACamaraCommand>
{
    public AsignarDependenciaACamaraCommandValidator()
    {
        RuleFor(x => x.CamaraId).NotEmpty();
        RuleFor(x => x.DependenciaId).NotEmpty();
    }
}

using FluentValidation;

namespace IGE.Informes.Application.Dependencias.Commands.AsignarUnidadRegionalADependencia;

public sealed class AsignarUnidadRegionalADependenciaCommandValidator : AbstractValidator<AsignarUnidadRegionalADependenciaCommand>
{
    public AsignarUnidadRegionalADependenciaCommandValidator()
    {
        RuleFor(x => x.DependenciaId).NotEmpty();
        RuleFor(x => x.UnidadRegionalId).NotEmpty();
    }
}

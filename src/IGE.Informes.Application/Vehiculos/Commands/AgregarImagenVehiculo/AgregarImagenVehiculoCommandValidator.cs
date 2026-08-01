using FluentValidation;
using IGE.Informes.Application.Common.Validation;

namespace IGE.Informes.Application.Vehiculos.Commands.AgregarImagenVehiculo;

public sealed class AgregarImagenVehiculoCommandValidator : AbstractValidator<AgregarImagenVehiculoCommand>
{
    private const int TamanioMaximoBytes = 10 * 1024 * 1024;

    public AgregarImagenVehiculoCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.NombreArchivo).NotEmpty();

        RuleFor(x => x.Contenido)
            .NotEmpty()
            .Must(c => c.Length <= TamanioMaximoBytes)
            .WithMessage($"La imagen no puede superar los {TamanioMaximoBytes / (1024 * 1024)}MB.");

        RuleFor(x => x.TipoMime)
            .Must(t => FormatoImagenHelper.TiposMimePermitidos.Contains(t))
            .WithMessage("Solo se aceptan imágenes JPEG, PNG o WebP.");

        // No confiar en el Content-Type declarado por el navegador — se
        // confirma contra los magic bytes reales del contenido (ver
        // FormatoImagenHelper).
        RuleFor(x => x)
            .Must(x => FormatoImagenHelper.CoincideConTipoDeclarado(x.Contenido, x.TipoMime))
            .WithMessage("El contenido del archivo no corresponde al formato declarado.")
            .When(x => FormatoImagenHelper.TiposMimePermitidos.Contains(x.TipoMime) && x.Contenido.Length > 0);
    }
}

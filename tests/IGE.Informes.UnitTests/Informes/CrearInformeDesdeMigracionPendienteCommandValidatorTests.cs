using IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — escenario "Completar la Fecha de
/// Análisis de una Migración Pendiente". Ver
/// CrearInformeDesdeMigracionPendienteCommandHandlerTests para el
/// comportamiento del Handler.
/// </summary>
public class CrearInformeDesdeMigracionPendienteCommandValidatorTests
{
    private readonly CrearInformeDesdeMigracionPendienteCommandValidator _validator = new();

    [Fact]
    public void Acepta_una_MigracionPendienteId_valida_con_fecha_informada()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), new DateOnly(2022, 6, 15));

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_MigracionPendienteId_vacio()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.Empty, new DateOnly(2022, 6, 15));

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_FechaAnalisis_en_el_futuro()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_FechaAnalisis_por_defecto_default_DateOnly()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), default);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }
}

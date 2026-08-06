using IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — escenarios "Completar la Fecha de
/// Análisis de una Migración Pendiente" y "Completar el ID Registro de una
/// Migración Pendiente". El Command ahora recibe además
/// <c>string? IdRegistro</c> — el Validator solo puede validar lo que no
/// requiere IO (igual criterio que ya aplica a FechaAnalisis): si el
/// IdRegistro del Command es obligatorio o no depende de si la
/// MigracionPendiente en base ya tenía uno, y eso el Validator no lo puede
/// saber sin consultar la base — esa regla queda en el Handler (ver
/// CrearInformeDesdeMigracionPendienteCommandHandlerTests). El Validator
/// solo rechaza un IdRegistro que venga como string vacío/solo espacios
/// (dato inválido explícito, distinto de null = "no se está informando").
/// Ver CrearInformeDesdeMigracionPendienteCommandHandlerTests para el
/// comportamiento del Handler.
/// </summary>
public class CrearInformeDesdeMigracionPendienteCommandValidatorTests
{
    private readonly CrearInformeDesdeMigracionPendienteCommandValidator _validator = new();

    [Fact]
    public void Acepta_una_MigracionPendienteId_valida_con_fecha_informada_sin_IdRegistro()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), new DateOnly(2022, 6, 15), null);

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Acepta_una_MigracionPendienteId_valida_con_fecha_y_con_IdRegistro_informados()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), new DateOnly(2022, 6, 15), "900/2022");

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_MigracionPendienteId_vacio()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.Empty, new DateOnly(2022, 6, 15), null);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_FechaAnalisis_en_el_futuro()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_FechaAnalisis_por_defecto_default_DateOnly()
    {
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), default, null);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_IdRegistro_vacio_o_solo_espacios_cuando_se_informa()
    {
        // No es lo mismo "no informar IdRegistro" (null, válido acá; el
        // Handler decide si hacía falta) que "informar un IdRegistro
        // inválido" (string vacío/blanco) — eso sí es un error de dato que
        // el Validator puede detectar sin IO.
        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), new DateOnly(2022, 6, 15), "   ");

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }
}

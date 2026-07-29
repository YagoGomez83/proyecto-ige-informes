using IGE.Informes.Application.TiposIncidente.Commands.CrearTipoIncidente;

namespace IGE.Informes.UnitTests.TiposIncidente;

/// <summary>
/// HU-18 (Catálogo de Tipos de Incidente) — cubre el Validator de
/// CrearTipoIncidenteCommand, mismo patrón que CrearBarrioCommandValidator
/// pero con dos campos obligatorios (Codigo + Descripcion).
/// </summary>
public class CrearTipoIncidenteCommandValidatorTests
{
    private readonly CrearTipoIncidenteCommandValidator _validator = new();

    [Fact]
    public void Rechaza_codigo_vacio()
    {
        var resultado = _validator.Validate(new CrearTipoIncidenteCommand(string.Empty, "Persona sospechosa"));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoIncidenteCommand.Codigo));
    }

    [Fact]
    public void Rechaza_descripcion_vacia()
    {
        var resultado = _validator.Validate(new CrearTipoIncidenteCommand("25", string.Empty));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoIncidenteCommand.Descripcion));
    }

    [Fact]
    public void Rechaza_codigo_que_excede_el_maximo_de_20_caracteres()
    {
        var codigoDemasiadoLargo = new string('1', 21);

        var resultado = _validator.Validate(new CrearTipoIncidenteCommand(codigoDemasiadoLargo, "Persona sospechosa"));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoIncidenteCommand.Codigo));
    }

    [Fact]
    public void Rechaza_descripcion_que_excede_el_maximo_de_200_caracteres()
    {
        var descripcionDemasiadoLarga = new string('a', 201);

        var resultado = _validator.Validate(new CrearTipoIncidenteCommand("25", descripcionDemasiadoLarga));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoIncidenteCommand.Descripcion));
    }

    [Fact]
    public void Acepta_codigo_y_descripcion_validos()
    {
        var resultado = _validator.Validate(new CrearTipoIncidenteCommand("25", "Persona sospechosa"));

        Assert.True(resultado.IsValid);
    }
}

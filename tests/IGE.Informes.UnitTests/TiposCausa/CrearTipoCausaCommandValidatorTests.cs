using IGE.Informes.Application.TipoCausa.Commands.CrearTipoCausa;

namespace IGE.Informes.UnitTests.TiposCausa;

/// <summary>
/// HU-19 (Catálogo de Tipos de Causa) — cubre el Validator de
/// CrearTipoCausaCommand, mismo patrón que CrearDependenciaCommandValidator
/// / CrearTipoIncidenteCommandValidatorTests pero con un único campo
/// obligatorio (Nombre).
/// </summary>
public class CrearTipoCausaCommandValidatorTests
{
    private readonly CrearTipoCausaCommandValidator _validator = new();

    [Fact]
    public void Rechaza_nombre_vacio()
    {
        var resultado = _validator.Validate(new CrearTipoCausaCommand(string.Empty));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoCausaCommand.Nombre));
    }

    [Fact]
    public void Rechaza_nombre_solo_con_espacios_en_blanco()
    {
        var resultado = _validator.Validate(new CrearTipoCausaCommand("   "));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoCausaCommand.Nombre));
    }

    [Fact]
    public void Rechaza_nombre_que_excede_el_maximo_de_200_caracteres()
    {
        var nombreDemasiadoLargo = new string('a', 201);

        var resultado = _validator.Validate(new CrearTipoCausaCommand(nombreDemasiadoLargo));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearTipoCausaCommand.Nombre));
    }

    [Fact]
    public void Acepta_nombre_valido()
    {
        var resultado = _validator.Validate(new CrearTipoCausaCommand("AV. ROBO CALIFICADO"));

        Assert.True(resultado.IsValid);
    }
}

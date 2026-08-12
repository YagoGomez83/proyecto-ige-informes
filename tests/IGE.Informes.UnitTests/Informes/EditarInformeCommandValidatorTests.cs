using IGE.Informes.Application.Informes.Commands.EditarInforme;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// Bug real reportado por el usuario: al editar solo la Carátula (ej. con
/// el <select> de TipoCausa, HU-19) sin tocar el N° de Pieza Sumarial de
/// un Informe migrado que nunca lo tuvo, el Handler descartaba el cambio
/// de Causa en silencio (Causa exige ambos campos, ver Domain/Causa.cs) —
/// la Dependencia se guardaba igual, sin ningún aviso de que la Causa no.
/// Este Validator convierte ese descarte silencioso en un error explícito
/// antes de llegar al Handler.
/// </summary>
public class EditarInformeCommandValidatorTests
{
    private readonly EditarInformeCommandValidator _validator = new();

    [Fact]
    public void Rechaza_caratula_sin_pieza_sumarial()
    {
        var command = new EditarInformeCommand(
            Guid.NewGuid(), null, null, "AV. ROBO", null, null);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EditarInformeCommand.CausaNroPiezaSumarial));
    }

    [Fact]
    public void Rechaza_pieza_sumarial_sin_caratula()
    {
        var command = new EditarInformeCommand(
            Guid.NewGuid(), null, null, null, "123/2026", null);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EditarInformeCommand.CausaCaratula));
    }

    [Fact]
    public void Acepta_caratula_y_pieza_sumarial_juntas()
    {
        var command = new EditarInformeCommand(
            Guid.NewGuid(), null, null, "AV. ROBO", "123/2026", null);

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Acepta_ambos_campos_de_causa_vacios()
    {
        var command = new EditarInformeCommand(
            Guid.NewGuid(), null, null, null, null, null);

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
    }
}

using IGE.Informes.Application.Informes.Commands.EditarInforme;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// docs/03-modelo-dominio.md, entrada "Causa.NroPiezaSumarial pasa a ser
/// opcional": hay Dependencias/tipos de análisis (ej. Narcotráfico) que no
/// aportan un N° de Pieza Sumarial real. Completar solo la Carátula, sin
/// Pieza Sumarial, es un caso válido — el bug real (Informes 38/2023 y
/// 73/2022 compartiendo Causa por el placeholder "--/--") se corrigió en
/// CausaMatcher, no exigiendo Pieza Sumarial acá. La regla inversa
/// (Pieza Sumarial sin Carátula) sigue siendo inválida.
/// </summary>
public class EditarInformeCommandValidatorTests
{
    private readonly EditarInformeCommandValidator _validator = new();

    [Fact]
    public void Acepta_caratula_sin_pieza_sumarial()
    {
        var command = new EditarInformeCommand(
            Guid.NewGuid(), null, null, "AV. ROBO", null, null);

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
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

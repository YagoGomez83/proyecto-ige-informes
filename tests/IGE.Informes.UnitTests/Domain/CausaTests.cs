using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class CausaTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

        Assert.NotEqual(Guid.Empty, causa.Id);
        Assert.Equal("N.N. s/Robo", causa.Caratula);
        Assert.Equal("7070029/26", causa.NroPiezaSumarial);
        Assert.Equal("Primera Circunscripción", causa.CircunscripcionJudicial);
    }

    [Theory]
    [InlineData("", "7070029/26")]
    [InlineData("N.N. s/Robo", "")]
    public void Alta_rechaza_campos_obligatorios_vacios(string caratula, string pieza)
    {
        Assert.Throws<ArgumentException>(() => new Causa(caratula, pieza, "Primera Circunscripción"));
    }

    // docs/03-modelo-dominio.md, "Decisiones ya resueltas" —
    // Causa.CircunscripcionJudicial pasa a ser opcional: varios expedientes
    // reales no la especifican. El constructor solo debe exigir Carátula y
    // N° de Pieza Sumarial no vacíos; Circunscripción judicial nula o vacía
    // ya no debe lanzar ArgumentException — y "" o "   " se normalizan a
    // null (nunca se persiste un espacio en blanco como si fuera un dato
    // real, mismo criterio que otros campos opcionales del proyecto).
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_AceptaCircunscripcionJudicialNulaOVacia_NoLanzaExcepcion(string? circunscripcion)
    {
        var causa = new Causa("N.N. s/Robo", "7070029/26", circunscripcion);

        Assert.NotEqual(Guid.Empty, causa.Id);
        Assert.Equal("N.N. s/Robo", causa.Caratula);
        Assert.Equal("7070029/26", causa.NroPiezaSumarial);
        Assert.Null(causa.CircunscripcionJudicial);
    }
}

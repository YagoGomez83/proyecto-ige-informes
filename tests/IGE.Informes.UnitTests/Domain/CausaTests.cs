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
    public void Alta_rechaza_campos_obligatorios_vacios(string caratula, string pieza)
    {
        Assert.Throws<ArgumentException>(() => new Causa(caratula, pieza, "Primera Circunscripción"));
    }

    // docs/03-modelo-dominio.md, entrada "Causa.NroPiezaSumarial pasa a ser
    // opcional" (hallazgo real: Informes 38/2023 y 73/2022 compartiendo
    // Causa por el placeholder "--/--"). Solo Caratula sigue siendo
    // obligatoria — NroPiezaSumarial nulo o vacío ya NO debe lanzar
    // ArgumentException.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_AceptaNroPiezaSumarialNuloOVacio_NoLanzaExcepcion(string? nroPiezaSumarial)
    {
        var causa = new Causa("AV. INFRACCIÓN LEY 23.737", nroPiezaSumarial, "Primera Circunscripción");

        Assert.NotEqual(Guid.Empty, causa.Id);
        Assert.Equal("AV. INFRACCIÓN LEY 23.737", causa.Caratula);
        Assert.Null(causa.NroPiezaSumarial);
    }

    // La carátula sigue siendo el único campo realmente obligatorio, incluso
    // cuando NroPiezaSumarial viene null (no alcanza con "no lanzar antes
    // por la pieza sumarial vacía" — Caratula vacía debe seguir fallando).
    [Fact]
    public void Alta_RechazaCaratulaVaciaAunConNroPiezaSumarialNulo()
    {
        Assert.Throws<ArgumentException>(() => new Causa("", null, "Primera Circunscripción"));
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

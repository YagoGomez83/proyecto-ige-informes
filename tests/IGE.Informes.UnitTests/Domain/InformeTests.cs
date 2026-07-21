using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class InformeTests
{
    private static readonly Guid CasoAnalisisId = Guid.NewGuid();
    private static readonly Guid DependenciaDestinoId = Guid.NewGuid();
    private static readonly Guid AnalistaSolicitanteId = Guid.NewGuid();

    private static Informe CrearInforme(Guid? causaId = null) => new(
        "290/2026",
        new DateOnly(2026, 7, 21),
        CasoAnalisisId,
        DependenciaDestinoId,
        AnalistaSolicitanteId,
        causaId);

    [Fact]
    public void Alta_nace_en_Borrador_y_asigna_al_solicitante_como_Interviniente()
    {
        var informe = CrearInforme();

        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Single(informe.Analistas);
        Assert.Equal(AnalistaSolicitanteId, informe.Analistas.Single().UsuarioId);
        Assert.Equal(RolInformeAnalista.Interviniente, informe.Analistas.Single().Rol);
    }

    [Fact]
    public void Alta_sin_Causa_se_acepta_dado_que_es_nullable()
    {
        var informe = CrearInforme(causaId: null);

        Assert.Null(informe.CausaId);
    }

    [Fact]
    public void Alta_con_Causa_la_vincula()
    {
        var causaId = Guid.NewGuid();

        var informe = CrearInforme(causaId);

        Assert.Equal(causaId, informe.CausaId);
    }

    [Fact]
    public void Alta_rechaza_id_registro_vacio()
    {
        Assert.Throws<ArgumentException>(() => new Informe(
            "", new DateOnly(2026, 7, 21), CasoAnalisisId, DependenciaDestinoId, AnalistaSolicitanteId));
    }

    [Fact]
    public void Alta_rechaza_caso_de_analisis_vacio()
    {
        Assert.Throws<ArgumentException>(() => new Informe(
            "290/2026", new DateOnly(2026, 7, 21), Guid.Empty, DependenciaDestinoId, AnalistaSolicitanteId));
    }

    [Fact]
    public void Alta_rechaza_dependencia_destino_vacia()
    {
        Assert.Throws<ArgumentException>(() => new Informe(
            "290/2026", new DateOnly(2026, 7, 21), CasoAnalisisId, Guid.Empty, AnalistaSolicitanteId));
    }
}
